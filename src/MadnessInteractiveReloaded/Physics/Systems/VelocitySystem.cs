using System;
using System.Numerics;
using Walgelijk;
using Walgelijk.Physics;

namespace MIR;

/// <summary>
/// <see cref="Walgelijk.System"/> for managing <see cref="VelocityComponent"/>s.
/// </summary>
public class VelocitySystem : Walgelijk.System
{
    private static QueryResult[] resultBuffer = new QueryResult[1];
    private static readonly Vector2[] polygonBuffer = new Vector2[4];

    public override void FixedUpdate()
    {
        if (MadnessUtils.IsPaused(Scene) || MadnessUtils.EditingInExperimentMode(Scene) || MadnessUtils.IsCutscenePlaying(Scene))
        {
            foreach (var v in Scene.GetAllComponentsOfType<VelocityComponent>())
            {
                var transform = Scene.GetComponentFrom<TransformComponent>(v.Entity);
                v.LastPosition = v.Position = transform.Position;
                v.LastRotation = v.Rotation = transform.Rotation;
            }

            return;
        }

        var ph = Scene.GetSystem<PhysicsSystem>();
        const float velocityScale = 0.025f;

        foreach (var v in Scene.GetAllComponentsOfType<VelocityComponent>())
        {
            var entity = v.Entity;
            bool isOnSurface = false;

            if (!v.Enabled)
            {
                var transform = Scene.GetComponentFrom<TransformComponent>(entity);
                v.LastPosition = v.Position = transform.Position;
                v.LastRotation = v.Rotation = transform.Rotation;
                continue;
            }

            var isProjectile = Scene.TryGetComponentFrom<ThrowableProjectileComponent>(entity, out var projectile) && projectile != null;
            var layerMask = isProjectile && projectile != null && !projectile.IsHeavy ? projectile.LayerMask : CollisionLayers.BlockPhysics;

            v.LastPosition = v.Position;
            v.LastRotation = v.Rotation;

                v.Acceleration.Y += projectile != null && !projectile.IsHeavy ? -10 : -120; // gravity

            if (v.OverrideVelocity.HasValue)
            {
                v.Velocity = v.OverrideVelocity.Value;
                v.Acceleration = Vector2.Zero;
                v.OverrideVelocity = null;
            }
            else
            {
                v.Velocity += v.Acceleration;
                v.RotationalVelocity += v.RotationalAcceleration;
            }

            var vel = velocityScale * v.Velocity;
            var velMagnitude = vel.Length();
            var floorNormal = new Vector2(1, 0);

            if (projectile != null && projectile.IsHeavy && velMagnitude > 50)
            {
                if (ph.Raycast(v.Position, vel, out var rh, velMagnitude, projectile.LayerMask, ignore: v.IgnoreCollision))
                {
                    ProcessThrowable(entity, v, projectile, rh);
                    v.Velocity *= 0.8f;
                    vel *= 0.8f;
                    velMagnitude *= 0.8f;
                }
            }

            if (projectile != null && velMagnitude < 20) // remove projectile component if we are going slow
                Scene.DetachComponent<ThrowableProjectileComponent>(projectile.Entity);

            if (ph.Raycast(v.Position, vel, out var raycastHit, velMagnitude, layerMask, ignore: v.IgnoreCollision))
            {
                //if (isProjectile && projectile != null && newVelocity.Length() > 500)
                //    ProcessThrowable(entity, component, transform, projectile, newVelocity, raycastHit);
                //else isProjectile = false;

                if (isProjectile && projectile != null && !projectile.IsHeavy && velMagnitude > 50)
                    ProcessThrowable(entity, v, projectile, raycastHit);
                else
                    isProjectile = false;

                floorNormal = raycastHit.Normal;
               // v.Position = raycastHit.Position + raycastHit.Normal;
                v.RotationalVelocity *= -1;
                v.Velocity =  Vector2.Reflect(v.Velocity, raycastHit.Normal) * Utilities.RandomFloat(0.1f, 0.4f);

                if (v.CollideSounds.Length > 0 && !v.WasOnFloor && velMagnitude > 20) // TODO this threshold should not be hardcoded
                {
                    float volume = Utilities.MapRange(20, 150, 0.2f, 1, velMagnitude);
                    var snd = Utilities.PickRandom(v.CollideSounds);
                    float pitch = Utilities.RandomFloat(0.98f, 1.02f);
                    Audio.PlayOnce(SoundCache.Instance.LoadSoundEffect(snd), new Vector3(v.Position, 0), volume, pitch);
                }

                isOnSurface = true;
            }
            else
            {
                int hit = ph.QueryPoint(v.Position + vel, resultBuffer, layerMask, ignore: v.IgnoreCollision);
                if (hit == 0)
                    v.Position += vel;
                else
                {
                    floorNormal = resultBuffer[0].Collider.SampleNormal(v.Position);
                    isOnSurface = true;
                    v.Position += floorNormal * 5f;
                    v.Velocity = default;
                }
            }

            if (isOnSurface) //TODO kan sneller
            {
                if (isProjectile && projectile != null && projectile.RemoveComponentOnDepletion)
                    Scene.DetachComponent<ThrowableProjectileComponent>(entity);

                var floorAngle = Utilities.NanFallback(Utilities.VectorToAngle(floorNormal) - 90);
                floorAngle += v.FloorAngleOffset;

                var targetAngle = Utilities.Snap(v.Rotation - floorAngle, 180) + floorAngle;

                v.RotationalVelocity += Utilities.DeltaAngle(v.Rotation, targetAngle) * 10;
                v.RotationalVelocity = Utilities.SmoothApproach(v.RotationalVelocity, 0, 25, Time.FixedInterval);

                //v.Velocity = default;
            }

            v.RotationalVelocity *= 0.9995f;
            v.Velocity *= 0.9995f;

            v.Rotation += velocityScale * v.RotationalVelocity;
            v.Acceleration = Vector2.Zero;
            v.RotationalAcceleration = 0;

            v.MeasuredSpeed = (v.Position - v.LastPosition).LengthSquared();
            v.WasOnFloor = isOnSurface;
        }
    }

    public override void Update()
    {
        if (MadnessUtils.IsPaused(Scene) || MadnessUtils.EditingInExperimentMode(Scene) || MadnessUtils.IsCutscenePlaying(Scene))
            return;

        foreach (var component in Scene.GetAllComponentsOfType<VelocityComponent>())
        {
            var entity = component.Entity;

            if (!component.Enabled)
                continue;

            var transform = Scene.GetComponentFrom<TransformComponent>(entity);
            transform.Position = Utilities.Lerp(component.LastPosition, component.Position, Time.Interpolation);
            transform.Rotation = Utilities.Lerp(component.LastRotation, component.Rotation, Time.Interpolation);
        }
    }

    private void ProcessThrowable(Entity ent, VelocityComponent v, ThrowableProjectileComponent projectile, RaycastResult raycastHit)
    {
        bool stuckInBodyBecauseSharp = false;
        if (Scene.TryGetComponentFrom<BodyPartComponent>(raycastHit.Entity, out var bodyPart))
        {
            if (Scene.TryGetComponentFrom<ImpactOffsetComponent>(raycastHit.Entity, out var impactOffset))
                impactOffset.TranslationOffset += raycastHit.Normal * -20;

            var character = bodyPart.Character.Get(Scene);

            if (!character.Flags.HasFlag(CharacterFlags.AttackResponseThrownProjectile))
                return;

            if (character.AnimationConstrainsAll(AnimationConstraint.PreventAllDamage)) // TODO maybe we need a PreventThrownDamage or something
                return;

            float damage = projectile.Damage * 0.009f;

            //het wapen is scherp
            if (projectile.SharpHitboxes.Count > 0)
            {
                projectile.UpdateWorldSharpBoxes(Scene);
                foreach ((Vector2 topLeft, Vector2 topRight, Vector2 bottomLeft, Vector2 bottomRight) in projectile.WorldSharpBoxCache)
                {
                    polygonBuffer[0] = topLeft;
                    polygonBuffer[1] = topRight;
                    polygonBuffer[2] = bottomLeft;
                    polygonBuffer[3] = bottomRight;
                    var center = (topLeft + topRight + bottomLeft + bottomRight) / 4;
                    var closestPoint = raycastHit.Collider.GetNearestPoint(center);
                    var d = MadnessUtils.DistanceToPolygon(polygonBuffer, closestPoint);
                    var isNear = d < 120;
                    if (isNear)
                    {
                        var hitTransform = Scene.GetComponentFrom<TransformComponent>(raycastHit.Entity);
                        for (int i = 0; i < 4; i++)
                            Prefabs.CreateBloodSpurt(Scene, closestPoint, Utilities.VectorToAngle(raycastHit.Normal), character.Look.BloodColour, 1.5f);
                        Audio.PlayOnce(Utilities.PickRandom(Sounds.LivingSwordHit), new Vector3(raycastHit.Position, 0)); //TODO should it always be LivingSwordHit? What if they are dead? 
                        stuckInBodyBecauseSharp = true;
                        if (Scene.TryGetComponentFrom<WeaponComponent>(ent, out var wpn))
                        {
                            wpn.StuckInsideParams = new StuckInsideParameters
                            {
                                Entity = raycastHit.Entity,
                                LocalOffset = Vector2.Transform(v.Position, hitTransform.WorldToLocalMatrix),
                                LocalRotation = v.Rotation - hitTransform.Rotation
                            };
                        }
                        damage *= 80;
                        if (Scene.TryGetComponentFrom<BodyPartShapeComponent>(raycastHit.Entity, out var destructible))
                        {
                            var localPoint = Vector2.Transform(raycastHit.Position, hitTransform.WorldToLocalMatrix);
                            destructible.TryAddHole(localPoint.X, localPoint.Y, 0.1f);
                        }
                    }
                    break;
                }
            }

            if (character.EquippedWeapon.TryGet(Scene, out var weapon))
            {
                var vel = Scene.GetComponentFrom<VelocityComponent>(weapon.Entity);
                character.DropWeapon(Scene);
                MadnessUtils.DelayPausable(0.01f, () =>
                {
                    vel.Acceleration += new Vector2(character.Positioning.IsFlipped ? -1 : 1, 1.2f) * 1500;
                    vel.RotationalAcceleration += Utilities.RandomFloat(-1000, 1000);
                });
            }

            bodyPart.Damage(damage);
            CharacterUtilities.UpdateAliveStatus(Scene, character);
            if (character.IsAlive)
            {
                bool isFacingImpact = character.Positioning.IsFlipped == raycastHit.Normal.X < 0;
                CharacterUtilities.StunHeavy(Scene, character, isFacingImpact);
            }
            else
            {
                // increment player kills stat
                if (Scene.FindAnyComponent<GameModeComponent>(out var mode) && mode.Mode == GameMode.Campaign)
                    if (projectile.Thrower?.TryGet(Scene, out var thrower) ?? false)
                        if (!character.IsAlive && Scene.HasTag(thrower.Entity, Tags.Player) && Level.CurrentLevel != null)
                            CampaignProgress.GetCurrentStats()?.IncrementKills(Level.CurrentLevel);

                var hitTransform = Scene.GetComponentFrom<TransformComponent>(raycastHit.Entity);
                var localPoint = Vector2.Transform(raycastHit.Position, hitTransform.WorldToLocalMatrix);
                if (Scene.TryGetComponentFrom<ShapeComponent>(raycastHit.Entity, out var bodyPartRenderer))
                    if (bodyPartRenderer.HorizontalFlip)
                        localPoint.X *= -1;

                CharacterUtilities.TryStartDeathAnimation(Scene, character, raycastHit.Normal * -1, localPoint, 0.9f);
            }
            v.RotationalVelocity *= -0.3f;

            if (!stuckInBodyBecauseSharp)
                Audio.PlayOnce(Utilities.PickRandom(character.IsAlive ? Sounds.LivingPunch : Sounds.GenericPunch), new Vector3(raycastHit.Position, 0));
        }

        if (stuckInBodyBecauseSharp)
            Scene.DetachComponent<ThrowableProjectileComponent>(ent);
    }
}
