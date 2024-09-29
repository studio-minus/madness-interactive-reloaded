using System.Numerics;
using Walgelijk;

namespace MIR;

/// <summary>
/// Link two <see cref="VerletNodeComponent"/>s.
/// </summary>
public class VerletLinkComponent : Component
{
    /// <summary>
    /// The first node.
    /// </summary>
    public VerletNodeComponent A;

    /// <summary>
    /// The other node.
    /// </summary>
    public VerletNodeComponent B;

    /// <summary>
    /// The preferred distance for the nodes to behave with eachother according to
    /// <see cref="Mode"/>.
    /// </summary>
    public float TargetDistance = 500;

    /// <summary>
    /// How the nodes will be effected via <see cref="TargetDistance"/>.
    /// </summary>
    public VerletLinkMode Mode = VerletLinkMode.KeepDistance;

    /// <summary>
    /// Only applicable if <see cref="Mode"/> is <see cref="VerletLinkMode.MinMaxDistance"/>
    /// </summary>
    public Vector2 MinMaxDistance = default;

    public VerletLinkComponent(VerletNodeComponent a, VerletNodeComponent b, float distance)
    {
        A = a;
        B = b;
        TargetDistance = distance;
    }

    public VerletLinkComponent(VerletNodeComponent a, VerletNodeComponent b, VerletLinkMode linkMode = VerletLinkMode.KeepDistance)
    {
        A = a;
        B = b;
        TargetDistance = Vector2.Distance(a.Position, b.Position);
        Mode = linkMode;
    }
}

public class VerletTransformComponent : Component
{
    public VerletNodeComponent Up;
    public VerletNodeComponent Center;

    public TransformComponent Transform;

    public Vector2 LocalOffset;
    public float LocalRotationalOffset;

    public VerletTransformComponent(
        Entity entity,
        TransformComponent transform,
        VerletNodeComponent up,
        VerletNodeComponent center)
    {
        Up = up;
        Center = center;
        Transform = transform;

        var upVector = MadnessVector2.Normalize(up.Position - center.Position);
        var rightVector = new Vector2(upVector.Y, -upVector.X);

        LocalRotationalOffset = transform.Rotation - Utilities.VectorToAngle(rightVector);
        LocalOffset = Vector2.TransformNormal(transform.Position - center.Position, transform.WorldToLocalMatrix);
    }
}
