namespace MIR;

using System.Numerics;
using Walgelijk;

/// <summary>
/// System for managing the <see cref="TransformConstraintComponent"/>s.
/// </summary>
public class TransformConstraintSystem : Walgelijk.System
{
    public override void PreRender()
    {
        foreach (var constraint in Scene.GetAllComponentsOfType<TransformConstraintComponent>())
        {
            if (!constraint.Enabled)
                continue;

            var entity = constraint.Entity;
            if (constraint.Other.TryGet(Scene, out var other) &&
                Scene.TryGetComponentFrom<TransformComponent>(entity, out var transform))
            {
                if (constraint.LockPosition)
                {
                    transform.Position = Vector2.Transform(new Vector2(
                        constraint.PositionOffset.X * (constraint.XPositionFlip ? -1 : 1),
                        constraint.PositionOffset.Y),
                        other.LocalToWorldMatrix);
                }

                if (constraint.LockRotation)
                    transform.Rotation = other.Rotation + constraint.RotationOffset;

                transform.RecalculateModelMatrix(Matrix3x2.Identity);
            }
        }
    }
}
