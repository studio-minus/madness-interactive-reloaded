using System.Numerics;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.Onion;
using Walgelijk.Onion.Controls;
using Walgelijk.SimpleDrawing;

namespace MIR;

public class TurretSystem : Walgelijk.System
{
    public override void FixedUpdate()
    {
        if (MadnessUtils.EditingInExperimentMode(Scene) || MadnessUtils.IsPaused(Scene) || MadnessUtils.IsCutscenePlaying(Scene))
            return;

        const float restDistance = 420;
        var dt = Time.FixedInterval * 35;

        foreach (var turret in Scene.GetAllComponentsOfType<TurretComponent>())
        {
            if (!Registries.Factions.TryGet(turret.Faction, out var faction))
                continue;

            if (turret.BarrelNode.LengthSquared() < 1)
                turret.BarrelNodeNext = turret.BarrelNode = restDistance * new Vector2(float.Cos(-turret.AngleRads), float.Sin(-turret.AngleRads));

            turret.BarrelNodeVelocity += dt * turret.BarrelNodeForceAcc;
            turret.BarrelNodeForceAcc = default;
            turret.BarrelNodeNext = turret.BarrelNode + dt * turret.BarrelNodeVelocity;

            if (!turret.Exploded)
            {
                if (turret.Health <= 0)
                {
                    turret.Exploded = true;
                }
                else if (turret.Target.TryGet(Scene, out var target) && target.IsAlive)
                {
                    // aim and kill

                    {
                        var dir = Vector2.Normalize(target.Positioning.Head.GlobalPosition - turret.Position) * turret.BarrelNode.Length();
                        turret.BarrelNodeForceAcc += 0.07f * (dir - turret.BarrelNode);
                    }

                    turret.ShootClock += Time.FixedInterval;
                    if (turret.ShootClock > 0.2f && turret.FindTargetClock > 1)
                    {
                        turret.ShootClock = 0;
                        var dir = Vector2.Normalize(turret.BarrelNode);

                        var origin = turret.Position + new Vector2(float.Cos(-turret.AngleRads), float.Sin(-turret.AngleRads)) * 80;
                        var barrelPos = origin + 420 * dir;
                        var flashPos = origin + (420 + 250) * dir;

                        turret.BarrelNodeForceAcc += Utilities.RandomVector2() * 30;
                        Prefabs.CreateMuzzleFlash(Scene, flashPos, Utilities.VectorToAngle(dir), 2.5f);
                        BulletEmitter.CastBulletRay(new BulletEmitter.BulletParameters
                        {
                            Origin = barrelPos,
                            Direction = dir + Utilities.RandomVector2() * 0.01f,
                            Damage = .8f,
                            ClusterSize = 1,
                            CanBeDeflected = true,
                            EnemyCollisionLayer = faction.AttackHitLayerComposite,
                            CanBeAutoDodged = true
                        });
                        Audio.PlayOnce(Utilities.PickRandom(turret.ShootSounds), pitch: Utilities.RandomFloat(0.7f, 0.75f));

                        //DebugDraw.Cross(barrelPos, 80, Colors.Magenta, 0.2f, RenderOrders.UserInterface);
                        //DebugDraw.Cross(origin, 25, Colors.Magenta, 0.2f, RenderOrders.UserInterface);
                    }
                }
                else
                {
                    // find target
                    if (turret.FindTargetClock > 1)
                    {
                        turret.FindTargetClock = 0;
                        foreach (var potentialVictim in Scene.GetAllComponentsOfType<CharacterComponent>())
                        {
                            if (Utilities.RandomFloat() > 0.5f)
                                continue;
                            if (potentialVictim.IsAlive)
                                if (faction.IsEnemiesWith(potentialVictim.Faction))
                                    turret.Target = potentialVictim;
                        }
                    }

                    // swivel
                    const float idleTime = 0.2f;
                    float t = (turret.Lifespan * 0.5f) % 2;
                    t = t > 1 ? 1 - t % 1 : t;
                    t = Utilities.MapRange(idleTime, 1 - idleTime, 0, 1, t);
                    t = Utilities.Clamp(t);
                    var th = Utilities.MapRange(0, 1, turret.AngleRangeRads.X, turret.AngleRangeRads.Y, t);

                    var tt = turret.BarrelNode.Length() * new Vector2(float.Cos(th), float.Sin(th));
                    turret.BarrelNodeNext += (tt - turret.BarrelNode) * 0.1f;
                }
            }
            else
                turret.BarrelNodeForceAcc += new Vector2(0, -10);

            {
                // angle constraint
                var x = turret.AngleRangeRads.X;
                var y = turret.AngleRangeRads.Y;
                float min = float.Min(x, y);
                float max = float.Max(x, y);
                float mid = (min + max) * 0.5f;
                var len = turret.BarrelNode.Length();
                var midPoint = len * new Vector2(float.Cos(mid), float.Sin(mid));
                var minPoint = len * new Vector2(float.Cos(min), float.Sin(min));
                var maxPoint = len * new Vector2(float.Cos(max), float.Sin(max));

                var maxDistance = Vector2.Distance(midPoint, minPoint);
                var distanceToMidPoint = Vector2.Distance(turret.BarrelNode, midPoint);

                if (distanceToMidPoint > maxDistance)
                {
                    var direction = midPoint - turret.BarrelNode;
                    turret.BarrelNodeNext += Vector2.Normalize(direction) * float.Abs(maxDistance - distanceToMidPoint);
                }
            }

            {
                // distance constraint
                var offset = Vector2.Normalize(turret.BarrelNode) * (restDistance - turret.BarrelNode.Length());
                turret.BarrelNodeNext += offset * 0.9f;
            }

            turret.BarrelNodeVelocity = (turret.BarrelNodeNext - turret.BarrelNode) / dt;
            turret.BarrelNode = turret.BarrelNodeNext;
            if (turret.Exploded)
                turret.BarrelNodeVelocity *= 0.95f;
            else
                turret.BarrelNodeVelocity *= 0.8f;

            turret.Lifespan += Time.FixedInterval;
            turret.FindTargetClock += Time.FixedInterval;

            turret.RenderedAimAngleRads = turret.AimAngleRads;
            turret.AimAngleRads = float.Atan2(turret.BarrelNode.Y, turret.BarrelNode.X);
        }
    }

    public override void Update()
    {
        var body = Assets.Load<Texture>("textures/turrets/default/body.png").Value;
        var head = Assets.Load<Texture>("textures/turrets/default/head.png").Value;

        Draw.Reset();

        foreach (var turret in Scene.GetAllComponentsOfType<TurretComponent>())
        {
            Draw.Order = turret.RenderOrder;
            Draw.Texture = body;
            Draw.Colour = Colors.White;
            Draw.BlendMode = BlendMode.AlphaBlend;

            var th = MadnessUtils.LerpRadians(
                turret.RenderedAimAngleRads, turret.AimAngleRads, Time.Interpolation);

            var o = Draw.TransformMatrix = Matrix3x2.CreateTranslation(turret.Position) * Matrix3x2.CreateRotation(-turret.AngleRads, turret.Position);
            Draw.Quad(new Rect(default, body.Size));
            Draw.TransformMatrix = Matrix3x2.CreateRotation(th + turret.AngleRads, default)
                * Matrix3x2.CreateTranslation(body.Width * 0.5f, -6) * Draw.TransformMatrix;
            Draw.Texture = head;
            Draw.Order = Draw.Order.OffsetOrder(-1);
            Draw.Quad(new Rect(default, head.Size).Translate(head.Width * 0.5f - 85, -3));

            if (!turret.Exploded)
            {
                var hasTarget = turret.Target.IsValid(Scene);
                float ping = hasTarget ? ((turret.Lifespan * 4) % 1) : (turret.Lifespan % 1);
                float s = hasTarget ? 50 : 25;
                Draw.Order = Draw.Order.OffsetOrder(1);
                Draw.ResetTexture();
                Draw.BlendMode = BlendMode.Addition;
                Draw.Colour = Colors.Red.WithAlpha(1 - Easings.Expo.Out(ping));
                Draw.Circle(new Vector2(375, 42), new(s * Easings.Expo.Out(ping)));
            }
        }
    }
}