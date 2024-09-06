using System.Collections.Generic;
using Walgelijk;

namespace MIR;

/// <summary>
/// Automatically runs through a spritesheet animation.
/// </summary>
public class FlipbookSystem : Walgelijk.System
{
    private readonly List<Material> toDelete = new();

    public override void Update()
    {
        if (MadnessUtils.IsPaused(Scene) || MadnessUtils.EditingInExperimentMode(Scene))
            return;

        foreach (var item in toDelete)
            Scene.Game.Window.Graphics.Delete(item);

        toDelete.Clear();

        foreach (var flipbook in Scene.GetAllComponentsOfType<FlipbookComponent>())
        {
            flipbook.Material.SetUniform("progress", flipbook.CurrentTime);
            flipbook.CurrentTime += Time.DeltaTime / flipbook.Duration;
            if (flipbook.CurrentTime > 1)
            {
                if (flipbook.DeleteWhenDone)
                {
                    Scene.RemoveEntity(flipbook.Entity);
                    //toDelete.Add(c.Material);
                }
                else if (flipbook.Loop)
                    flipbook.CurrentTime = 0;
            }
        }
    }
}
