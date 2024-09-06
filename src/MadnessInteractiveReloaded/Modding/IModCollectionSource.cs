using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MIR;

public interface IModCollectionSource
{
    public IEnumerable<Mod> ReadAll();
    public bool TryRead(ModID id, [NotNullWhen(true)] out Mod? mod);
}
