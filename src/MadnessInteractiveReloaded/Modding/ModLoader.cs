using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Walgelijk;
using Walgelijk.AssetManager;

namespace MIR;

public static class ModLoader
{
    /// <summary>
    /// Get a copy of the loaded mod list
    /// </summary>
    public static IEnumerable<Mod> Mods => sortedMods.Select(static m => mods[m].Mod);

    /// <summary>
    /// Event dispatched when the mod list has changed
    /// </summary>
    public static event Action? OnModListChange;

    public static IEnumerable<IModCollectionSource> Sources => sources;

    private static readonly ConcurrentBag<IModCollectionSource> sources = [];
    private static readonly ConcurrentDictionary<ModID, LoadedMod> mods = [];
    private static readonly List<ModID> sortedMods = [];

    private static readonly SemaphoreSlim modLoadingSemaphore = new(1);
    private static bool needsAssetRefresh = false;

    /// <summary>
    /// Mod IDs the player has explicitly disabled. Persisted between sessions.
    /// A disabled mod is loaded (so it shows in the menu) but never activated.
    /// </summary>
    private static readonly HashSet<string> disabled = [];
    private static bool disabledLoaded = false;

    [Command(Alias = "ModSources", HelpString = "List mod sources and their status")]
    private static void ModSourcesCmd()
    {
        var str = new StringBuilder();
        foreach (var src in sources)
        {
            int l = mods.Values.Count(m => m.Source == src);

            str.Append(src.GetType().Name);
            str.Append(" | ");
            str.Append(src.IsValid ? "valid" : "invalid");
            str.Append(" | ");
            str.AppendFormat("{0} loaded", l);

            switch (src)
            {
                case LocalModCollectionSource local:
                    str.Append(" | ");
                    str.Append(local.Directory.FullName);
                    break;
            }

            str.AppendLine();
        }
        Game.Main.Console.WriteLine(str.ToString(), ConsoleMessageType.Plain);
    }

    public static void AddSource(IModCollectionSource source)
    {
        Logger.Log($"Mod source added {source}");
        sources.Add(source);
    }

    /// <summary>
    /// Returns true if the player has persistently disabled the given mod.
    /// </summary>
    public static bool IsDisabled(ModID id)
    {
        EnsureDisabledListLoaded();
        return disabled.Contains(id.Value);
    }

    private static void EnsureDisabledListLoaded()
    {
        if (disabledLoaded)
            return;
        disabledLoaded = true;

        try
        {
            var path = UserData.Paths.DisabledMods;
            if (File.Exists(path))
                foreach (var line in File.ReadAllLines(path))
                {
                    var id = line.Trim();
                    if (!string.IsNullOrEmpty(id))
                        disabled.Add(id);
                }
        }
        catch (Exception e)
        {
            Logger.Warn($"Failed to read disabled mods list: {e}");
        }
    }

    private static void SaveDisabledList()
    {
        try
        {
            var path = UserData.Paths.DisabledMods;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            lock (disabled)
                File.WriteAllLines(path, disabled);
        }
        catch (Exception e)
        {
            Logger.Error($"Failed to save disabled mods list: {e}");
        }
    }

    /// <summary>
    /// Enable or disable a mod, persisting the choice. Enabling activates the mod immediately.
    /// Disabling deactivates script mods immediately; asset changes from data mods fully apply on the next launch.
    /// </summary>
    public static void SetModEnabled(ModID id, bool enabled)
    {
        EnsureDisabledListLoaded();

        if (enabled)
        {
            if (disabled.Remove(id.Value))
                SaveDisabledList();
            ActivateMod(id);
        }
        else
        {
            bool changed;
            lock (disabled)
                changed = disabled.Add(id.Value);
            if (changed)
                SaveDisabledList();
            DeactivateMod(id);
        }
    }

    /// <summary>
    /// Deactivate a loaded mod without unloading it. Script mods get <see cref="IModEntry.OnUnload"/> called.
    /// </summary>
    public static void DeactivateMod(ModID id)
    {
        if (!mods.TryGetValue(id, out var loaded) || !loaded.Active)
            return;

        loaded.Active = false;
        needsAssetRefresh = true;
        var mod = loaded.Mod;

        if (mod.ModType is ModType.Script && mod.Assembly != null)
        {
            try
            {
                mod.Assembly.ModEntry.OnUnload();
            }
            catch (Exception e)
            {
                Logger.Error($"Mod {mod.Name} threw " + e);
            }
        }

        OnModListChange?.Invoke();
    }

    public static void LoadModsFromSources()
    {
        foreach (var source in sources)
        {
            LoadModsFromSource(source);
        }
    }

    public static void UnloadMod(ModID id)
    {
        modLoadingSemaphore.Wait();

        try
        {
            if (mods.TryRemove(id, out var l))
            {
                var mod = l.Mod;

                if (mod.ModType is ModType.Script && mod.Assembly != null)
                {
                    try
                    {
                        mod.Assembly.ModEntry.OnUnload();
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"Mod {mod.Name} threw " + e);
                    }
                }

                mod.Dispose();
            }
            else
                throw new KeyNotFoundException("Invalid mod id: not found");
        }
        finally
        {
            modLoadingSemaphore.Release();
        }
    }

    public static IModCollectionSource GetSourceFor(ModID id)
    {
        if (mods.TryGetValue(id, out var loaded))
            return loaded.Source;
        throw new Exception($"Give mod ID \"{id}\" was not loaded");
    }

    public static void LoadModsFromSource(IModCollectionSource source)
    {
        foreach (var mod in source.ReadAll())
        {
            LoadMod(source, mod);
        }
    }

    public static void LoadMod(IModCollectionSource source, Mod mod)
    {
        modLoadingSemaphore.Wait();
        lock (sortedMods)
            try
            {
                var loaded = new LoadedMod(source, mod, false);
                if (!mods.TryAdd(mod.Id, loaded))
                {
                    if (mods.TryGetValue(mod.Id, out loaded))
                        Logger.Error($"{nameof(ModLoader)} attempted to load a mod from source \"{source.GetType()}\" but the id \"{mod.Id}\" was already loaded from source \"{loaded.Source.GetType()}\"");
                    else
                        Logger.Error($"{nameof(ModLoader)} attempted to load a mod (\"{mod}\") from source \"{source.GetType()}\" but failed to add it to the collection");


                    // TODO this is fucked because this function should be designed to run
                    // on a different thread and that means that the event delegates will
                    // be executed on my thread as well and thats really confusing!! and bad
                    // solution is to set a flag and let others poll
                    OnModListChange?.Invoke();
                }
                else
                {
                    Logger.Log($"Mod {mod.Name} (\"{mod.Id}\") loaded from {source}");
                }

                sortedMods.Add(mod.Id);
                // don't activate mods the player has disabled; they still appear in the menu as inactive
                if (loaded != null && !IsDisabled(mod.Id))
                    ActivateMod(mod.Id);
            }
            finally
            {
                modLoadingSemaphore.Release();
            }
    }

    public static bool IsActive(ModID id)
    {
        return mods.TryGetValue(id, out var m) && m.Active;
    }

    public static void ActivateMod(ModID id)
    {
        if (!mods.TryGetValue(id, out var loaded))
            return;

        if (loaded.Active)
            return;

        if (loaded.Mod.Errors.Count > 0)
        {
            Logger.Error($"Can't activate mod \"{id}\" because it has errors: {string.Join(Environment.NewLine, loaded.Mod.Errors)}");
            return;
        }

        needsAssetRefresh = true;
        loaded.Active = true;
        var mod = loaded.Mod;

        foreach (var p in mod.AssetPackages)
            Assets.RegisterPackage(p.FullName);

        if (mod.ModType is ModType.Script && mod.Assembly != null)
        {
            try
            {
                mod.Assembly.ModEntry.OnReady();
            }
            catch (Exception e)
            {
                Logger.Error($"Mod {mod.Name} threw " + e);
            }
        }

        OnModListChange?.Invoke();
    }

    public static bool TryGet(ModID id, [NotNullWhen(true)] out Mod? mod)
    {
        if (mods.TryGetValue(id, out var loaded))
        {
            mod = loaded.Mod;
            return true;
        }
        mod = null;
        return false;
    }

    public class LoadedMod
    {
        public readonly IModCollectionSource Source;
        public readonly Mod Mod;
        public bool Active;

        public LoadedMod(IModCollectionSource source, Mod mod, bool active)
        {
            Source = source;
            Mod = mod;
            Active = active;
        }
    }
}
