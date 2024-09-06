using System.Numerics;
using Walgelijk;

namespace MIR;

/// <summary>
/// For character's feet.
/// </summary>
public class FootLimb : IInitialOffsetLimb
{
    /// <summary>
    /// Entity of the actual foot sprite
    /// </summary>
    public Entity Entity;

    /// <summary>
    /// World space position
    /// </summary>
    public Vector2 GlobalPosition;

    /// <summary>
    /// Additional world space offset
    /// </summary>
    public Vector2 Offset;

    public Vector2 InitialOffset { get; set; }
}
