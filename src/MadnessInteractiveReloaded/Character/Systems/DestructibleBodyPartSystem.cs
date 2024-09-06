namespace MIR;

using System;
using System.Numerics;
using Walgelijk;

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

    public static void ResetMaterial(Material material)
    {
        material.SetUniform("holesCount", 0);
        material.SetUniform("holes", Array.Empty<Vector3>()); 
        
        material.SetUniform("slashesCount", 0);
        material.SetUniform("slashes", Array.Empty<Vector3>());

        material.SetUniform("innerCutoutHolesCount", 0);
        material.SetUniform("innerCutoutHoles", Array.Empty<Vector3>());
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
    }
}
