using Walgelijk;

namespace MIR;

/// <summary>
/// Make a poolable return to the pool after a given time.
/// </summary>
[RequiresComponents(typeof(PoolablePrefabComponent))]
public class ReturnToPrefabPoolAfterTimeComponent : Component
{
    /// <summary>
    /// How long until we return it to the pool.
    /// </summary>
    public float TimeInSeconds = 1;

    /// <summary>
    /// How long the entity as been active for (in seconds).
    /// </summary>
    public float CurrentLifetime = 0;
}