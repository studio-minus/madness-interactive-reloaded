namespace MIR;

using System.Collections.Generic;
using System.IO;

public static class LinesReader
{
    /// <summary>
    /// Returns all lines in a text file, skipping lines starting with # or empty lines
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static IEnumerable<string> GetLines(string path)
    {
        using var file = new StreamReader(path);
        while (true)
        {
            var l = file.ReadLine();
            if (l == null)
                yield break;

            if (string.IsNullOrWhiteSpace(l) || l.StartsWith("#"))//# is a comment line
                continue;

            yield return l;
        }
    }
}
