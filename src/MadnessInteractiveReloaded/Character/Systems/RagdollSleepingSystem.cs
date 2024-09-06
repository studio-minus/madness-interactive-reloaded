using System.Numerics;
using Walgelijk;

namespace MIR;

/// <summary>
/// Reduces computation by putting ragdolls to sleep if they've
/// been inactive for long enough.
/// </summary>
public class RagdollSleepingSystem : Walgelijk.System
{
    public override void Update()
    {
        if (MadnessUtils.EditingInExperimentMode(Scene) || MadnessUtils.IsPaused(Scene))
            return;

        foreach (var ragdoll in Scene.GetAllComponentsOfType<RagdollComponent>())
        {
            var velocity = Vector2.Zero;
            foreach (var n in ragdoll.Nodes)
                if (n.TryGet(Scene, out var node))
                    velocity = Vector2.Max(Vector2.Abs(node.PreviousVelocity), velocity);
            if (velocity.LengthSquared() < 100f)
                ragdoll.SleepingTime += Time.DeltaTime;
            else
                ragdoll.SleepingTime = 0;
        }
    }
}
