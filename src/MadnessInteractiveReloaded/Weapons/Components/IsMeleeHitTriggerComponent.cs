namespace MIR;
using Walgelijk;

/// <summary>
/// If this is on an entity a <see cref="HitEvent"/> will be dispatched if it is melee hit
/// </summary>
[RequiresComponents(typeof(Walgelijk.Physics.PhysicsBodyComponent))]
public class IsMeleeHitTriggerComponent : Component
{
    //public uint LayerMask = CollisionLayers.All;
    public readonly Hook<HitEvent> Event = new();
}
