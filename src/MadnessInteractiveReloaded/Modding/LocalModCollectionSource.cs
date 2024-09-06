using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.CommandLine.Help;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Walgelijk;

namespace MIR;

public class LocalModCollectionSource : IModCollectionSource
{
    public readonly DirectoryInfo Directory;

    private readonly Dictionary<ModID, DirectoryInfo> modDirs = [];

    public readonly static ImmutableArray<string> IgnoredDevFolderNames = [
        "obj",
        ".vscode",
    ];

    public readonly static ImmutableArray<string> IgnoredDevFileExtensions = [
        ".cache",
        ".targets",
        ".props",
        ".csproj",
        ".sln",
    ];

    public LocalModCollectionSource(DirectoryInfo dir)
    {
        Directory = dir;
        if (!dir.Exists)
            dir.Create();
    }

    public IEnumerable<Mod> ReadAll()
    {
        foreach (var dir in Directory.GetDirectories("*", SearchOption.TopDirectoryOnly))
            yield return LoadModFromDirectory(dir);

        foreach (var file in Directory.GetFiles("*.zip"))
            yield return LoadModFromFile(file);
    }

    /// <summary>
    /// Tries to find the mod with the given ID in this source. Returns true if found. The found mod is assigned to the <paramref name="mod"/> parameter. This method is quite expensive.
    /// </summary>
    public bool TryRead(ModID id, [NotNullWhen(true)] out Mod? mod)
    {
        // TODO find a way to optimise this. Maybe by making it impossible to get a mod 

        var dirs = Directory.GetDirectories("*", SearchOption.TopDirectoryOnly);
        foreach (var dir in dirs)
        {
            if (dir.TryGetFile("meta.json", out var p))
            {
                try
                {
                    var json = File.ReadAllText(p.FullName);
                    var m = new
                    {
                        id = string.Empty
                    };
                    var meta = JsonConvert.DeserializeAnonymousType(json, m);

                    if ((meta?.id ?? string.Empty) == id)
                    {
                        mod = LoadModFromDirectory(dir);
                        return true;
                    }
                }
                catch (System.Exception e)
                {
                    Logger.Error("Failed to read mod meta: " + e.Message);
                }
            }
        }

        var files = Directory.GetFiles("*.zip");
        foreach (var file in files)
        {
            using var zip = new ZipArchive(file.Open(FileMode.Open));
            if (zip.TryGetEntry("meta.json", out var metaEntry))
            {
                using var stream = metaEntry.Open();
                using var json = new StreamReader(stream);
                var m = new
                {
                    id = string.Empty
                };
                var meta = JsonConvert.DeserializeAnonymousType(json.ReadToEnd(), m);

                if ((meta?.id ?? string.Empty) == id)
                {
                    stream.Dispose();
                    json.Dispose();
                    mod = new Mod(zip);
                    //mod = LoadModFromFile(file);
                    return true;
                }
            }
        }

        mod = null;
        return false;
    }

    private Mod LoadModFromFile(FileInfo file)
    {
        var m = new Mod(new ZipArchive(file.Open(FileMode.Open)));
        if (modDirs.ContainsKey(m.Id))
        {
            m.Dispose();
            throw new System.Exception($"Mod with ID (\"{m.Id}\") already present");
        }
        modDirs.Add(m.Id, file.Directory ?? Directory);
        return m;
    }

    /// <summary>
    /// Returns the directory for the given mod. Null if it was never loaded.
    /// </summary>
    public DirectoryInfo? GetModDirectory(ModID mod)
    {
        return modDirs.TryGetValue(mod, out var dir) ? dir : null;
    }

    private Mod LoadModFromDirectory(DirectoryInfo dir)
    {
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            AddFolderToZip("", zip, dir, root: true);
        }

        var readZip = new ZipArchive(stream);

        if (System.IO.Directory.Exists("./debugout/"))
            System.IO.Directory.Delete("./debugout/", true);
        readZip.ExtractToDirectory("./debugout/", true);

        var m = new Mod(readZip);
        modDirs.Add(m.Id, dir);
        return m;
    }

    private void AddFolderToZip(string parent, ZipArchive zip, DirectoryInfo dir, bool root = false)
    {
        if (dir.Attributes.HasFlag(FileAttributes.Hidden) || IgnoredDevFolderNames.Any(f => dir.Name.Equals(f, System.StringComparison.InvariantCultureIgnoreCase)))
            return;

        var path = root ? parent : Path.Combine(parent, dir.Name + '/');

        foreach (var chilDir in dir.GetDirectories())
        {
            AddFolderToZip(path, zip, chilDir);
        }

        foreach (var file in dir.GetFiles("*", SearchOption.TopDirectoryOnly))
        {
            if (file.Attributes.HasFlag(FileAttributes.Hidden) || IgnoredDevFileExtensions.Any(f => file.Extension.Equals(f, System.StringComparison.InvariantCultureIgnoreCase)))
                continue;

            var entry = zip.CreateEntry(Path.Combine(path, file.Name), CompressionLevel.Fastest);
            using var write = entry.Open();
            using var read = file.OpenRead();
            read.CopyTo(write);
        }
    }
}
