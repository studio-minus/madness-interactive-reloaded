using System;
using System.Collections;
using System.Numerics;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.ParticleSystem;
using Walgelijk.Physics;

namespace MIR;

/// <summary>
/// Manage <see cref="WeaponComponent"/>s.
/// </summary>
public class WeaponSystem : Walgelijk.System
{
    private static ParticlesComponent? GetParticlesFor(EjectionParticle ejectionParticle, Scene scene)
    {
        if (scene.FindAnyComponent<CasingParticleDictComponent>(out var dict))
            if (dict.EntityByParticle.TryGetValue(ejectionParticle, out var ent))
                return scene.GetComponentFrom<ParticlesComponent>(ent);

        return null;
    }

    public override void Update()
    {
        if (MadnessUtils.IsPaused(Scene) || MadnessUtils.EditingInExperimentMode(Scene))
            return;

        foreach (var weapon in Scene.GetAllComponentsOfType<WeaponComponent>())
        {
            var entity = weapon.Entity;
            var data = weapon.Data;
            var transform = Scene.GetComponentFrom<TransformComponent>(entity);
            var velocityComponent = Scene.GetComponentFrom<VelocityComponent>(entity);

            velocityComponent.Enabled = !weapon.Wielder.IsValid(Scene) && !weapon.StuckInsideParams.HasValue && !weapon.IsAttachedToWall;

            if (weapon.IsAttachedToWall)
            {
                if (Scene.TryGetComponentFrom<DespawnComponent>(entity, out var weaponDespawner))
                    weaponDespawner.Timer = 0;
            }
            else if (weapon.Wielder.TryGet(Scene, out var wielder))
            {
                weapon.Timer += Time.DeltaTime;
                weapon.IsAttachedToWall = false;

                switch (data.WeaponType)
                {
                    case WeaponType.Firearm:
                        ProcessFirearm(weapon, transform, wielder);
                        break;
                    case WeaponType.Melee:
                    default:
                        break;
                }
            }
            else if (weapon.StuckInsideParams.HasValue)
            {
                var param = weapon.StuckInsideParams.Value;

                if (!Scene.HasEntity(param.Entity))
                {
                    weapon.StuckInsideParams = null;
                    weapon.IsAttachedToWall = true;
                }
                else
                {
                    var attachedTransform = Scene.GetComponentFrom<TransformComponent>(param.Entity);
                    transform.Position = Vector2.Transform(param.LocalOffset, attachedTransform.LocalToWorldMatrix);
                    transform.Rotation = attachedTransform.Rotation + param.LocalRotation;
                }
            }
            else
            {
                transform.LocalPivot = default;
                if (!weapon.HasRoundsLeft && velocityComponent.MeasuredSpeed <= float.Epsilon)
                    transform.Scale = new Vector2(1, weapon.IsFlipped ? -0.5f : 0.5f);
                else
                    transform.Scale = new Vector2(1, weapon.IsFlipped ? -1 : 1);
            }

            var renderer = Scene.GetComponentFrom<QuadShapeComponent>(weapon.BaseSpriteEntity);

            //if (weapon.ShouldBeHighlighted)
            //    renderer.RenderOrder = RenderOrders.HighlightedItemsLower;
            //else 
            if (weapon.StuckInsideParams.HasValue)
                renderer.RenderOrder = RenderOrders.RagdollsLower.WithOrder(-100);
            else if (weapon.IsAttachedToWall)
                renderer.RenderOrder = RenderOrders.BackgroundBehind.WithOrder(1);
            else
                renderer.RenderOrder = weapon.RenderOrder;

            weapon.ShouldBeHighlighted = false;
            if (weapon.AnimatedParts != null)
                foreach (var animatedPart in weapon.AnimatedParts)
                    Scene.GetComponentFrom<QuadShapeComponent>(animatedPart).RenderOrder = renderer.RenderOrder.OffsetOrder(1);

            weapon.IsFiring = false;
        }
    }


    /// <summary>
    /// Helper function to forcefully fire a weapon. Make sure to increment the weapon timer if you have to!
    /// This function ignores auto fire and burst fire, it just straight up shoots the gun.
    /// </summary>
    public void ShootWeapon(WeaponComponent weapon, CharacterComponent wielder, float recoilMultiplier = 1)
    {
        if (weapon.Data.WeaponType is not WeaponType.Firearm)
            return;

        var transform = Scene.GetComponentFrom<TransformComponent>(weapon.Entity);
        var data = weapon.Data;
        var isPlayer = Scene.HasTag(wielder.Entity, Tags.Player);
        bool infiniteAmmoCheat = ImprobabilityDisks.IsEnabled("infinite_ammo") && isPlayer;

        if (ShouldPump(weapon))
        {
            weapon.PumpAction(Scene, transform.Position);
            if (weapon.RemainingRounds != data.RoundsPerMagazine || infiniteAmmoCheat)
                EjectCasingParticle(weapon, transform);
        }

        if (weapon.HasRoundsLeft || infiniteAmmoCheat)
        {
            EmitBulletFrom(weapon, transform, wielder, isPlayer, recoilMultiplier);
            weapon.HasBeenPumped = false;
            if (!weapon.HasRoundsLeft && !infiniteAmmoCheat)
            {
                if (weapon.AnimatedParts != null && !data.IsPumpAction)
                    foreach (var animatedPart in weapon.AnimatedParts)
                    {
                        var animation = Scene.GetComponentFrom<WeaponPartAnimationComponent>(animatedPart);
                        animation.IsPlaying = false;
                        if (animation.InvisbleWhenOutOfAmmo && Scene.TryGetComponentFrom<ShapeComponent>(animatedPart, out var shape))
                            shape.Visible = false;
                        animation.CurrentPlaybackTime = animation.OutOfAmmoKeyframeTime * animation.Duration;
                    }
            }
        }
    }

    private static bool ShouldPump(WeaponComponent weapon)
    {
        var data = weapon.Data;
        return data.IsPumpAction && weapon.Timer >= data.UseDelay * 0.2f && !weapon.HasBeenPumped && weapon.HasRoundsLeft;
    }

    private void ProcessFirearm(WeaponComponent weapon, TransformComponent transform, CharacterComponent wielder)
    {
        var entity = weapon.Entity;
        var data = weapon.Data;

        var isPlayer = Scene.HasTag(wielder.Entity, Tags.Player);
        bool infiniteAmmoCheat = ImprobabilityDisks.IsEnabled("infinite_ammo") && isPlayer;

        if (ShouldPump(weapon))
        {
            weapon.PumpAction(Scene, transform.Position);
            if (weapon.RemainingRounds != data.RoundsPerMagazine || infiniteAmmoCheat)
                EjectCasingParticle(weapon, transform);
        }


        if (weapon.IsFiring && weapon.Timer >= data.UseDelay && (weapon.HasRoundsLeft || infiniteAmmoCheat))
        {
            if (!weapon.Data.Automatic && weapon.Data.BurstFireCount > 1)
            {
                //do burst fire
                if (!weapon.IsBusyBurstFiring)
                {
                    weapon.IsBusyBurstFiring = true;
                    for (int i = 0; i < data.BurstFireCount; i++)
                        MadnessUtils.DelayPausable(i * data.UseDelay, () => EmitBulletFrom(weapon, transform, wielder, isPlayer));
                    MadnessUtils.DelayPausable((data.BurstFireCount + 1) * data.UseDelay, () => weapon.IsBusyBurstFiring = false);
                }
            }
            else
                EmitBulletFrom(weapon, transform, wielder, isPlayer);

            weapon.HasBeenPumped = false;
            if (!weapon.HasRoundsLeft && !infiniteAmmoCheat)
            {
                //out of ammo
                if (isPlayer)
                {
                    //Prefabs.CreateNotification(
                    //    Scene,
                    //    transform.Position + new Vector2(0, 90),
                    //    "Out of ammo", 1f);
                    Audio.PlayOnce(Sounds.OutOfAmmo, 1, 1);
                }

                if (weapon.AnimatedParts != null && !data.IsPumpAction)
                    foreach (var animatedPart in weapon.AnimatedParts)
                    {
                        var animation = Scene.GetComponentFrom<WeaponPartAnimationComponent>(animatedPart);
                        animation.IsPlaying = false;
                        if (animation.InvisbleWhenOutOfAmmo && Scene.TryGetComponentFrom<ShapeComponent>(animatedPart, out var shape))
                            shape.Visible = false;
                        animation.CurrentPlaybackTime = animation.OutOfAmmoKeyframeTime * animation.Duration;
                    }
            }
        }
    }

    private void EmitBulletFrom(WeaponComponent weapon, TransformComponent transform, CharacterComponent wielder, bool isPlayer, float recoilMultiplier = 1)
    {
        var entity = weapon.Entity;
        var data = weapon.Data;

        bool infiniteAmmo = weapon.InfiniteAmmo || (ImprobabilityDisks.IsEnabled("infinite_ammo") && isPlayer);

        if ((!infiniteAmmo && !weapon.HasRoundsLeft) || !Scene.HasEntity(entity))
            return;

        if (infiniteAmmo)
            weapon.RemainingRounds++; // we do ++ so that we can still detect that the weapon has been used
        else
            weapon.RemainingRounds--;

        weapon.Timer = 0;
        var barrel = GetBarrel(weapon, transform);

        if (data.ShootSounds != null && data.ShootSounds.Count > 0)
        {
            var sound = Utilities.PickRandom(data.ShootSounds).Value;
            float volume = 1;

            if (!infiniteAmmo && isPlayer)
            {
                const float percentageThreshold = 0.5f;
                float r = (weapon.RemainingRounds / (float)weapon.Data.RoundsPerMagazine) / percentageThreshold;
                if (r < 1)
                {
                    Audio.PlayOnce(SoundCache.Instance.LoadSoundEffect(
                        Assets.Load<FixedAudioData>("sounds/low_ammo_warn.wav")), (1 - r) * 0.9f);
                    volume = float.Lerp(r, 1, 0.8f);
                }
            }

            Audio.PlayOnce(SoundCache.Instance.LoadSoundEffect(sound), volume);
        }

        var muzzleFlashSize = Utilities.Clamp(data.Damage * 2.5f * Utilities.RandomFloat(0.8f, 1.2f) * data.BulletsPerShot, 1.5f, 3.3f);
        Prefabs.CreateMuzzleFlash(Scene,
            barrel.position + barrel.direction * 80 * muzzleFlashSize,
            MathF.Atan2(barrel.direction.Y, barrel.direction.X) * Utilities.RadToDeg,
            muzzleFlashSize);

        if (!data.IsPumpAction)
            EjectCasingParticle(weapon, transform);

        wielder.Positioning.CurrentRecoil += weapon.Data.Recoil * recoilMultiplier;

        if (weapon.AnimatedParts != null && !data.IsPumpAction)
            foreach (var animatedPart in weapon.AnimatedParts)
            BulletEmitter.CastBulletRay(new BulletEmitter.BulletParameters(weapon)
            {
                Origin = barrel.position,
                ClusterSize = weapon.Data.BulletsPerShot,
                Direction = dir,
                OriginCharacter = wielder,
                CanBeAutoDodged = !wielder.AttacksCannotBeAutoDodged,
                CanBeDeflected = data.CanBulletsBeDeflected,
                EnemyCollisionLayer = wielder.EnemyCollisionLayer,
                IgnoreCollisionSet = wielder.AttackIgnoreCollision
            });
            {
                var animation = Scene.GetComponentFrom<WeaponPartAnimationComponent>(animatedPart);
                animation.IsPlaying = true;
                animation.CurrentPlaybackTime = 0;
            }

        for (int i = 0; i < data.BulletsPerShot; i++)
        {
            var dir = barrel.direction;
            dir += Utilities.RandomPointInCircle() * (1 - data.Accuracy);
            MadnessUtils.Shake(weapon.Data.Damage * 4);
        }
    }

    private void EjectCasingParticle(WeaponComponent weapon, TransformComponent transform)
    {
        if (weapon.Data.EjectionParticle == EjectionParticle.None)
            return;

        var particles = GetParticlesFor(weapon.Data.EjectionParticle, Scene);
        if (particles == null)
            return;

        var casing = particles.GenerateParticleObject(Game.State, transform);
        casing.Rotation = transform.Rotation;
        casing.Size = weapon.Data.CasingSize * 60;
        casing.Position = Vector2.Transform(weapon.CasingEjectionPoint, transform.LocalToWorldMatrix);
        casing.Velocity = Vector2.TransformNormal(Vector2.UnitY, transform.LocalToWorldMatrix) * 2000 + Utilities.RandomPointInCircle(0, 500);
        Scene.GetSystem<ParticleSystem>().CreateParticle(particles, casing);
    }

    /// <summary>
    /// Get a barrel's position and direction from a weapon.
    /// </summary>
    /// <param name="weapon"></param>
    /// <param name="weaponTransform"></param>
    /// <returns></returns>
    public static (Vector2 position, Vector2 direction) GetBarrel(WeaponComponent weapon, TransformComponent weaponTransform)
    {
        var endPoint = Vector2.Transform(new Vector2(weapon.BarrelEndPoint.X, weapon.BarrelEndPoint.Y), weaponTransform.LocalToWorldMatrix);
        var aimingDir = Vector2.Normalize(Vector2.TransformNormal(new Vector2(1, 0), weaponTransform.LocalToWorldMatrix));

        return (endPoint, aimingDir);
    }
}
