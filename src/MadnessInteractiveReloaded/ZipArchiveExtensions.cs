using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;

namespace MIR;

public static class ZipArchiveExtensions
{
    public static bool TryGetEntry(this ZipArchive z, string entryName, [NotNullWhen(true)] out ZipArchiveEntry? entry)
    {
        entry = z.GetEntry(entryName);
        if (entry == null)
            return false;
        return true;
    }
}

public static class IOExtensions
{
    public static bool TryGetDirectory(this DirectoryInfo parent, string childName, [NotNullWhen(true)] out DirectoryInfo? child)
    {
        var childPath = Path.Combine(parent.FullName, childName + '/');
        if (Directory.Exists(childPath))
        {
            child = new DirectoryInfo(childPath);
            return true;
        }
        child = null;
        return false;
    }   
    
    public static bool TryGetFile(this DirectoryInfo parent, string childName, [NotNullWhen(true)] out FileInfo? child)
    {
        var childPath = Path.Combine(parent.FullName, childName );
        if (File.Exists(childPath))
        {
            child = new FileInfo(childPath);
            return true;
        }
        child = null;
        return false;
    }
}