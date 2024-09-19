using System.Numerics;
using Walgelijk;

namespace MIR;

/// <summary>
/// Runs logic for <see cref="ImpactOffsetComponent"/>
/// </summary>
public class ImpactOffsetSystem : Walgelijk.System
{
    public override void Update()
    {
        if (MadnessUtils.EditingInExperimentMode(Scene) || MadnessUtils.IsPaused(Scene))
            return;

        foreach (var impactOffset in Scene.GetAllComponentsOfType<ImpactOffsetComponent>())
        {
            impactOffset.TranslationOffset = Utilities.SmoothApproach(
                impactOffset.TranslationOffset,
                Vector2.Zero,
                impactOffset.DecaySpeed,
                Time.DeltaTime);

            impactOffset.TranslationOffset = MinMagnitude(impactOffset.TranslationOffset, 120);

            impactOffset.RotationOffset = Utilities.SmoothAngleApproach(
                impactOffset.RotationOffset,
                0, impactOffset.DecaySpeed,
                Time.DeltaTime);

            impactOffset.RotationOffset = float.Clamp(impactOffset.RotationOffset, -25, 25);
        }
    }

    private static Vector2 MinMagnitude(Vector2 v, float mag)
    {
        var m = v.Length();
        if (m <= float.Epsilon)
            return v;
        return v / m * float.Min(m, mag);
    }
}
