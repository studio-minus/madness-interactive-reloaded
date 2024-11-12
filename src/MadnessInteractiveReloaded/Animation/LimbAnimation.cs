namespace MIR;

using Walgelijk;

/// <summary>
/// Animation specific to character limbs
/// </summary>
public class LimbAnimation
{
    public float Duration = 0;
    public bool AdjustForAim = false;
    public Vec2Curve? TranslationCurve = null;
    public AngleCurve? RotationCurve = null;
    public Vec2Curve? ScaleCurve = null;
}

