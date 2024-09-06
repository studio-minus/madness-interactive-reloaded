using System.Numerics;
using Walgelijk;

namespace MIR;

/// <summary>
/// <see cref="AnimatedLimb"/> for hands.
/// </summary>
public class HandAnimatedLimb : AnimatedLimb, IInitialOffsetLimb
{
    public bool IsLeftHand;
    public HandLook Look;
    public bool ShouldFollowRecoil;
    public HandLook? AnimatedHandLook;

    public Vector2 PreviousAnimatedPosition;
    public float PreviousAnimatedAngle;

    public Vector2 PosePosition;
    public float PoseRotation;

    /// <summary>
    /// This is just the <see cref="RenderOrder"/> that was last applied. Writing to this won't actually change it.
    /// </summary>
    public RenderOrder ApparentRenderOrder;

    public Vector2 InitialOffset { get; set; }
}