namespace MIR;
using Walgelijk;

/// <summary>
/// A <see cref="Component"/> for animating something.
/// See <see cref="AnimationSystem"/>
/// </summary>
[RequiresComponents(typeof(TransformComponent))]
public class AnimationComponent : Component
{
    /// <summary>
    /// The <see cref="Vec2Curve"/> for translation.
    /// </summary>
    public Vec2Curve? Translational;
    
    /// <summary>
    /// The <see cref="AngleCurve"/> for rotation.
    /// </summary>
    public AngleCurve? Rotational;

    /// <summary>
    /// The <see cref="Vec2Curve"/> for scaling.
    /// </summary>
    public Vec2Curve? Scaling;

    /// <summary>
    /// The <see cref="FloatCurve"/> for defining visibility.
    /// </summary>
    public FloatCurve? Visibility;

    /// <summary>
    /// The <see cref="ColorCurve"/> for defining color tint.
    /// </summary>
    public ColorCurve? Tint;

    /// <summary>
    /// Is the animation currently playing?
    /// </summary>
    public bool IsPlaying;

    /// <summary>
    /// Does this animation loop?
    /// </summary>
    public bool Loops;

    /// <summary>
    /// Is the animation reversed?
    /// </summary>
    public bool Reversed = false;

    /// <summary>
    /// How long (in seconds) the animation is.
    /// </summary>
    public float Duration = 1;

    /// <summary>
    /// Used when setting the <see cref="Material"/>'s shader tint uniform when a <see cref="Tint"/> curve is present.
    /// </summary>
    public string TintUniformName = "tint";

    /// <summary>
    /// The current time of the animation.
    /// </summary>
    public float CurrentPlaybackTime;

    /// <summary>
    /// The <see cref="AnimationInterpolationMode"/> for the animation.
    /// </summary>
    public AnimationInterpolationMode InterpolationMode = AnimationInterpolationMode.Linear;
}
