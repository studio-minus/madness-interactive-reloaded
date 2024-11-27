using System;
using System.Reflection;

namespace MIR;

/// <summary>
/// Static class that contains game version information
/// </summary>
public static class GameVersion
{
    /// <summary>
    /// Current game version
    /// </summary>
    public static readonly Version Version;

    static GameVersion()
    {
        var v = Assembly.GetExecutingAssembly().GetName()!.Version!;
        Version = new(v.Major, v.Minor, v.Build);
    }
}