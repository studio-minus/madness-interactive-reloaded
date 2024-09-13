using Walgelijk;

namespace MIR;

/// <summary>
/// Handle level transition triggers.
/// </summary>
public class LevelProgressTriggerSystem : Walgelijk.System
{
    public override void Update()
    {
        if (!MadnessUtils.FindPlayer(Scene, out var player, out var character) ||
            !Scene.FindAnyComponent<LevelProgressComponent>(out var progress) ||
            !progress.GoalReached)
            return;

        var playerBounds = character.GetBoundingBox(Scene);

        foreach (var trigger in Scene.GetAllComponentsOfType<LevelProgressTriggerComponent>())
        {
            if (!trigger.IsExpired && trigger.WorldRect.IntersectsRectangle(playerBounds))
            {
                trigger.IsExpired = true;
                Scene.GetSystem<LevelProgressSystem>().TransitionToNextLevel();
            }
        }
    }
}
