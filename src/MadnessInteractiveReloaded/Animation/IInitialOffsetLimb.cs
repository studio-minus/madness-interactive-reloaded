using System.Numerics;

namespace MIR;

/// <summary>
/// Interface for <see cref="AnimatedLimb"/> that provides an <see cref="InitialOffset"/> member, 
/// which is the offset from the character center to the limb position at instantiation
/// </summary>
public interface IInitialOffsetLimb
{
    /// <summary>
    /// The offset that this was created with
    /// </summary>
    public Vector2 InitialOffset { get; set; }
}
