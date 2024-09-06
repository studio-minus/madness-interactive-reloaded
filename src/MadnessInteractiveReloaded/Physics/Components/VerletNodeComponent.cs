using System.Numerics;
using Walgelijk;

namespace MIR;

/// <summary>
/// A component for an entity to be simulated by the <see cref="VerletPhysicsSystem"/>.
/// </summary>
public class VerletNodeComponent : Component
{
    /// <summary>
    /// The position in the simulation (world).
    /// </summary>
    public Vector2 Position;

    /// <summary>
    /// The position last simulation tick.
    /// </summary>
    public Vector2 PreviousPosition;

    /// <summary>
    /// The acceleration of the node.
    /// </summary>
    public Vector2 Acceleration;
    
    /// <summary>
    /// The velocity from the previous tick.
    /// </summary>
    public Vector2 PreviousVelocity;
    
    /// <summary>
    /// How much friction this object has, resistance to acceleration.
    /// </summary>
    public float Friction = 0;

    /// <summary>
    /// The radius for the node's circle collider.
    /// </summary>
    public float Radius = 50;

    /// <summary>
    /// The force needed to move the node.
    /// </summary>
    public float Mass = 1;

    public float DirectionOffset = 90;
    
    public static Vector2 Gravity = new(0, -20);

    public float RotationOffset = 0;

    public VerletNodeComponent(Vector2 position, float radius, float mass = 1) : this(position)
    {
        Radius = radius;
        Mass = mass;
    }

    public VerletNodeComponent(Vector2 position)
    {
        Position = position;
        PreviousPosition = position;
    }
}
