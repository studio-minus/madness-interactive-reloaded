using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Numerics;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.ParticleSystem;
using Walgelijk.Physics;
using static MIR.Textures;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MIR;

/// <summary>
/// Manage <see cref="WeaponComponent"/>s.
/// </summary>
public class WeaponSystem : Walgelijk.System
{
    private static QueryResult[] buffer = new QueryResult[8];

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

    /// <summary>
    /// Get how big a bullethole will be from a weapon.
    /// </summary>
    public static float GetBulletHoleSize(float totalDistance, WeaponComponent weapon) =>
        0.1f + weapon.GetDamageAtDistance(totalDistance) * 0.05f / weapon.Data.BulletsPerShot;

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
            CastBulletRay(barrel.position, dir, weapon, data, wielder, 0);
        }
    }

    private void CastBulletRay(Vector2 origin, Vector2 bulletDirection, WeaponComponent weapon, WeaponData data, CharacterComponent wielder, float totalDistance, int iteration = 0, bool isCosmetic = false)
    {
        if (iteration >= 8)
            return;

        var physics = Scene.GetSystem<PhysicsSystem>();
        var isExitWound = physics.QueryPoint(origin + bulletDirection * 5, buffer, wielder.EnemyCollisionLayer, ignore: wielder.AttackIgnoreCollision) > 0;

        if (physics.Raycast(origin, bulletDirection, out var hit, filter: wielder.EnemyCollisionLayer | CollisionLayers.BlockBullets, ignore: wielder.AttackIgnoreCollision) && Scene.HasEntity(hit.Entity))
        {
            totalDistance += hit.Distance;

            Scene.GetSystem<BulletTracerSystem>().ShowTracer(origin, hit.Position);
            if ((hit.Body.FilterBits & CollisionLayers.BlockBullets) == CollisionLayers.BlockBullets)
            {
                var inDecalZone = physics.QueryPoint(hit.Position, buffer, CollisionLayers.DecalZone) > 0;
                if (inDecalZone)
                    Prefabs.CreateBulletHole(Scene, hit.Position, Utilities.VectorToAngle(hit.Normal), 50);
            }

            if (Vector2.Distance(origin, hit.Position) < 5)
                isExitWound = false;

            if (isCosmetic && iteration > 1)
                return;

            var hitTransform = Scene.GetComponentFrom<TransformComponent>(hit.Entity);
            var localPoint = Vector2.Transform(hit.Position, hitTransform.WorldToLocalMatrix);

            var victimIsPlayer = Scene.HasTag(hit.Entity, Tags.Player);
            if (victimIsPlayer && ImprobabilityDisks.IsEnabled("god"))
                return;

            CharacterComponent? victimChar = null;
            var hasBodyPart = Scene.TryGetComponentFrom<BodyPartComponent>(hit.Entity, out var bodyPart);

            var isCharacter = hasBodyPart && Scene.TryGetComponentFrom(bodyPart!.Character.Entity, out victimChar);
            bool hasDodged = false;

            if (isCharacter && (victimChar?.IsAlive ?? false))
            {
                var isDodging = victimChar.AnimationConstrainsAny(AnimationConstraint.PreventBeingShot);

                if (isDodging ||
                    victimChar.Flags.HasFlag(CharacterFlags.Invincible))
                {
                    hasDodged = true;

                    if (!victimIsPlayer)
                    {
                        // drain dodge from enemies a bit anyway
                        victimChar.DrainDodge(0.005f);
                    }
                }

                {
                    bool canDodge = victimChar.HasDodge() && !isDodging;

                    if (canDodge && Scene.TryGetComponentFrom<AiComponent>(bodyPart!.Character.Entity, out var a))
                        a.PanicLevel += ConVars.Instance.PanicMultiplier * victimChar.Stats.PanicIntensity;

                    // deflection

                    bool willDeflect =
                        weapon.Data.BulletsPerShot == 1
                        && bulletDirection.X > 0 == victimChar.Positioning.IsFlipped;

                    bool deflectingWeapon =
                        canDodge &&
                        victimChar.IsMeleeBlocking
                        && victimChar.HasWeaponEquipped
                        && victimChar.EquippedWeapon.TryGet(Scene, out var victimEquipped)
                        && victimEquipped.Data.CanDeflectBullets
                        && weapon.Data.CanBulletsBeDeflected;

                    float armourDeflectChance = bodyPart != null
                        && bodyPart.Entity == victimChar.Positioning.Head.Entity ? victimChar.Look.GetHeadDeflectionChance() : victimChar.Look.GetBodyDeflectionChance();

                    bool deflectingArmour = !victimChar.Look.Cosmetic && armourDeflectChance > Utilities.RandomFloat();

                    willDeflect &= deflectingArmour || deflectingWeapon;

                    if (willDeflect)
                    {
                        if (deflectingWeapon)
                            victimChar.DrainDodge(ConVars.Instance.DeflectDodgeCost * weapon.Data.Damage); //minder dodge damage met zwaardiaan

                        if ((victimChar.HasDodge() || victimChar.Stats.DodgeOversaturate))
                        {
                            var perfectDeflect = deflectingWeapon && wielder.AttacksCannotBeAutoDodged && victimChar.Positioning.MeleeBlockProgress < 1; // TODO convar
                            if (!wielder.AttacksCannotBeAutoDodged || perfectDeflect) // you are allowed to deflect an accurate shot event if you time it right
                            {
                                var hitPosOnLine = hit.Position;

                                if (deflectingWeapon)
                                {
                                    hitPosOnLine = MadnessUtils.ClosestPointOnLine(victimChar.DeflectionLine.A, victimChar.DeflectionLine.B, hit.Position);
                                    victimChar.Positioning.MeleeBlockImpactIntensity += Utilities.RandomFloat(-1, 1);
                                }

                                if (perfectDeflect)
                                {
                                    wielder.DodgeMeter = -1;
                                    wielder.DodgeRegenCooldownTimer = 10;
                                    victimChar.DodgeMeter = victimChar.Stats.DodgeAbility;
                                    victimChar.Positioning.MeleeBlockProgress = 1;
                                    victimChar.Positioning.MeleeBlockImpactIntensity += 1;
                                    victimChar.Positioning.CurrentRecoil += 2;

                                    // TODO should be in a system and the sound is not very nice
                                    Audio.PlayOnce(
                                        SoundCache.Instance.LoadSoundEffect(
                                            Assets.Load<FixedAudioData>("sounds/deflection/perfect_deflect_1.wav")), 2);
                                    const float d = 0.5f;
                                    MadnessUtils.RoutineForSecondsPausable(d, static (dt) =>
                                    {
                                        Game.Main.State.Time.TimeScale = Utilities.SmoothApproach(Game.Main.State.Time.TimeScale, 1, 1, dt);
                                    });
                                    MadnessUtils.DelayPausable(0.05f, static () => { Game.Main.State.Time.TimeScale = 0.2f; });
                                    MadnessUtils.DelayPausable(d, static () => { Game.Main.State.Time.TimeScale = 1; });
                                }
                                else
                                    Audio.PlayOnce(Utilities.PickRandom(Sounds.BulletDeflection), 0.5f);

                                if (deflectingArmour && victimChar.IsAlive && victimChar.Flags.HasFlag(CharacterFlags.StunAnimationOnNonFatalAttack))
                                {
                                    if (bulletDirection.X < 0 == victimChar.Positioning.IsFlipped)
                                        victimChar.PlayAnimation(Registries.Animations.Get("stun_light_forwards"), 1.2f);
                                    else
                                        victimChar.PlayAnimation(Registries.Animations.Get("stun_light_backwards"), 1.2f);
                                }

                                var returnDir = perfectDeflect
                                    ? Vector2.Normalize(wielder.Positioning.Head.GlobalPosition - hitPosOnLine)
                                    : Vector2.Normalize(bulletDirection * new Vector2(-1, Utilities.RandomFloat(-12, 12)));
                                wielder = victimChar;
                                MadnessUtils.DelayPausable(0.05f, () =>
                                {
                                    CastBulletRay(hitPosOnLine, returnDir, weapon, data, wielder, totalDistance, iteration + 1);
                                });
                                Prefabs.CreateDeflectionSpark(Scene, hitPosOnLine, Utilities.VectorToAngle(returnDir), 1);
                                return;
                            }
                        }
                    }
                    else if (canDodge)
                    {
                        var bulletIsComingFromFacingDirection = bulletDirection.X > 0 == victimChar.Positioning.IsFlipped;
                        var dodgeCost = float.Max(weapon.Data.Damage, 0.4f) / 1.5f * (victimChar.Stats.DodgeOversaturate ? 1 : weapon.Data.BulletsPerShot);

                        if (!bulletIsComingFromFacingDirection)
                            dodgeCost *= ConVars.Instance.DodgeFromBehindCostMultiplier;

                        var distanceSquared = Vector2.DistanceSquared(hit.Position, origin);

                        if (!victimIsPlayer)
                        {
                            float pointBlankDistance = ConVars.Instance.PointBlankDistance;
                            const float minDodgeCurveDistance = 1600;
                            const float maxDodgeCurveDistance = 2500;

                            if (distanceSquared < (pointBlankDistance * pointBlankDistance))
                            {
                                if (!victimChar.Stats.DodgeOversaturate)
                                    dodgeCost *= ConVars.Instance.DodgePointBlankCostMultiplier;
                            }
                            else
                                dodgeCost /= Utilities.MapRange(
                                    minDodgeCurveDistance * minDodgeCurveDistance, maxDodgeCurveDistance * maxDodgeCurveDistance,
                                    1, 8,
                                    Utilities.Clamp(distanceSquared, minDodgeCurveDistance * minDodgeCurveDistance, maxDodgeCurveDistance * maxDodgeCurveDistance));
                        }

                        victimChar.DrainDodge(dodgeCost);

                        bool shouldJumpDodge = victimChar.Stats.AgilitySkillLevel != AgilitySkillLevel.None && !victimIsPlayer && Utilities.RandomFloat() < ConVars.Instance.JumpDodgeChance;
                        canDodge = victimChar.HasDodge() || victimChar.Stats.DodgeOversaturate || shouldJumpDodge;

                        // je kan nog steeds ontwijken omdat het pas net < 0 is geworden :)
                        if (canDodge && !wielder.AttacksCannotBeAutoDodged)
                        {
                            if (shouldJumpDodge)
                                CharacterUtilities.TryJumpDodge(Scene, victimChar);
                            else
                                CharacterUtilities.TryDodgeAnimation(victimChar);

                            if (victimIsPlayer)
                                Audio.PlayOnce(Utilities.PickRandom(Sounds.NearMiss), 1f);
                            hasDodged = true;
                        }
                    }
                }
            }

            if (hasDodged)
                CastBulletRay(hit.Position, bulletDirection, weapon, data, wielder, totalDistance, iteration + 1);
            else if (victimChar == null || victimChar.Flags.HasFlag(CharacterFlags.AttackResponseBullet))
            {
                //actual on hit

                if (Scene.TryGetComponentFrom<ShapeComponent>(hit.Entity, out var bodyPartRenderer))
                    if (bodyPartRenderer.HorizontalFlip)
                        localPoint.X *= -1;

                if (Scene.TryGetComponentFrom<IsShotTriggerComponent>(hit.Entity, out var trigger))
                    trigger.Event.Dispatch(new HitEvent(weapon, hit.Position, hit.Normal, bulletDirection));


                if (isCharacter && victimChar != null)
                {
                    if (victimChar.IsAlive && totalDistance < 300 && Utilities.RandomFloat() > 0.95f)
                    {
                        var splatter = SoundCache.Instance.LoadSoundEffect(Assets.Load<FixedAudioData>("sounds/splatter.wav"));
                        Audio.PlayOnce(splatter, 1, Utilities.RandomFloat(0.95f, 1.05f));
                    }

                    //damage broken armour
                    if (hit.Entity == victimChar.Positioning.Head.Entity && Utilities.RandomFloat() > 0.5f)
                    {
                        var originalLook = victimChar.Look;
                        victimChar.Look = new CharacterLook(victimChar.Look); //copy for for the copy

                        if (originalLook.HeadLayer1?.TryGetBrokenReplacement(out var key) ?? false)
                            victimChar.Look.HeadLayer1 = Registries.Armour.HeadAccessory.Get(key);

                        if (originalLook.HeadLayer2?.TryGetBrokenReplacement(out key) ?? false)
                            victimChar.Look.HeadLayer2 = Registries.Armour.HeadAccessory.Get(key);

                        if (originalLook.HeadLayer3?.TryGetBrokenReplacement(out key) ?? false)
                            victimChar.Look.HeadLayer3 = Registries.Armour.HeadAccessory.Get(key);

                        victimChar.NeedsLookUpdate = true;
                    }
                }

                if (Scene.TryGetComponentFrom<ImpactOffsetComponent>(hit.Entity, out var impactOffset))
                {
                    float rot = 3;
                    rot *= bulletDirection.X > 0 ? 1 : -1;

                    if (localPoint.Y > hitTransform.LocalRotationPivot.Y)
                        impactOffset.RotationOffset += -rot;
                    else
                        impactOffset.RotationOffset += rot;

                    impactOffset.TranslationOffset += bulletDirection * 6/* / Math.Min(data.BulletsPerShot, 2)*/;
                }

                var wasAlive = victimChar?.IsAlive ?? false;

                if (victimChar != null && hasBodyPart && bodyPart != null && bodyPart.Character.IsValid(Scene))
                {
                    if (!victimChar.IsAlive && Scene.TryGetComponentFrom<VerletNodeComponent>(hit.Entity, out var ragdollNode))
                        ragdollNode.Acceleration += bulletDirection * data.Damage * 1;

                    if (Utilities.RandomFloat() > 0.7f)
                    {
                        var angle = Utilities.RandomFloat(-25, 25) + Utilities.VectorToAngle(bulletDirection);
                        var size = Utilities.RandomFloat(250, 400);
                        Prefabs.CreateBloodSplat(Scene, hit.Position, angle, victimChar.Look.BloodColour, size);
                    }

                    CharacterUtilities.DoGunDamage(Scene, weapon.GetDamageAtDistance(totalDistance), bodyPart, victimChar, bulletDirection, localPoint);

                    // increment player kills stat
                    if (Scene.FindAnyComponent<GameModeComponent>(out var mode) && mode.Mode == GameMode.Campaign)
                        if (wasAlive && !victimChar.IsAlive && Scene.HasTag(wielder.Entity, Tags.Player) && Level.CurrentLevel != null)
                            CampaignProgress.GetCurrentStats()?.IncrementKills(Level.CurrentLevel);

                    if (Scene.TryGetComponentFrom<AiComponent>(bodyPart.Character.Entity, out var ai))
                        ai.PanicLevel += 0.25f;
                }

                if (Scene.TryGetComponentFrom<BodyPartShapeComponent>(hit.Entity, out var damagable) && victimChar != null)
                {
                    var charScaleSqrd = victimChar.Positioning.Scale * victimChar.Positioning.Scale;
                    var randomness = Utilities.RandomPointInCircle(-0.1f, 0.1f);
                    randomness.X *= 2;
                    var p = localPoint + randomness;
                    var s = GetBulletHoleSize(totalDistance, weapon) / (iteration + 1) * (isExitWound ? 2 : 0.5f);
                    if (isCosmetic)
                        s *= 0.5f;
                    s /= charScaleSqrd;
                    s = damagable.TryAddHole(p.X, p.Y, s);

                    Prefabs.CreateBloodSpurt(Scene, hit.Position, MathF.Atan2(bulletDirection.Y, bulletDirection.X) * Utilities.RadToDeg, damagable.BloodColour, Utilities.Clamp(s * 4, 1f, 1.4f));

                    if (s > 0.2f)
                    {
                        var d = isExitWound ? bulletDirection : hit.Normal;
                        var n = Vector2.TransformNormal(d, hitTransform.WorldToLocalMatrix) * 180;
                        var newP = p + n * s;
                        damagable.TryAddInnerCutoutHole(newP.X, newP.Y, s + 1 / charScaleSqrd);
                    }

                    // make sure that corpses never block bullets
                    bool cosmeticBullet = !wasAlive;// && 
                                                    //weapon.GetDamageAtDistance(hit.Distance) / (iteration + 1) < Utilities.RandomFloat(0.6f, 0.8f);

                    cosmeticBullet = weapon.GetDamageAtDistance(totalDistance) / (iteration + 1) < 0.4f;

                    // TODO is this even working as intended??
                    CastBulletRay(hit.Position, bulletDirection, weapon, data, wielder, totalDistance, iteration + 1, cosmeticBullet);
                }

                if (victimChar != null &&
                    victimChar.Flags.HasFlag(CharacterFlags.StunAnimationOnNonFatalAttack) &&
                    victimChar.IsAlive && !isExitWound)
                {
                    if (bulletDirection.X < 0 == victimChar.Positioning.IsFlipped)
                        victimChar.PlayAnimation(Registries.Animations.Get("stun_light_forwards"), 1.2f);
                    else
                        victimChar.PlayAnimation(Registries.Animations.Get("stun_light_backwards"), 1.2f);
                }
            }
        }
        else
            Scene.GetSystem<BulletTracerSystem>().ShowTracer(origin, origin + bulletDirection * 10000);
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
