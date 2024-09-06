namespace MIR;

using Walgelijk;

/// <summary>
/// Uses a <see cref="FixedIntervalDistributor"/>
/// to to apply velocity at a fixed rate to <see cref="MeasuredVelocityComponent"/>.
/// </summary>
public class MeasuredVelocitySystem : Walgelijk.System
{
    public readonly FixedIntervalDistributor MeasureRate = new(100);

    public override void Update()
    {
        if (MadnessUtils.IsPaused(Scene) || MadnessUtils.EditingInExperimentMode(Scene) || MadnessUtils.IsCutscenePlaying(Scene))
            return;

        for (int i = 0; i < MeasureRate.CalculateCycleCount(Time.DeltaTime); i++)
        {
            foreach (var comp in Scene.GetAllComponentsOfType<MeasuredVelocityComponent>())
            {
                var transform = Scene.GetComponentFrom<TransformComponent>(comp.Entity);

                if (comp.ValidState) //valid state betekent dat de Previous waarden zijn geupdate :)
                {
                    comp.DeltaTranslation = (transform.Position - comp.PreviousTranslation) / MeasureRate.Interval;
                    comp.DeltaRotation = (Utilities.DeltaAngle(comp.PreviousRotation, transform.Rotation)) / MeasureRate.Interval;
                }

                comp.PreviousTranslation = transform.Position;
                comp.PreviousRotation = transform.Rotation;
                comp.ValidState = true;
            }
        }
    }
}
