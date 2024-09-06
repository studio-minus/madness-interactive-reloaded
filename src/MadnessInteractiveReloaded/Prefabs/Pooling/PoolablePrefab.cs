using System;
using Walgelijk;

namespace MIR;

/// <summary>
/// A prefab that gets re-used in a pool via the <see cref="PrefabPoolSystem"/>.
/// </summary>
public class PoolablePrefab
{
    public PrefabFunction OnCreateNew;
    public Action<Scene, Entity> OnReturn;

    public PoolablePrefab(PrefabFunction onCreateNew, Action<Scene, Entity> onReturn)
    {
        OnCreateNew = onCreateNew;
        OnReturn = onReturn;
    }

    public PrefabPoolID Identity => new(GetHashCode());
}
