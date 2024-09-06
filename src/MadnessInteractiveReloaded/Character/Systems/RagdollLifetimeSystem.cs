using Walgelijk;

namespace MIR;

/// <summary>
/// Keeps track of and deletes stale ragdolls.
/// </summary>
public class RagdollLifetimeSystem : Walgelijk.System
{
    public override void Update()
    {
        if (MadnessUtils.EditingInExperimentMode(Scene) || MadnessUtils.IsPaused(Scene))
            return;

        foreach (var ragdoll in Scene.GetAllComponentsOfType<RagdollComponent>())
        {
            if (!ragdoll.ShouldDelete)
                continue;

            ragdoll.Lifetime += Time.DeltaTime;
            if (ragdoll.Lifetime > 10 || ragdoll.SleepingTime > 2)
                ragdoll.Delete(Scene, ragdoll.Lifetime <= 10);
        }
    }
}
