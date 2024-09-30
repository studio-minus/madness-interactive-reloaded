using Walgelijk;
using Walgelijk.Physics;

namespace MIR;

public class JumpDodgeSystem : Walgelijk.System
{
    public override void Update()
    {
        if (MadnessUtils.IsPaused(Scene)
            || MadnessUtils.IsCutscenePlaying(Scene)
            || MadnessUtils.EditingInExperimentMode(Scene))
            return;

        var dt = Time.DeltaTime;
        foreach (var item in Scene.GetAllComponentsOfType<JumpDodgeComponent>())
        {
            var character = Scene.GetComponentFrom<CharacterComponent>(item.Entity);

            item.Time += dt;
            if (item.Time > item.Duration || character.IsPlayingAnimationGroup("cutscene"))
            {
                // reset character after jumpin :)
                character.WalkAcceleration = item.InitialAcceleration;
                character.Positioning.HopStartingPosition = character.Positioning.GlobalCenter.X;
                character.Positioning.HopAcceleration = 1;
                Scene.DetachComponent<JumpDodgeComponent>(item.Entity);
            }
            else
            {
                // take over CharacterMovementSystem's job
                character.Positioning.HopAcceleration = 0;
                character.Positioning.HopAnimationTimer = -1;
                if (item.ShouldDash)
                {
                    const float jumpDodgeDashSpeed = 2000; // this magic number should be defined somewhere else
                    float progress = item.Time / item.Duration;
                    float v = item.InitialAcceleration.X > 0 ? jumpDodgeDashSpeed : -jumpDodgeDashSpeed;
                    character.WalkAcceleration.X = v * Easings.Circ.Out(Utilities.Clamp(progress));

                    var nextPos = character.Positioning.GlobalTarget + character.WalkAcceleration * dt;

                    var query = Scene.GetSystem<PhysicsSystem>().QueryCircle
                        (character.Positioning.Body.ComputedVisualCenter, 150 * character.Positioning.Scale, 
                        new QueryResult[1], CollisionLayers.BlockMovement);
                    if (query == 0)
                        character.Positioning.GlobalTarget = nextPos;
                }
            }
        }
    }
}
