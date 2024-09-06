using System.Collections.Generic;
using System.Numerics;
using Walgelijk;
using Walgelijk.AssetManager;

namespace MIR;

/// <summary>
/// Component for giving an entity physical velocity.
/// <br></br>
/// Includes rotation, acceleration, rotational acceleration and rotational velocity.
/// </summary>
[RequiresComponents(typeof(TransformComponent), typeof(Walgelijk.Physics.PhysicsBodyComponent))]
public class VelocityComponent : Component
{
    public VelocityComponent()
    {

    }

    public VelocityComponent(TransformComponent initial)
    {
        LastPosition = Position = initial.Position;
        LastRotation = Rotation = initial.Rotation;
    }

    public bool Enabled;

    public Vector2 LastPosition;
    public Vector2 Position;

    public float LastRotation;
    public float Rotation;

    public Vector2 Acceleration;
    public Vector2 Velocity;

    public float RotationalAcceleration;
    public float RotationalVelocity;

    public float FloorAngleOffset = 0;

    public Vector2? OverrideVelocity;
    public float MeasuredSpeed;

    public bool WasOnFloor;

    public AssetRef<FixedAudioData>[] CollideSounds = Sounds.FirearmCollision;

    public HashSet<Entity> IgnoreCollision = [];
}