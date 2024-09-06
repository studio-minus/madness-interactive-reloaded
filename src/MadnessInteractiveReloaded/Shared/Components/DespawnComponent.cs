using Walgelijk;

namespace MIR;

/// <summary>
/// Make an entity despawn after the given time: <see cref="DespawnTime"/>.
/// </summary>
public class DespawnComponent : Component
{
    /// <summary>
    ///  How long has the component been active for (in seconds).
    /// </summary>
    public float Timer = 0;

    /// <summary>
    /// How long until the entity despawns after spawning (in seconds).
    /// </summary>
    public float DespawnTime = 5;

    /// <summary>
    /// Other entities you wish to delete alongside this one.
    /// </summary>
    public Entity[]? AlsoDelete = null;

    public DespawnComponent(float despawnTime)
    {
        DespawnTime = despawnTime;
    }
}
