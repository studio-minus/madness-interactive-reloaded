namespace MIR;

using Walgelijk;

/// <summary>
/// Set material "time" uniforms every frame to scaled <see cref="Time.DeltaTime"/>.
/// </summary>
public class TimeUniformSystem : Walgelijk.System
{
    public override void Render()
    {
        if (MadnessUtils.IsPaused(Scene) || MadnessUtils.EditingInExperimentMode(Scene))
            return;

        foreach (var comp in Scene.GetAllComponentsOfType<TimeUniformComponent>())
        {
            comp.Time += Time.DeltaTime * Time.TimeScale * comp.Speed;

            comp.Material.SetUniform(comp.UniformName, comp.Time);
        }
    }
}
