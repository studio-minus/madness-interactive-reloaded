using System.Numerics;
using Walgelijk;
using Walgelijk.AssetManager;

namespace MIR;

/// <summary>
/// A weapon part that gets animated, like slides on pistols.
/// </summary>
public class AnimatedWeaponPart
{
    public float OutOfAmmoKeyframeTime = 0.1f;
    public bool InvisbleWhenOutOfAmmo = false;
    public float Duration = 0.15f;
    public Vector2 Scale = new(1, 1);
    public AssetRef<Texture> Texture;
    public Vec2Curve? TranslationCurve;
    public FloatCurve? AngleCurve;
    public FloatCurve? VisibilityCurve;

    public AnimatedWeaponPart(GlobalAssetId texture)
    {
        Texture = new(texture);
    }
}