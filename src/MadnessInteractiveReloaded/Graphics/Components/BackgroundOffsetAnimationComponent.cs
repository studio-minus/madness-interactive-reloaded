namespace MIR;

using Walgelijk;

/// <summary>
/// For animating background movement.
/// </summary>
[RequiresComponents(typeof(Background.BackgroundComponent))]
public class BackgroundOffsetAnimationComponent : Component
{
    /// <summary>
    /// Should the animation be playing?
    /// </summary>
    public bool IsPlaying = true;

    /// <summary>
    /// Does the animation loop?
    /// </summary>
    public bool Loops;

    /// <summary>
    /// Should the animation be reversed?
    /// </summary>
    public bool Reversed;

    /// <summary>
    /// If the animation fills towards the right side of the screen and stops.
    /// </summary>
    public bool FillForwards = true;

    /// <summary>
    /// If the animation speed is affected by the <see cref="Time.TimeScale"/>. If off, also plays while the scene is paused.
    /// </summary>
    public bool AffectedByTimeScale = true;

    /// <summary>
    /// How long the animation has played.
    /// </summary>
    public float CurrentPlaybackTime;

    /// <summary>
    /// The duration of the animation.
    /// </summary>
    public float Duration = 0.3f;

    /// <summary>
    /// The curve to animate along.
    /// </summary>
    public Vec2Curve? OffsetCurve;
}
