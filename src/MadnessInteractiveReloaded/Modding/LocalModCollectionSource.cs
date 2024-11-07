using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
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
        foreach (var file in Directory.GetFiles("meta.json", SearchOption.AllDirectories))
        {
            if (file.Directory == null)
                continue;

            Mod? m = null;
            try
            {
                m = LoadModFromDirectory(file.Directory);
            }
            catch (System.Exception e)
            {
                Logger.Error($"Failed to load mod at {file.FullName}: {e}");
                m?.Dispose();
            }

            if (m != null)
                yield return m;
        }

        foreach (var file in Directory.GetFiles("*.zip"))
            yield return LoadModFromFile(file);
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

        var debugoutPath = Path.Combine(Game.Main.AppDataDirectory, "debugout/");
        if (System.IO.Directory.Exists(debugoutPath))
            System.IO.Directory.Delete(debugoutPath, true);
        readZip.ExtractToDirectory(debugoutPath, true);

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
