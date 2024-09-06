using System.Numerics;

namespace MIR;

/// <summary>
/// The core Body limb for an animated character
/// </summary>
public class BodyAnimatedLimb : AnimatedLimb, IInitialOffsetLimb
{
    public Vector2 ComputedVisualCenter;

    public Vector2 InitialOffset { get; set; }
}
