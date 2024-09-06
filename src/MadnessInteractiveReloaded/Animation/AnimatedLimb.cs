using System.Numerics;
using Walgelijk;

namespace MIR;

/// <summary>
/// Base limb for an animated character.
/// <see cref="HeadAnimatedLimb"/> Hands,
/// <see cref="HandAnimatedLimb"/> limbs, and
/// <see cref="BodyAnimatedLimb"/> bodies.
/// </summary>
public class AnimatedLimb
{
    /// <summary>
    /// Entity of the limb
    /// </summary>
    public Entity Entity;

    /// <summary>
    /// World space position of the limb
    /// </summary>
    public Vector2 GlobalPosition;

    /// <summary>
    /// World space rotation of the limb
    /// </summary>
    public float GlobalRotation;

    /// <summary>
    /// The translational offset as decided by the animation
    /// </summary>
    public Vector2 AnimationPosition;

    /// <summary>
    /// The rotational offset as decided by the animation
    /// </summary>
    public float AnimationAngle;

    /// <summary>
    /// Absolute scale of the transform as determined by the texture
    /// </summary>
    public Vector2 Scale;
}
