using System.Numerics;
using Walgelijk;

namespace MIR;

/// <summary>
/// Plays cutscenes for <see cref="CutscenePlayerComponent"/>s.
/// </summary>
public class CutscenePlayerSystem : Walgelijk.System
{
    public override void Update()
    {
        if (!MadnessUtils.FindPlayer(Scene, out _, out var character))
            return;

        if (MadnessUtils.IsCutscenePlaying(Scene))
            return;

        foreach (var item in Scene.GetAllComponentsOfType<CutscenePlayerComponent>())
        {
            var dist = Vector2.Distance(item.Position, character.Positioning.GlobalCenter);

            if (dist < 64)
            {
                Scene.RemoveEntity(item.Entity);
                Scene.AttachComponent(Scene.CreateEntity(), new CutsceneComponent(item.Cutscene));
                if (item.ProgressLevelOnEnd && Scene.TryGetSystem<LevelProgressSystem>(out var s))
                    MadnessUtils.WaitUntil(() => (!MadnessUtils.IsCutscenePlaying(Scene)), () =>
                    {
                        s.Win();
                        s.TransitionToNextLevel();
                    });
                return;
            }
        }
    }
}