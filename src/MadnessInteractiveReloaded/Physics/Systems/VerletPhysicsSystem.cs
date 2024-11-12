using System;
using System.Numerics;
using Walgelijk;
using Walgelijk.Physics;
using Walgelijk.SimpleDrawing;

namespace MIR;

/// <summary>
/// Physics solver system using Verlet integration: https://en.wikipedia.org/wiki/Verlet_integration.
/// <br></br>
/// Walgelijk's <see cref="Walgelijk.Physics.PhysicsSystem"/> does not perform any collision resolutions, 
/// it's just used for overlap queries and raycasts. 
/// We write our own resolver (this System) in game code.
/// </summary>
public class VerletPhysicsSystem : Walgelijk.System
{
    /// <summary>
    /// Whether or not to show debug overlay information for the solver.
    /// </summary>
    public bool DrawDebug = false;

    private readonly FixedIntervalDistributor distributor = new()
    {
        Rate = 200,
        //MaxRate = 64
    };

    private const int MaxNodes = 256;
    private static readonly VerletNodeComponent[] nodeBuffer = new VerletNodeComponent[MaxNodes];
    private static readonly VerletLinkComponent[] linkBuffer = new VerletLinkComponent[MaxNodes];
    private static readonly VerletTransformComponent[] transformLinkBuffer = new VerletTransformComponent[MaxNodes];

    private static QueryResult[] resultBuffer = new QueryResult[1];

    public override void Update()
    {
        if (MadnessUtils.IsPaused(Scene) || MadnessUtils.EditingInExperimentMode(Scene) || MadnessUtils.IsCutscenePlaying(Scene))
            return;

        var ph = Scene.GetSystem<PhysicsSystem>();

        var cycles = distributor.CalculateCycleCount(Time.DeltaTime);
        cycles = Utilities.Clamp(cycles, 0, 256);

        int nodeCount = Scene.CopyAllComponentsOfType(nodeBuffer);
        int linkCount = Scene.CopyAllComponentsOfType(linkBuffer);
        int transformLinkCount = Scene.CopyAllComponentsOfType(transformLinkBuffer);

        for (int i = 0; i < cycles; i++)
        {
            for (int p = 0; p < nodeCount; p++)
                SolveMotion(ph, nodeBuffer[p]);

            for (int p = 0; p < linkCount; p++)
                SolveConstraints(linkBuffer[p]);

            if (DrawDebug)
                if (Input.IsButtonHeld(MouseButton.Left))
                    for (int vv = 0; vv < nodeCount; vv++)
                    {
                        var node = nodeBuffer[vv];
                        if (Vector2.Distance(Input.WorldMousePosition, node.Position) < 400)
                        {
                            node.Acceleration -= node.PreviousVelocity * 0.02f;
                            node.Acceleration += 1 * Vector2.Normalize(Input.WorldMousePosition - node.Position);
                        }
                    }
        }

        for (int p = 0; p < transformLinkCount; p++)
            SolveTripleTransformLinks(transformLinkBuffer[p]);
    }

    public override void Render()
    {
        if (!DrawDebug)
            return;

        Draw.Reset();
        Draw.Order = RenderOrder.Top;

        foreach (var node in Scene.GetAllComponentsOfType<VerletNodeComponent>())
        {
            Draw.Colour = Colors.Magenta.WithAlpha(0.1f);
            Draw.Circle(node.Position, new Vector2(node.Radius));
        }

        foreach (var link in Scene.GetAllComponentsOfType<VerletLinkComponent>())
        {
            Draw.Colour = Colors.Green.WithAlpha(0.5f);
            Draw.Line(link.A.Position, link.B.Position, 4);

            var direction = Vector2.Normalize(link.B.Position - link.A.Position);

            switch (link.Mode)
            {
                case VerletLinkMode.KeepDistance:
                case VerletLinkMode.MaxDistanceOnly:
                case VerletLinkMode.MinDistanceOnly:
                    Draw.Colour = Colors.Red;
                    Draw.Circle(link.A.Position + direction * link.TargetDistance, new Vector2(5));
                    break;
                case VerletLinkMode.MinMaxDistance:
                    Draw.Colour = Colors.Cyan;
                    Draw.Circle(link.A.Position + direction * link.MinMaxDistance.X, new Vector2(9));

                    Draw.Colour = Colors.Orange;
                    Draw.Circle(link.A.Position + direction * link.MinMaxDistance.Y, new Vector2(9));
                    break;
            }
        }
    }

    private void SolveMotion(PhysicsSystem ph, VerletNodeComponent node)
    {
        //TODO dit kan allemaal veel sneller :(

        node.Position = Utilities.NanFallback(node.Position);
        node.PreviousPosition = Utilities.NanFallback(node.PreviousPosition);

        var velocity = node.Position - node.PreviousPosition;
        node.PreviousPosition = node.Position;

        //var speed = velocity.Length();
        //if (speed > 40)
        //    velocity = velocity / speed * 40;

        velocity += node.Acceleration * 2;
       velocity += VerletNodeComponent.Gravity * distributor.Interval * 3f;

        //hit wall
        if (ph.Raycast(node.Position, velocity, out var raycastHit, float.Max(velocity.Length(), node.Radius), CollisionLayers.BlockPhysics))
        {
            // DebugDraw.Cross(raycastHit.Position, 8, Colors.Magenta, 0.5f);

            velocity = velocity.ProjectOnPlane(raycastHit.Normal);
            node.PreviousPosition = node.Position = raycastHit.Collider.GetNearestPoint(node.Position) + raycastHit.Normal * node.Radius;
            node.Position += velocity * 0.9f;
        }
        else
        {
            //is inside wall
            int hit = ph.QueryCircle(node.Position + velocity, node.Radius, resultBuffer, CollisionLayers.BlockPhysics);
            if (hit == 0)
            {
                node.Position += velocity;
            }
            else
            {
                Vector2 normal = default;
                int it = 0;
                while (it < 100 && ph.QueryCircle(node.Position + velocity, node.Radius, resultBuffer, CollisionLayers.BlockPhysics) > 0)
                {
                    normal = resultBuffer[0].Collider.SampleNormal(node.Position);
                    node.Position += normal * 0.5f;
                    it++;
                }
                node.PreviousPosition = node.Position;
                velocity = velocity.ProjectOnPlane(normal);
                node.Position += velocity * (1 - node.Friction);
            }
        }

        node.Acceleration = Vector2.Zero;

        if (velocity.LengthSquared() < 10 && (node.PreviousVelocity - velocity).LengthSquared() > 65)
            Audio.PlayOnce(Utilities.PickRandom(Sounds.SoftBodyImpact), 0.4f, 1, AudioTracks.SoundEffects);

        node.PreviousVelocity = velocity;
    }

    private void SolveConstraints(VerletLinkComponent link)
    {
        var a = link.A;
        var b = link.B;
        float aInfluence = b.Mass / (a.Mass + b.Mass);
        float bInfluence = 1 - aInfluence;

        var distance = (a.Position - b.Position).Length();
        var targetDistance = link.TargetDistance;
        switch (link.Mode)
        {
            case VerletLinkMode.MaxDistanceOnly:
                if (distance <= targetDistance)
                    return;
                break;
            case VerletLinkMode.MinDistanceOnly:
                if (distance > targetDistance)
                    return;
                break;
            case VerletLinkMode.MinMaxDistance:
                if (distance > link.MinMaxDistance.X && distance < link.MinMaxDistance.Y)
                    return;
                if (distance < link.MinMaxDistance.X)
                    targetDistance = link.MinMaxDistance.X;
                else
                    targetDistance = link.MinMaxDistance.Y;
                break;
        }

        var m = (a.Position + b.Position) / 2;
        var diff = (distance - targetDistance) * 0.45f;
        a.Position += diff * Vector2.Normalize(m - a.Position) * aInfluence;
        var bD = Vector2.Normalize(m - b.Position);
        b.Position += diff * bD * bInfluence;
        var targetRot = MathF.Atan2(bD.Y, bD.X) * Utilities.RadToDeg + link.B.DirectionOffset;
        b.RotationOffset = targetRot;
    }

    private void SolveTripleTransformLinks(VerletTransformComponent link)
    {
        var up = link.Up;
        var ct = link.Center;

        var transform = link.Transform;

        var center = ct.Position;
        var upVector = Vector2.Normalize(up.Position - center);
        var rightVector = new Vector2(upVector.Y, -upVector.X);

        var angle = Utilities.VectorToAngle(rightVector);

        transform.Rotation = Utilities.SmoothAngleApproach(transform.Rotation, link.LocalRotationalOffset + angle, 35, Time.DeltaTime);
        transform.Position = Utilities.SmoothApproach(transform.Position, center + Vector2.TransformNormal(link.LocalOffset, transform.LocalToWorldMatrix), 35, Time.DeltaTime) + link.GlobalOffset;
    }
}
