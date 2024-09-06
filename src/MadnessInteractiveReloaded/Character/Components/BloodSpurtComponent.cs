namespace MIR;

using Walgelijk;

/// <summary>
/// For making blood spurt effects.
/// See <see cref="Prefabs.CreateBloodSpurt(Scene, System.Numerics.Vector2, float, Color, float)"/>
/// </summary>
public class BloodSpurtComponent : Component
{
    public BloodSpurtTexture FrameSheet;
    public Material Material;

    public BloodSpurtComponent(BloodSpurtTexture frameSheet, Material material)
    {
        FrameSheet = frameSheet;
        Material = material;
    }
}