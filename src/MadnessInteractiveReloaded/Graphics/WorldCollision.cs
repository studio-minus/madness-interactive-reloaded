using System;
using System.Numerics;
using Walgelijk;
using Walgelijk.ParticleSystem;
using Walgelijk.Physics;

namespace MIR;

/// <summary>
/// For simple particle collisions.
/// </summary>
public struct WorldCollision : IParticleModule
{
    public bool Disabled { get; set; }

    /// <summary>
    /// How bouncey is the object?
    /// </summary>
    public float BounceFactor = 0.4f;

    /// <summary>
    /// Dampens the collision response.
    /// </summary>
    public float DampeningFactor = 0.9f;

    /// <summary>
    /// Invoked when the collision happens.
    /// </summary>
    public Hook<Particle> OnCollide = new();

    public Scene Scene;

    /// <summary>
    /// What collision mask to collide with.
    /// </summary>
    public uint CollisionMask = int.MaxValue;

    private QueryResult[] buffer = new QueryResult[1];

    public WorldCollision(float bounceFactor, float dampeningFactor, Scene scene) : this()
    {
        BounceFactor = bounceFactor;
        DampeningFactor = dampeningFactor;
        Scene = scene;
    }

    public void Process(int index, ref Particle particle, in GameState gameState, ParticlesComponent component, TransformComponent transform)
    {
        const float radius = 5;
        var phys = Scene.GetSystem<PhysicsSystem>();

        var offset = new Vector2(0, (Utilities.Hash(index * 4523.52334f) * 2 - 1) * 35);

        offset *= float.Clamp(Utilities.MapRange(0, 0.1f, 0, 1, particle.NormalisedLife), 0, 1);

        particle.Position -= offset;

        var count = phys.QueryPoint(particle.Position, buffer, CollisionMask);
        if (count > 0)
        {
            int maxIterations = 1200;
            while (maxIterations > 0)
            {
                maxIterations--;
                particle.Position += buffer[0].Collider.SampleNormal(particle.Position) * 10;
                if (!buffer[0].Collider.IsPointInside(particle.Position))
                    break;
            }
        }
        else
        {
            var vel = particle.Velocity;

            if (MathF.Abs(vel.X) <= float.Epsilon && MathF.Abs(vel.Y) <= float.Epsilon)
                vel = -Vector2.UnitY;
            var maxDist = MathF.Max(30, vel.Length() * gameState.Time.DeltaTime);

            var isHit = phys.Raycast(particle.Position, vel, out var hit, maxDistance: maxDist, filter: CollisionMask);
            if (isHit)
            {
                //var hit = buffer[0];
                var other = hit.Position;
                var normal = hit.Normal;

                if (particle.Velocity.LengthSquared() > 4)
                {
                    particle.RotationalVelocity *= -1;
                    OnCollide.Dispatch(particle);
                }
                particle.Rotation = Utilities.VectorToAngle(normal) + 90;

                particle.Position = other + normal * radius;
                particle.Velocity = Vector2.Reflect(particle.Velocity, normal) * BounceFactor;
                particle.RotationalVelocity *= 0.5f;
            }
        }

        particle.Position += offset;
    }
}
