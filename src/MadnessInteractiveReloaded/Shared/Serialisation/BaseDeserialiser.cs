using System;
using System.Collections.Generic;
using System.IO;

namespace MIR.Serialisation;

/// <summary>
/// All custom deserialisers should inherit the behaviour of this deserialiser.
/// </summary>
public static class BaseDeserialiser
{
    public const string CommentPrefix = "#";
    public record Line(string String, Version Version, int LineNumber, string Untrimmed);

    /// <exception cref="Exceptions.SerialisationException"></exception>
    public static IEnumerable<Line> Read(StreamReader input)
    {
        var expectedVersion = input.ReadLine();
        int ln = 2;

        if (!Version.TryParse(expectedVersion, out var version))
            throw new Exceptions.SerialisationException($"Expected version at the first line, got '{expectedVersion}' instead");

        while (true)
        {
            var line = input.ReadLine();
            if (line == null)
                yield break;
            if (string.IsNullOrWhiteSpace(line))
                continue;
            if (line.StartsWith(CommentPrefix))
                continue;

            yield return new(line.Trim(), version, ln, line);

            ln++;
        }
    }

    /// <summary>
    /// Save something to disk.
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="lines"></param>
    /// <returns></returns>
    public static bool Write(StreamWriter writer, IEnumerable<string> lines)
    {
        writer.WriteLine(GameVersion.Version);
        writer.WriteLine();

        foreach (var l in lines)
        {
            if (l == null || string.IsNullOrWhiteSpace(l))
                continue;

            writer.WriteLine(l.Trim());
        }

        return true;
    }
}
