using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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

    public static void AddSource(IModCollectionSource source)
    {
        Logger.Log($"Mod source added {source}");
        sources.Add(source);
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
                if (loaded != null)
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
