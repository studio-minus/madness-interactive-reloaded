using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Loader;
using Walgelijk;
using Walgelijk.AssetManager;

namespace MIR;

/// <summary>
/// A loaded game mod. Its assembly (if applicable) lives here as well.
/// </summary>
public sealed class Mod : IDisposable
{
    /// <summary>
    /// Globally unique ID for this mod. It is recommended to use a friendly name so that the mod can be identified by humans from the ID alone. E.g. "<c>studiominus.zooi.telekenesis</c>"
    /// </summary>
    public readonly ModID Id;

    /// <summary>
    /// The mod type, determined by the presence of compiled code in the mod file
    /// </summary>
    public readonly ModType ModType;

    /// <summary>
    /// Collection of Mod IDs on which this mod depends. This is used to determine mod loading order.
    /// </summary>
    public readonly ImmutableArray<ModID> Dependencies;

    /// <summary>
    /// The thumbnail texture used in the game UI
    /// </summary>
    public readonly IReadableTexture Thumbnail = Texture.ErrorTexture;

    /// <summary>
    /// Friendly display name for this mod
    /// </summary>
    public readonly string Name;

    /// <summary>
    /// Author names used in the UI
    /// </summary>
    public readonly string Authors;

    /// <summary>
    /// A somewhat-rich-text description of the mod. The first paragraph is used as a short description or tagline, <see cref="ShortDescription"/>
    /// </summary>
    public readonly string Description;

    /// <summary>
    /// The first paragraph if <see cref="Description"/>
    /// </summary>
    public readonly string ShortDescription;

    /// <summary>
    /// Version of the mod
    /// </summary>
    public readonly string Version;

    /// <summary>
    /// Path to binary
    /// </summary>
    public readonly string BinaryPath;

    /// <summary>
    /// If true, the assembly can be unloaded at the cost of some performance. This means the mod can be recompiled. If false, the assembly is permanently loaded and performance is improved.
    /// </summary>
    public readonly bool DevelopmentMode = false;

    /// <summary>
    /// Path to the extracted archive
    /// </summary>
    public string ExtractedPath => ExtractedFolder.FullName;

    internal readonly ZipArchive ZipArchive;
    internal readonly DirectoryInfo ExtractedFolder;
    //internal readonly AssemblyLoadContext? AssemblyLoadContext;
    internal readonly ModAssemblyWrapper? Assembly;

    internal readonly string CompressedSizeString;
    internal ConcurrentBag<Exception> Errors = [];
    internal FileInfo[] AssetPackages;

    public readonly long CompressedSize;
    public readonly long UncompressedSize;

    /* TODO mod api
     * 
     * - mod config.
     *      just a function provided by the mod assembly that is called as a UI function. it SHOULD contain a load of buttons and dials. one issue with this is that the mod needs to be loaded and active and  everything. 
     *      alternatively, the mod config could be a `config.json` that provides names, types, and UI options thatll be used to draw the proper UI. one issue with this is that i dont know how to make the mod get the config data.
     */

    private bool disposed;

    public Mod(ZipArchive zipArchive)
    {
        // TODO error handling etc.

        ZipArchive = zipArchive;

        var metaJson = zipArchive.GetEntry("meta.json") ?? throw new System.Exception("Mod contains no \"meta.json\"");
        var thumbnail = zipArchive.GetEntry("thumb.png");

        // read metadata
        using (var s = metaJson.Open())
        {
            using var reader = new StreamReader(s);
            var intermediate = new
            {
                // default metadata

                id = "invalid.mod.id",
                dependencies = Array.Empty<string>(),

                name = "Untitled",
                authors = "Unknown",
                description = "No description provided",
                binary = string.Empty,

                version = "???",
                devMode = false

            };

            var j = JsonConvert.DeserializeAnonymousType(reader.ReadToEnd(), intermediate) ?? throw new Exception("Mod has null metadata");

            Id = j.id;
            Dependencies = j.dependencies?.Select(s => new ModID(s)).ToImmutableArray() ?? [];
            Name = j.name ?? "Untitled";
            Authors = j.authors ?? "No authors provided";
            Description = j.description ?? "No description provided";
            var terminator = Description.IndexOf('\n');
            if (terminator == -1)
                terminator = Description.Length;
            ShortDescription = Description[..terminator];
            BinaryPath = j.binary;
            Version = j.version;
            DevelopmentMode = j.devMode;
        }

        // read thumbnail
        if (thumbnail == null)
        {
            Thumbnail = Textures.Black;
            Errors.Add(new Exception($"No thumbnail for {Id}"));
        }
        else
        {
            using var s = thumbnail.Open();
            using var m = new MemoryStream();

            s.CopyTo(m);
            try
            {
                Thumbnail = TextureLoader.FromBytes(m.ToArray());
            }
            catch (Exception e)
            {
                Errors.Add(e);
                Logger.Error($"Failed to load thumbnail for {Id}: {e}");
            }
        }

        foreach (var item in zipArchive.Entries)
        {
            UncompressedSize += item.Length;
            CompressedSize += item.CompressedLength;
        }

        {
            // TODO important!!! calculate a hash for the zip archive and store it with the cache folder
            // then, before extracting the zip again, check if the folder exists already
            // if it does, compare the stored hash against the newly created hash
            // if they match, do nothing. otherwise, delete it and start extracting
            var mirTemp = Path.Combine(Path.GetTempPath(), "MIR/");
            if (!Directory.Exists(mirTemp))
                Directory.CreateDirectory(mirTemp);

            ExtractedFolder = new DirectoryInfo(Path.Combine(mirTemp, $"{Id}/"));
            if (ExtractedFolder.Exists)
                ExtractedFolder.Delete(true);
            ExtractedFolder.Create();
            zipArchive.ExtractToDirectory(ExtractedFolder.FullName, true);
            Logger.Log($"Mod \"{Id}\" extracted to \"{ExtractedFolder}\"");

            AssetPackages = [.. ExtractedFolder.EnumerateFiles("*.waa", SearchOption.AllDirectories)];
        }

        // read assembly
        if (!string.IsNullOrWhiteSpace(BinaryPath))
        {
            if (!ExtractedFolder.TryGetFile(BinaryPath, out var assemblyFile))
                Errors.Add(new FileNotFoundException($"Binary path not found \"{BinaryPath}\""));
            else
            {
                try
                {
                    var assembly = global::System.Reflection.Assembly.LoadFrom(assemblyFile.FullName);
                    Assembly = new ModAssemblyWrapper(this, assembly);
                }
                catch (Exception e)
                {
                    Errors.Add(e);
                    Logger.Error($"Failed to load assembly for ${Id}: {e}");
                }
                // TODO maybe get some kind of unique ID for this assembly so that you can detect later which assembly belongs to which mod
            }
            ModType = ModType.Script;
        }
        else
            ModType = ModType.Data;

        CompressedSizeString = GetFriendlySizeString(CompressedSize);
    }

    private static string GetFriendlySizeString(long byteCount)
    {
        if (byteCount > 1e9) // GB
            return $"{byteCount / 1000_000_000} GB";

        if (byteCount > 1e6) // MB
            return $"{byteCount / 1000_000} MB";

        if (byteCount > 1000) // kB
            return $"{byteCount / 1000} kB";

        return $"{byteCount} B";
    }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        ZipArchive.Dispose();
        Thumbnail.Dispose();

        GC.SuppressFinalize(this);
    }

    // TODO this can be cached because everything is readonly (maybe the compiler does it by itself lol)
    public override string ToString() => $"{ModType} mod \"{Name}\" ({Id})";
}
