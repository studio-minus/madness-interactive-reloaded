namespace MIR;

using System.Numerics;
using Walgelijk;

/// <summary>
/// For rendering a decal.
/// </summary>
public class DecalComponent : Component
{
    /// <summary>
    /// <see cref="DecalType.Blood"/>
    /// or
    /// <see cref="DecalType.BulletHole"/>
    /// </summary>
    public DecalType DecalType;

    /// <summary>
    /// In the case of decals, 
    /// simply points to which column of the sprite sheet
    /// to use. Selected at random.
    /// </summary>
    public int FlipbookIndex;

    /// <summary>
    /// Where the decal is in the world.
    /// </summary>
    public Vector2 Position;

    /// <summary>
    /// What color to render the decal.
    /// The sprites are greyscale so you can tint them whatever you want.
    /// </summary>
    public Color Color;

    /// <summary>
    /// Rotation in degrees.
    /// </summary>
    public float RotationDegrees;

    /// <summary>
    /// How big is the decal? Defaults to (1,1).
    /// </summary>
    public Vector2 Scale = new Vector2(1);

    public RenderOrder RenderOrder = RenderOrders.BackgroundInFront.WithOrder(50);

    /// <summary>
    /// Get the texture for the two <see cref="DecalType"/>s.
    /// </summary>
    /// <param name="t"></param>
    /// <returns>A tuple of (<see cref="int"/> columns,<see cref="Texture"/> texture) of information about the respective texture.
    /// Only works for the built-in bullet hole and blood textures.</returns>
    public static (int columns, Texture tex) GetTextureForType(DecalType t)
    {
        switch (t)
        {
            case DecalType.Blood:
                return (10, Textures.Decals.BloodSplat);
            case DecalType.BulletHole:
                return (5, Textures.Decals.BulletHole);
            default:
                Logger.Warn("Unknown decal type texture requested...");
                return (1, Texture.ErrorTexture);
        }
    }
}
