using System.Numerics;

namespace MIR;

/// <summary>
/// <see cref="AnimatedLimb"/> specifically for heads
/// </summary>
public class HeadAnimatedLimb : AnimatedLimb
{
    /// <summary>
    /// Normalised direction that he head should be pointing towards
    /// </summary>
    public Vector2 Direction;

    /// <summary>
    /// The offset applied based on where the character is looking
    /// </summary>
    public Vector2 LookOffset;

    /// <summary>
    /// Walk bobbing offset
    /// </summary>
    public Vector2 BobOffset;
}
