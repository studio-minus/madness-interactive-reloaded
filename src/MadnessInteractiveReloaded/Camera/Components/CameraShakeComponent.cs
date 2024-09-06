namespace MIR;

using System.Numerics;
using Walgelijk;

/// <summary>
/// Used for making the camera shake.
/// See <see cref="MadnessUtils.Shake(float)"/>
/// </summary>
public class CameraShakeComponent : CameraOffsetComponent
{
    /// <summary>
    /// Rate of camera shake.
    /// </summary>
    public float ShakeFrequency = 70;

    /// <summary>
    /// Controls the shake.
    /// <see cref="MadnessUtils.Shake(float)"/>
    /// </summary>
    public float ShakeIntensity;

    /// <summary>
    /// The shake offset that has been calculated.
    /// </summary>
    public float CalculatedShakeOffset = 0;

    public override Vector2 GetOffset() => new Vector2(0, CalculatedShakeOffset);
}

/// <summary>
/// Use for components that need to add an offset to the camera position.
/// </summary>
public abstract class CameraOffsetComponent : Component
{
    public abstract Vector2 GetOffset();
}