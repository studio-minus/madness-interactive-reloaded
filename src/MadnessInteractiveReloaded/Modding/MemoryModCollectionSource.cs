using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace MIR;

/// <summary>
/// A mod collection that reads directly from memory. For debugging and testing purposes.
/// </summary>
public class MemoryModCollectionSource : IModCollectionSource, IDisposable
{
    public readonly Dictionary<ModID, Mod> Mods;

    public MemoryModCollectionSource(Mod[] mods)
    {
        Mods = mods.ToDictionary(d => d.Id);
    }

    public void Dispose()
    {
        foreach (var item in Mods)
            item.Value.Dispose();
        Mods.Clear();
    }

    public IEnumerable<Mod> ReadAll() => Mods.Values;
}
