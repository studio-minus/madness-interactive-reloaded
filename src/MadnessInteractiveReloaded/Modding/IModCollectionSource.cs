using System.Collections.Generic;

namespace MIR;

public interface IModCollectionSource
{
    public IEnumerable<Mod> ReadAll();
}
