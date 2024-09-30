using System;
using System.Numerics;
using Walgelijk;

namespace MIR;

/// <summary>
/// Runs logic for <see cref="BodyPartShapeComponent"/>
/// </summary>
public class DestructibleBodyPartSystem : Walgelijk.System
{
    public override void Update()
    {
        if (MadnessUtils.IsPaused(Scene) || MadnessUtils.EditingInExperimentMode(Scene))
            return;

        foreach (var bodyPart in Scene.GetAllComponentsOfType<BodyPartShapeComponent>())
        {
            bodyPart.ShotHeat = Utilities.Clamp(bodyPart.ShotHeat - Time.DeltaTime, 0, 2);
        }
    }

    public override void Render()
    {
        foreach (var bodyPart in Scene.GetAllComponentsOfType<BodyPartShapeComponent>())
        {
            var material = bodyPart.RenderTask.Material;

            if (bodyPart.NeedsUpdate)
            {
                material.SetUniform("holesCount", bodyPart.HolesCount);
                material.SetUniform("holes", bodyPart.Holes);
                
                material.SetUniform("slashesCount", bodyPart.SlashesCount);
                material.SetUniform("slashes", bodyPart.Slashes);

                material.SetUniform("innerCutoutHolesCount", bodyPart.InnerCutoutHolesCount);
                material.SetUniform("innerCutoutHoles", bodyPart.InnerCutoutHoles);

                bodyPart.NeedsUpdate = false;
            }
        }   
        
        foreach (var apparel in Scene.GetAllComponentsOfType<ApparelSpriteComponent>())
        {
            var material = apparel.RenderTask.Material;

            if (apparel.NeedsUpdate)
            {
                material.SetUniform("holesCount", apparel.HolesCount);
                material.SetUniform("holes", apparel.Holes);

                    apparel.NeedsUpdate = false;
            }
        }
    }
}
