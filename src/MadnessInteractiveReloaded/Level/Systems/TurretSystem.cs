using System.Numerics;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.SimpleDrawing;

namespace MIR;

public class TurretSystem : Walgelijk.System
{
    public override void FixedUpdate()
    {
        if (MadnessUtils.EditingInExperimentMode(Scene) || MadnessUtils.IsPaused(Scene) || MadnessUtils.IsCutscenePlaying(Scene))
            return;

        foreach (var turret in Scene.GetAllComponentsOfType<TurretComponent>())
        {
            if (!Registries.Factions.TryGet(turret.Faction, out var faction))
                continue;

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
                        var dir = target.Positioning.Head.GlobalPosition - turret.Position;
                        float th = Utilities.Snap(float.Atan2(dir.Y, dir.X) + turret.AngleRads, float.Tau / 30);
                        turret.AimAnglePredictedRads = float.Lerp(turret.AimAnglePredictedRads, th, 0.1f);
                    }

                    turret.ShootClock += Time.FixedInterval;
                    if (turret.ShootClock > 0.2f && turret.FindTargetClock > 1)
                    {
                        turret.ShootClock = 0;
                        var th = (turret.AimAngleRads - turret.AngleRads);
                        var dir = new Vector2(float.Cos(th), float.Sin(th));

                        var origin = turret.Position + new Vector2(
                            float.Cos(-turret.AngleRads), 
                            float.Sin(-turret.AngleRads)) * 80;
                        var barrelPos = origin + 420 * dir;
                        var flashPos = origin + (420 + 250) * dir;

                        turret.AimAngleVelocity += Utilities.RandomFloat(-1f, 1f) * 0.05f;
                        Prefabs.CreateMuzzleFlash(Scene, flashPos, float.RadiansToDegrees(th), 2.5f);
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
                    turret.AimAnglePredictedRads = MadnessUtils.LerpRadians(turret.AimAnglePredictedRads, th, 0.5f);
                }
            }
            else
            {
                turret.AimAnglePredictedRads += (0 - turret.AimAngleRads) * 0.05f ;
            }

            turret.RenderedAimAngleRads = turret.AimAngleRads;
            turret.AimAngleVelocity += (turret.AimAnglePredictedRads - turret.AimAngleRads) * 0.4f;
            turret.AimAngleRads += float.Clamp(turret.AimAngleVelocity, -0.1f, 0.1f);
            turret.AimAngleVelocity *= 0.85f;

            turret.Lifespan += Time.FixedInterval;
            turret.FindTargetClock += Time.FixedInterval;
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

            var th = float.Lerp(turret.RenderedAimAngleRads, turret.AimAngleRads, Time.Interpolation);

            var o = Draw.TransformMatrix = Matrix3x2.CreateTranslation(turret.Position) * Matrix3x2.CreateRotation(-turret.AngleRads, turret.Position);
            Draw.Quad(new Rect(default, body.Size));
            Draw.TransformMatrix = Matrix3x2.CreateRotation(th, default) * Matrix3x2.CreateTranslation(body.Width * 0.5f, -6) * Draw.TransformMatrix;
            Draw.Texture = head;
            Draw.Order = Draw.Order.OffsetOrder(-1);
            Draw.Quad(new Rect(default, head.Size).Translate(head.Width * 0.5f - 85, -3));
        }
    }
}