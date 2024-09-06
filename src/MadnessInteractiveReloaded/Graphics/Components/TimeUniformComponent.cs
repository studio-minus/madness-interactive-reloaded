using Walgelijk;

namespace MIR;

/// <summary>
/// Will set <see cref="UniformName"/> to <see cref="Time"/> on <see cref="Material"/>.
/// </summary>
public class TimeUniformComponent : Component
{
    /// <summary>
    /// The shader uniform, "time" by default.
    /// </summary>
    public string UniformName = "time";

    /// <summary>
    /// Current time in seconds.
    /// </summary>
    public float Time;

    /// <summary>
    /// The scale of time.
    /// </summary>
    public float Speed = 1;

    /// <summary>
    /// What material to set the uniform of.
    /// </summary>
    public Material Material;

    public TimeUniformComponent(Material material)
    {
        Material = material;
    }
}
