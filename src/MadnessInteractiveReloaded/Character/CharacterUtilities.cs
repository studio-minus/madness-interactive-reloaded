using System;
using System.Numerics;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.Physics;
using static MIR.Textures;

namespace MIR;

/// <summary>
/// Static singleon helper class for <see cref="CharacterComponent"/> and related things.
/// </summary>
public static class CharacterUtilities
{
    /// <summary>
    /// If the character's body or head health is low enough,
    /// kill the character.
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="character"></param>
    public static void UpdateAliveStatus(Scene scene, CharacterComponent character)
    {
        if (character.AnimationConstrainsAny(AnimationConstraint.PreventDying))
            return;

        var head = scene.GetComponentFrom<BodyPartComponent>(character.Positioning.Head.Entity);
        if (head.Health <= float.Epsilon)
        {
            character.Kill();
            return;
        }

        //dit is sneller. minimaal ja weet ik maar het helpt 
        var body = scene.GetComponentFrom<BodyPartComponent>(character.Positioning.Body.Entity);
        if (body.Health <= float.Epsilon)
        {
            character.Kill();
            return;
        }
    }

    public static void PickupWithAnimation(Scene scene, CharacterComponent character, WeaponComponent? weapon)
    {
        if (scene.HasComponent<CharacterPickupComponent>(character.Entity))
            return;

        if (character.HasWeaponEquipped)
            character.DropWeapon(scene);

        if (weapon != null)
        {
            float delay = 0;

            if (!scene.HasComponent<JumpDodgeComponent>(character.Entity))
            {
                var d = scene.AttachComponent(character.Entity, new CharacterPickupComponent
                {
                    Target = new(weapon.Entity)
                });
                delay = d.PickupTime * d.Duration;
            }

            MadnessUtils.DelayPausable(delay, () => // TODO what if the scene changes? ideally, these routines should be erased on scene change
            {
                if (character.EquipWeapon(scene, weapon))
                {
                    var pickupAssets = Assets.EnumerateFolder("sounds/pickup");
                    var data = Assets.Load<FixedAudioData>(Utilities.PickRandom(pickupAssets));
                    scene.Game.AudioRenderer.PlayOnce(SoundCache.Instance.LoadSoundEffect(data));
                }
            });
        }
    }

    /// <summary>
    /// Do damage with a gun.
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="damage"></param>
    /// <param name="bodyPart"></param>
    /// <param name="character"></param>
    /// <param name="bulletDirection"></param>
    /// <param name="localHitPoint"></param>
    public static void DoGunDamage(Scene scene, float damage, BodyPartComponent bodyPart, CharacterComponent character, Vector2 bulletDirection, Vector2 localHitPoint)
    {
        bodyPart.Damage(damage);
        if (!character.HasBeenRagdolled)
        {
            UpdateAliveStatus(scene, character);
            if (!character.IsAlive && !character.Flags.HasFlag(CharacterFlags.NoRagdoll))
                TryStartDeathAnimation(scene, character, bulletDirection, localHitPoint);
            //MadnessUtils.TurnIntoRagdoll(Scene, entity, character, phys);
        }
    }

    /// <summary>
    /// Responsible for positioning character hands based on which type of weapon
    /// they're are holding.
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="character"></param>
    /// <param name="equipped"></param>
    public static void PositionHandsForWeapon(Scene scene, CharacterComponent character, WeaponComponent? equipped)
    {
        // (duston): reset the flag first only here so every single positioning function doesn't need to explicitly set it to false, only the ones that set it to true should care.
        character.Positioning.SecondaryHandFollowsPrimary = false;
        var poseParams = new HandPoseParams(scene, character, scene.Game.State.Time.DeltaTime, equipped);
        if (!character.HasWeaponEquipped || equipped == null) // fist fight
            HandPosingFunctions.FistFight(poseParams);
        else
            switch (equipped.Data.WeaponType)
            {
                case WeaponType.Firearm:
                    switch (character.Positioning.HandPositionMode)
                    {
                        case HandPositionMode.OneHand:
                            HandPosingFunctions.OneHandedGun(poseParams);
                            break;
                        case HandPositionMode.TwoHands:
                            HandPosingFunctions.TwoHandedGun(poseParams);
                            break;
                        default:
                            break;
                    }

                    break;
                case WeaponType.Melee:
                    switch (character.Positioning.HandPositionMode)
                    {
                        case HandPositionMode.OneHand:
                            switch (equipped.Data.MeleeDamageType)
                            {
                                case MeleeDamageType.Blade:
                                    HandPosingFunctions.OneHandedSword(poseParams);
                                    break;
                                case MeleeDamageType.Axe:
                                    HandPosingFunctions.OneHandedAxe(poseParams);
                                    break;
                                case MeleeDamageType.Blunt:
                                    HandPosingFunctions.OneHandedBlunt(poseParams);
                                    break;
                                default:
                                    Logger.Error(
                                        "Invalid melee damage type when holding a melee weapon. Only 'Blade', 'Axe', and 'Blunt' is valid");
                                    break;
                            }

                            break;
                        case HandPositionMode.TwoHands:
                            switch (equipped.Data.MeleeDamageType)
                            {
                                case MeleeDamageType.Blade:
                                    HandPosingFunctions.TwoHandedSword(poseParams);
                                    break;
                                case MeleeDamageType.Axe:
                                    HandPosingFunctions.TwoHandedAxe(poseParams);
                                    break;
                                case MeleeDamageType.Blunt:
                                    HandPosingFunctions.TwoHandedBlunt(poseParams);
                                    break;
                                default:
                                    Logger.Error(
                                        "Invalid melee damage type when holding a melee weapon. Only 'Blade', 'Axe', and 'Blunt' is valid");
                                    break;
                            }

                            break;
                        default:
                            break;
                    }
                    break;
            }

        foreach (var @override in character.Positioning.HandPoseFunctionOverride)
            @override(poseParams);
    }

    /// <summary>
    /// Will try to drop the characters weapon and play their death animation.
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="entity"></param>
    /// <param name="character"></param>
    /// <param name="bulletDirection"></param>
    /// <param name="localHitPoint"></param>
    /// <param name="chance"></param>
    /// <exception cref="Exception"></exception>
    public static void TryStartDeathAnimation(Scene scene, CharacterComponent character, Vector2 bulletDirection, Vector2 localHitPoint, float chance = 0.5f)
    {
        if (character.AnimationConstrainsAny(AnimationConstraint.PreventDying))
            return;

        var entity = character.Entity;

        character.AllowWalking = false;
        character.Kill();

        if (character.AnimationConstrainsAny(AnimationConstraint.PreventDeathAnimation) ||
            character.Positioning.IsFlying ||
            character.IsPlayingAnimationGroup("deaths")) // TODO where to get this from? hardcoded string bs
        {
            var anim = character.MainAnimation;
            if (character.Positioning.IsFlying ||
                (anim != null &&
                !character.AnimationConstrainsAny(AnimationConstraint.PreventRagdoll) &&
                anim.UnscaledTimer > anim.Animation.TotalDuration * 0.2f)) // wtf
                MadnessUtils.TurnIntoRagdoll(scene, character);
            return;
        }

        var headPart = scene.GetComponentFrom<BodyPartComponent>(character.Positioning.Head.Entity);
        var bodyPart = scene.GetComponentFrom<BodyPartComponent>(character.Positioning.Body.Entity);

        bool isHeadMoreDamaged = (headPart.Health / headPart.MaxHealth) < (bodyPart.Health / bodyPart.MaxHealth);
        float damageIntensity = 1 / float.Max(0.01f, float.Min(headPart.Health, bodyPart.Health) + 1);
        var bulletComesFromFacingDirection = bulletDirection.X > 0 == character.Positioning.IsFlipped;

        if (isHeadMoreDamaged && Utilities.RandomFloat() > 0.8f)
            MadnessUtils.EjectHeadDecorations(scene, character);

        if (damageIntensity > 1.25f && Utilities.RandomFloat() > (1 - chance))
        {
            if (isHeadMoreDamaged)
            {
                if (Utilities.RandomFloat() > (1 - chance))
                {
                    playDeathAnimation(Animations.DeathByHeadshot.PickRandom(bulletComesFromFacingDirection));
                    return;
                }
            }
            else //dus dan is het lichaam meer gedamaged
            {
                var anim = localHitPoint.Y < 0 ? Animations.DeathByLegshot : Animations.DeathByThroatshot;
                if (float.Abs(localHitPoint.Y) < 0.2f)
                    anim = Animations.DeathByBodyshot;
                playDeathAnimation(anim.PickRandom(bulletComesFromFacingDirection));
                return;
            }
        }

        void playDeathAnimation(CharacterAnimation anim)
        {
            character.DropWeapon(scene);

            // (duston): Don't attempt death animations on un-even terrain.
            if (Level.CurrentLevel != null && !Level.CurrentLevel.IsFlatAt(character.Positioning.GlobalCenter.X))
            {
                MadnessUtils.TurnIntoRagdoll(scene, character);
                return;
            }

            var s = 1f; // Utilities.RandomFloat(1f, 1.3f);
            character.StopAllAnimations();
            var a = character.PlayAnimation(anim, s);
            // during the animation, check if the body is above a flat ground. if it isnt, turn into a ragdoll MID ANIMATION 
            // TODO this should be in a system, not in a routine
            MadnessUtils.RoutineForSecondsPausable(anim.TotalDuration / s, dt =>
            {
                var isFlatUnderMe = Level.CurrentLevel?.IsFlatAt(character.Positioning.Body.ComputedVisualCenter.X) ?? true;
                if (!isFlatUnderMe && !character.AnimationConstrainsAny(AnimationConstraint.PreventRagdoll))
                    MadnessUtils.TurnIntoRagdoll(scene, character);

            });
            // if the animation is over stamp it onto the background immediately
            a.OnEnd += () =>
            {
                if (!character.HasBeenRagdolled && scene.HasEntity(entity))
                {
                    if (scene.HasComponent<PlayerComponent>(character.Entity))
                    {
                        // the player should never be deleted so we turn it into a ragdoll
                        // ignore the character animation constraints as well because this mfer (player) just died
                        MadnessUtils.TurnIntoRagdoll(scene, character);
                    }
                    else
                    {
                        Stamper.Stamp(scene, character.Positioning);
                        character.Delete(scene);
                    }
                }
            };
        }

        // nothing has been chosen
        if (!character.AnimationConstrainsAny(AnimationConstraint.PreventRagdoll))
            MadnessUtils.TurnIntoRagdoll(scene, character);
    }

    public static void StunHeavy(Scene scene, CharacterComponent character, bool facingImpact)
    {
        if (character.Positioning.IsFlying)
            return;

        if (Level.CurrentLevel == null || !Level.CurrentLevel.IsFlatAt(character.Positioning.GlobalCenter.X))
            return;

        scene.DetachComponent<MeleeSequenceComponent>(character.Entity);
        var dir = facingImpact == character.Positioning.IsFlipped ? 1 : -1;
        var o = character.Positioning.Body.ComputedVisualCenter;
        if (scene.GetSystem<PhysicsSystem>().Raycast(o, new Vector2(dir, 0), out var nearWallHit, 800, CollisionLayers.BlockMovement | CollisionLayers.BlockPhysics))
        {
            // we gotta do something else if the animation will throw us into a wall
            if (facingImpact)
                character.PlayAnimation(Registries.Animations["stun_wall_heavy_backwards"]);
            else
                character.PlayAnimation(Registries.Animations["stun_wall_heavy_forwards"]);
            return;
        }

        var backwardsAnimation = Utilities.PickRandom("stun_heavy_backwards", "stun_heavy_backwards2");
        character.PlayAnimation(Registries.Animations.Get(facingImpact ? backwardsAnimation : "stun_heavy_forwards"));
    }

    /// <summary>
    /// Try to perform a jump dodge that makes
    /// the character invulnerable for a short time while dodging.
    /// </summary>
    public static void TryJumpDodge(Scene scene, CharacterComponent character)
    {
        if (character.AnimationConstrainsAny(AnimationConstraint.PreventDodge))
            return;

        scene.DetachComponent<MeleeSequenceComponent>(character.Entity);
        var oldAcc = character.WalkAcceleration;
        bool isWalking = float.Abs(character.WalkAcceleration.X) > 0.1f;
        CharacterAnimation? anim;

        if (character.Positioning.IsFlying)
        {
            if (isWalking)
            {
                anim = character.WalkAcceleration.X > 0 == character.Positioning.IsFlipped
                    ? Registries.Animations.Get("jump_flight_backward")
                    : Registries.Animations.Get("jump_flight_forward");
            }
            else
                anim = Registries.Animations.Get("jump_flight_stationary");
        }
        else
        {
            if (isWalking)
            {
                anim = character.WalkAcceleration.X > 0 == character.Positioning.IsFlipped
                    ? Animations.GetBackwardsJumpDodge(character.Stats.AgilitySkillLevel)
                    : Animations.GetForwardsJumpDodge(character.Stats.AgilitySkillLevel);
            }
            else
                anim = Animations.GetStationaryJumpDodge(character.Stats.AgilitySkillLevel);
        }

        if (anim == null)
            return; // current character and requested animation is not valid, so just return. The method name starts with "Try", after all

        {
            // only master level agility can jump dodge on uneven terrain
            var isMaster = character.Stats.AgilitySkillLevel is AgilitySkillLevel.Master;

            if (!isMaster && !(Level.CurrentLevel?.IsFlatAt(character.Positioning.GlobalTarget.X) ?? true)) // flat where i am? 
            {
                TryDodgeAnimation(character);
                return;
            }

            if (isWalking)
            {
                const float range = 800; // TODO how do i determine this

                var targetX = character.Positioning.GlobalTarget.X + (character.WalkAcceleration.X > 0 ? range : -range);
                if (!isMaster && !(Level.CurrentLevel?.IsFlatAt(targetX) ?? true)) // flat where i will be?
                {
                    TryDodgeAnimation(character);
                    return;
                }

                // nobody can jump dodge through a wall
                if (Game.Main.Scene.GetSystem<PhysicsSystem>().Raycast(character.Positioning.Body.GlobalPosition, character.WalkAcceleration, out var hit, range, CollisionLayers.BlockMovement))
                {
                    TryDodgeAnimation(character);
                    return;
                }
            }
        }

        character.PlayAnimation(anim, 1);
        character.DrainDodge(1 / 4f); // TODO convar? stat? 
        scene.AttachComponent(character.Entity, new JumpDodgeComponent(anim)
        {
            Duration = character.Stats.JumpDodgeDuration,
            InitialAcceleration = character.WalkAcceleration,
            ShouldDash = isWalking
        });

        scene.Game.AudioRenderer.PlayOnce(Utilities.PickRandom(Sounds.Jump), 0.3f);
    }

    /// <summary>
    /// </summary>
    /// <param name="character"></param>
    /// <returns>Can this character dodge right now?</returns>
    public static bool CanDodge(CharacterComponent character)
    {
        if (character.Flags.HasFlag(CharacterFlags.Invincible))
            return true;

        if (character.AnimationConstrainsAny(AnimationConstraint.PreventDodge))
            return false;

        return character.HasDodge();
    }

    /// <summary>
    /// Try to play the dodge animation.
    /// </summary>
    /// <param name="character"></param>
    public static void TryDodgeAnimation(CharacterComponent character)
    {
        if (character.Flags.HasFlag(CharacterFlags.Invincible) ||
            character.AnimationConstrainsAny(AnimationConstraint.PreventDodge)
            /* || character.IsPlayingAnimationGroup("melee")*/)
            return;

        var anim = Animations.Dodge[character.AnimationFlipFlop % Animations.Dodge.Length];
        character.PlayAnimation(anim, 1 / float.Max(0.85f, character.Stats.DodgeAbility));
        character.AnimationFlipFlop++;
    }

    /// <summary>
    /// Perform an "accurate shot" from <paramref name="shooter"/> to <paramref name="victim"/>, which can only be dodged using a jump-dodge
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="shooter"></param>
    /// <param name="victim"></param>
    public static void DoAccurateShot(Scene scene, ComponentRef<CharacterComponent> shooter, ComponentRef<CharacterComponent> victim)
    {
        if (scene.Game.State.Time.SecondsSinceLoad - AiComponent.LastAccurateShotTime <
            CharacterConstants.AccurateShotCooldown)
            return;

        AiComponent.LastAccurateShotTime = scene.Game.State.Time.SecondsSinceLoad;

        Prefabs.CreateAccurateShotWarning(scene, victim, shooter);
    }

    /// <summary>
    /// Blends the characters animations together.
    /// </summary>
    /// <param name="character"></param>
    /// <returns></returns>
    public static AnimationResult CalculateMixedAnimation(CharacterComponent character)
    {
        if (!character.IsPlayingAnimation)
            return default;

        float mixFactor = Easings.Quad.InOut(character.AnimationMixProgress);
        var result = CalculateAnimationResult(character.Animations[^1], character);

        if (character.Animations.Count > 1)
        {
            var previous = CalculateAnimationResult(character.Animations[^2], character);

            result.BodyPosition = Utilities.Lerp(previous.BodyPosition, result.BodyPosition, mixFactor);
            result.BodyRotation = Utilities.LerpAngle(previous.BodyRotation, result.BodyRotation, mixFactor);

            result.HeadPosition = Utilities.Lerp(previous.HeadPosition, result.HeadPosition, mixFactor);
            result.HeadRotation = Utilities.LerpAngle(previous.HeadRotation, result.HeadRotation, mixFactor);

            result.Hand1Position = Utilities.Lerp(previous.Hand1Position, result.Hand1Position, mixFactor);
            result.Hand1Rotation = Utilities.LerpAngle(previous.Hand1Rotation, result.Hand1Rotation, mixFactor);
            result.Hand1Look = mixFactor < 0.5f ? previous.Hand1Look : result.Hand1Look;

            result.Hand2Position = Utilities.Lerp(previous.Hand2Position, result.Hand2Position, mixFactor);
            result.Hand2Rotation = Utilities.LerpAngle(previous.Hand2Rotation, result.Hand2Rotation, mixFactor);
            result.Hand2Look = mixFactor < 0.5f ? previous.Hand2Look : result.Hand2Look;
        }

        return result;
    }

    /// <summary>
    /// Get the info for positioning a character based on their animation.
    /// </summary>
    /// <param name="activeAnim"></param>
    /// <param name="character"></param>
    /// <returns></returns>
    public static AnimationResult CalculateAnimationResult(ActiveCharacterAnimation activeAnim, CharacterComponent character)
    {
        float flipScaling = character.Positioning.IsFlipped ? -1 : 1;
        var result = new AnimationResult();

        if (activeAnim.Animation.BodyAnimation?.TranslationCurve != null)
        {
            result.BodyPosition = activeAnim.GetBodyPosition();
            result.BodyPosition.X *= flipScaling;
        }

        if (activeAnim.Animation.BodyAnimation?.RotationCurve != null)
            result.BodyRotation = activeAnim.GetBodyRotation() * flipScaling;

        if (activeAnim.Animation.HeadAnimation?.TranslationCurve != null)
        {
            result.HeadPosition = activeAnim.GetHeadPosition();
            result.HeadPosition.X *= flipScaling;
        }

        if (activeAnim.Animation.HeadAnimation?.RotationCurve != null)
            result.HeadRotation = activeAnim.GetHeadRotation() * flipScaling;

        if (activeAnim.Animation.HandAnimations != null)
        {
            var handAnims = activeAnim.Animation.HandAnimations;
            //TODO dit kan mooier want het is allemaal hetzeldfe

            if (handAnims.Length >= 1)
            {
                var a = handAnims[0];
                if (a.TranslationCurve != null)
                    result.Hand1Position = calculateHandPosition(a,
                        character.Positioning.IsFlipped
                            ? character.Positioning.Hands[1]
                            : character.Positioning.Hands[0]);

                result.Hand1Rotation = calculateHandRotation(a);

                if (a.HandLooks != null && a.HandLooks.Length > 0)
                    result.Hand1Look = evalHandLook(a);
            }

            if (handAnims.Length >= 2)
            {
                var a = handAnims[1];
                if (a.TranslationCurve != null)
                    result.Hand2Position = calculateHandPosition(a,
                        character.Positioning.IsFlipped
                            ? character.Positioning.Hands[0]
                            : character.Positioning.Hands[1]);

                result.Hand2Rotation = calculateHandRotation(a);

                if (a.HandLooks != null && a.HandLooks.Length > 0)
                    result.Hand2Look = evalHandLook(a);
            }

            float calculateHandRotation(HandLimbAnimation handAnim)
            {
                var offset = 0f;

                if (handAnim.AdjustForAim)
                {
                    offset = Utilities.VectorToAngle(character.AimDirection * flipScaling);
                }

                if (handAnim.RotationCurve != null)
                    offset += (evalRotation(handAnim)) * flipScaling;

                return offset;
            }

            Vector2 calculateHandPosition(LimbAnimation handAnim, HandAnimatedLimb hand)
            {
                Vector2 result = evalTranslation(handAnim);
                result.X *= flipScaling;
                result *= character.Positioning.Scale;
                result += character.Positioning.GlobalCenter;

                if (activeAnim.Animation.RelativeHandPosition)
                {
                    result += hand.PosePosition;
                    var t1 = handAnim.TranslationCurve?.Evaluate(0) ?? default;
                    t1.X *= flipScaling;
                    result -= t1;
                }

                if (handAnim.AdjustForAim)
                {
                    var pivot = hand.PosePosition + character.Positioning.GlobalCenter; // the pivot for an aim-adjusted hand animation should be the original unanimated hand position (hand.PosePosition is local space so we have to add the global center)
                    var dir = character.AimDirection;
                    if (character.Positioning.IsFlipped)
                        dir *= -1;
                    result = Utilities.RotatePoint(result, Utilities.VectorToAngle(dir), pivot);
                }

                return result;
            }
        }

        Vector2 evalTranslation(LimbAnimation a) =>
            a.TranslationCurve?.Evaluate(activeAnim.CalculateProgress(a)) ?? Vector2.Zero;

        float evalRotation(LimbAnimation a) => a?.RotationCurve?.Evaluate(activeAnim.CalculateProgress(a)) ?? 0;
        HandLook? evalHandLook(HandLimbAnimation a) => a.GetHandLookForTime(activeAnim.UnscaledTimer / a.Duration);

        return result;
    }

    /// <summary>
    /// Try a weapon if you have one.
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="character"></param>
    public static void TryThrowWeapon(Scene scene, CharacterComponent character)
    {
        if (!character.IsAlive)
            return;

        if (!character.EquippedWeapon.TryGet(scene, out var wpn))
            return;

        if (character.IsPlayingAnimation && character.AnimationConstrainsAny(AnimationConstraint.PreventThrowing))
            return;

        var vel = scene.GetComponentFrom<VelocityComponent>(character.EquippedWeapon.Entity);
        var wpnTransform = scene.GetComponentFrom<TransformComponent>(character.EquippedWeapon.Entity);

        vel.IgnoreCollision = character.AttackIgnoreCollision;

        const float speed = 1.3f;
        var a = Animations.Throw.PickRandom(character.Positioning.IsFlipped);
        var animationDuration = a.TotalDuration / speed;

        character.PlayAnimation(a, speed);

        MadnessUtils.DelayPausable(animationDuration * 0.5f, () =>
        {
            if (!character.IsAlive || !character.HasWeaponEquipped)
                return;

            // (duston): We do this again because of the fact this is a DelayPausable.
            // When TryThrowWeapon is called, the currently held weapon is cached immediately,
            // between then and when this delayed routine runs
            // the weapon could have changed so we'd be referencing the stale WeaponComponent.
            if (!character.EquippedWeapon.TryGet(scene, out wpn))
                return;

            scene.AttachComponent(character.EquippedWeapon.Entity, new ThrowableProjectileComponent
            (
                wpn.Data.ThrowableDamage,
                wpn.Data.ThrowableHeavy,
                true,
                CollisionLayers.BlockPhysics | character.EnemyCollisionLayer,
                wpn.Data.ThrowableSharpBoxes,
                wpn.BaseSpriteEntity,
                new ComponentRef<CharacterComponent>(character.Entity)
            ));
            character.DropWeapon(scene);

            var targetPosition = character.AimTargetPosition;
            var delta = character.AimTargetPosition - character.Positioning.Head.GlobalPosition;
            var dir = Vector2.Normalize(delta);

            var distanceToTarget = 0f;

            if (scene.GetSystem<PhysicsSystem>().Raycast(wpnTransform.Position, dir, out var hit, 15000, // max auto-rotate distance
                    character.EnemyCollisionLayer | CollisionLayers.BlockPhysics, ignore: character.AttackIgnoreCollision))
            {
                delta = hit.Position - wpnTransform.Position;
                distanceToTarget = hit.Distance;
            }
            else
                distanceToTarget = delta.Length();

            var throwSpeed = 1e4f * 0.8f;
            vel.OverrideVelocity = dir * throwSpeed;

            scene.Game.AudioRenderer.PlayOnce(Utilities.PickRandom(Sounds.Swish));

            switch (wpn.Data.WeaponType)
            {
                case WeaponType.Melee:
                    switch (wpn.Data.MeleeDamageType)
                    {
                        case MeleeDamageType.Blade:
                        case MeleeDamageType.Blunt:
                            {
                                var targetRotation = Utilities.VectorToAngle(dir) +
                                                     (character.Positioning.IsFlipped ? 90 : -90);
                                var rotationDelta = Utilities.DeltaAngle(wpnTransform.Rotation, targetRotation);
                                var rotationOverDistance = rotationDelta / (distanceToTarget / throwSpeed);
                                vel.RotationalVelocity = rotationOverDistance;
                            }
                            break;
                        case MeleeDamageType.Axe:
                            {
                                var targetRotation = Utilities.VectorToAngle(dir);
                                var rotationDelta = Utilities.DeltaAngle(wpnTransform.Rotation, targetRotation) +
                                                    (character.Positioning.IsFlipped ? 360 : -360) * 1.2f; // slightly bump it up to account for drag
                                var rotationOverDistance = rotationDelta / (distanceToTarget / throwSpeed);
                                vel.RotationalVelocity = rotationOverDistance;
                            }
                            break;
                    }

                    break;
                default:
                    if (scene.TryGetComponentFrom<MeasuredVelocityComponent>(wpn.Entity, out var measured))
                        vel.RotationalVelocity = measured.DeltaRotation;

                    break;
            }
        });
    }

    public static void ApplyActiveModifiers(Scene scene, CharacterComponent character)
    {
        var ent = character.Entity;

        foreach (var d in ImprobabilityDisks.Enabled)
            d.Apply(scene, character);
    }
}