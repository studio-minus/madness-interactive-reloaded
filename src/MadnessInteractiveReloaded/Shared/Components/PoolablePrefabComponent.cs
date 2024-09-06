using Walgelijk;

namespace MIR;

/// <summary>
/// Used for referencing which prefabpool an entity belongs to.
/// </summary>
public class PoolablePrefabComponent : Component
{
    /// <summary>
    /// The ID of the prefab pool this entity came from.
    /// </summary>
    public PrefabPoolID OriginPool;

    /// <summary>
    /// If this pooled item is active and can't be re-used yet.
    /// </summary>
    public bool IsInUse = false;
}
