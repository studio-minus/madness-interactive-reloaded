namespace MIR;

using Walgelijk;

/// <summary>
/// System for entities with <see cref="LifetimeComponent"/>.
/// </summary>
public class LifetimeSystem : Walgelijk.System
{
    public override void Update()
    {
        if (MadnessUtils.IsPaused(Scene) || MadnessUtils.EditingInExperimentMode(Scene) || MadnessUtils.IsCutscenePlaying(Scene))
            return;

        foreach (var item in Scene.GetAllComponentsOfType<LifetimeComponent>())
            item.Lifetime += Time.DeltaTime;
    }
}
