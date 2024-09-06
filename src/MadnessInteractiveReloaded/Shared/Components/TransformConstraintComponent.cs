using System.Numerics;
using Walgelijk;

namespace MIR;

/// <summary>
/// Constrain this entity to another entity.
/// </summary>
public class TransformConstraintComponent : Component
{
    /// <summary>
    /// If the constraint is active.
    /// </summary>
    public bool Enabled = true;

    /// <summary>
    /// The other thing to constrain to.
    /// </summary>
    public ComponentRef<TransformComponent> Other;

    /// <summary>
    /// If the transform of this entity should be locked to <see cref="Other"/><br></br>
    /// plus <see cref="PositionOffset"/>.
    /// </summary>
    public bool LockPosition;

    /// <summary>
    /// What to offset the locked position of this entity from <see cref="Other"/> by.
    /// </summary>
    public Vector2 PositionOffset;

    /// <summary>
    /// If this entity should have its rotation locked to <see cref="Other"/>'s rotation.
    /// </summary>
    public bool LockRotation;

    /// <summary>
    /// How much to offset the rotation of this entity from <see cref="Other"/>'s.
    /// </summary>
    public float RotationOffset;

    /// <summary>
    /// If the x component of the position should be inverted.
    /// </summary>
    public bool XPositionFlip;

    /// <summary>
    /// If the y component of the position should be inverted.
    /// </summary>
    public bool YPositionFlip;
}
