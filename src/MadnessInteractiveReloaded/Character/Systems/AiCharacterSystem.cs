using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.Physics;
using static System.Formats.Asn1.AsnWriter;

namespace MIR;
/// <summary>
/// A <see cref="Walgelijk.System"/> for running AI character logic.
/// </summary>
public class AiCharacterSystem : Walgelijk.System
{
    /// <summary>
    /// The maximum number of AI the system will process.
    /// </summary>
    public const int MaxAiCount = 128;
    private readonly AiComponent[] aiBuffer = new AiComponent[MaxAiCount];

    public static bool DisableAI = false;
    public static bool AutoSpawn = false;

    // maak het zo dat enemies elkaar ontwijken
    // maak het zo dat enemies langzamer achteruit lopen dan vooruit
    // maak het zo dat enemies een beetje rondlopen als ze geen target hebben

    public override void Update()
    {
        if (DisableAI)
            return;

        if (MadnessUtils.IsPaused(Scene) || MadnessUtils.IsCutscenePlaying(Scene))
            return;

        var experimentMode = MadnessUtils.EditingInExperimentMode(Scene);

        if (!experimentMode)
            foreach (var ai in Scene.GetAllComponentsOfType<AiComponent>())
            {
                if (ai.HasKillTarget &&
                    Scene.TryGetComponentFrom<CharacterComponent>(ai.Entity, out var aiChar) && aiChar.IsAlive &&
                    ai.KillTarget.TryGet(Scene, out var targetCharacter))
                {
                    if (targetCharacter.CurrentAttackerCount >=
                        (Level.CurrentLevel?.MaxSimultaneousAttackingEnemies ?? 2))
                        ai.TooBusyToAttack = true;
                    else
                    {
                        targetCharacter.CurrentAttackerCount++;
                        ai.TooBusyToAttack = false;
                    }
                }
            }

        WeaponComponent? equipped = null;
        CharacterComponent? killTargetChar = null;
        var allAi = Scene.GetAllComponentsOfType(aiBuffer);

        foreach (var ai in allAi)
        {
            equipped = null;
            killTargetChar = null;

            var entity = ai.Entity;
            var character = Scene.GetComponentFrom<CharacterComponent>(entity);
            var transform = Scene.GetComponentFrom<TransformComponent>(entity);

            if (!character.IsAlive)
            {
                if (!character.HasBeenRagdolled && !character.IsPlayingAnimationGroup("deaths"))
                    MadnessUtils.TurnIntoRagdoll(Scene, character);
                continue;
            }

            bool prevFlipped = character.Positioning.IsFlipped;

            if (ai.HasKillTarget && (
                    ai.IsDocile ||
                    !ai.KillTarget.TryGet(Scene, out killTargetChar) ||
                    !killTargetChar.IsAlive))
                ai.HasKillTarget = false;

            if (ai.HasItemTarget && (!ai.ItemTarget.TryGet(Scene, out WeaponComponent? itemTargetWeapon) ||
                                     itemTargetWeapon.Wielder.IsValid(Scene) ||
                                     !itemTargetWeapon.HasRoundsLeft))
            {
                ai.HasItemTarget = false;
            }

            if (!ai.IsDocile && !ai.HasKillTarget && !experimentMode)
            {
                ai.AttackModeDuration = 0;
                FindKillTarget(ai, character, transform);
            }
            else if (!experimentMode)
                ai.AttackModeDuration += Time.DeltaTime;

            // I don't remember why this is necessary but it looks unhinged. What is killTargetChar and why does it exist?
            if (killTargetChar == null)
                ai.KillTarget.TryGet(Scene, out killTargetChar);

            character.IsIronSighting = character.HasWeaponEquipped && ai.WantsToIronSight.Value;

            var originalPos = character.Positioning.Body.ComputedVisualCenter;

            var aimingSource = originalPos;
            if (!experimentMode)
            {
                if (character.EquippedWeapon.TryGet(Scene, out equipped))
                {
                    aimingSource.Y += equipped.BarrelEndPoint.Y;

                    if (equipped.Data.WeaponType == WeaponType.Firearm)
                        originalPos.Y += 100 * character.Positioning.IronSightProgress;
                    // TODO where does this 100 number come from???

                    if (equipped.Data.WeaponType is WeaponType.Melee && MadnessUtils.TimeSafeRandom(ai.Seed - 42.23f) > 0.2f) // fuck melee weapons
                        FindItemTarget(ai, transform);
                }
                else
                {
                    equipped = null;
                    if (MadnessUtils.TimeSafeRandom(ai.Seed + 2239) > 0.6f && !ai.HasItemTarget)
                        //TODO chance of them doing this (picking up weapons in the middle of a fight) should be higher for more skilled enemies
                        FindItemTarget(ai, transform);
                }
            }

            var targetHandPosition = ai.AimingPosition - originalPos;
            var aimingDirection = ai.AimingPosition - aimingSource;
            float maxHandRange = CharacterConstants.MaxHandRange * (equipped?.Data.MaxHandRangeMultiplier ?? 1) * character.Positioning.Scale;
            if (targetHandPosition.LengthSquared() > (maxHandRange * maxHandRange))
                targetHandPosition = MadnessVector2.Normalize(targetHandPosition) * maxHandRange;

            character.Positioning.HandMousePosition = targetHandPosition;

            if (!experimentMode)
                character.AimTargetPosition = ai.AimingPosition;

            if (!experimentMode && !ai.IsDocile && character.HasWeaponEquipped && equipped != null)
            {
                if (!equipped.HasRoundsLeft)
                {
                    equipped = null;
                    character.DropWeapon(Scene);
                    continue;
                }

                switch (equipped.Data.WeaponType)
                {
                    case WeaponType.Firearm:
                        if (equipped.HoldPoints.Length > 1)
                            character.Positioning.HandPositionMode = HandPositionMode.TwoHands;
                        else
                            character.Positioning.HandPositionMode = HandPositionMode.OneHand;
                        break;
                    default:
                    case WeaponType.Melee:
                        if (equipped.HoldPoints.Length > 1)
                            character.Positioning.HandPositionMode = HandPositionMode.TwoHands;
                        else
                            character.Positioning.HandPositionMode = HandPositionMode.OneHand;
                        break;
                }
            }
            else
            {
                character.Positioning.HandPositionMode = HandPositionMode.None;
                equipped = null;
            }

            if (!experimentMode && ai.WantsToInteract.Value && !character.AnimationConstrainsAny(AnimationConstraint.PreventWorldInteraction))
                PickupNearestWeapon(transform.Position, character, ai);
            character.EquippedWeapon.TryGet(Scene, out equipped);

            if (!experimentMode && !ai.IsDocile &&
                !ai.IsDoingAccurateShot &&
                character.HasWeaponEquipped &&
                equipped != null &&
                equipped.Data.WeaponType == WeaponType.Firearm &&
                !MadnessUtils.IsTargetedByAccurateShot(Scene, ai.KillTarget.Entity))
            {
                if (ai.WantsToShoot.BecameTrue && !equipped.HasRoundsLeft)
                    Audio.PlayOnce(Sounds.DryFire);

                if (ai.WantsToDoAccurateShot.BecameTrue &&
                    ai.PanicLevel < 0.1f &&
                    (Time.SecondsSinceLoad - AiComponent.LastAccurateShotTime) > CharacterConstants.AccurateShotCooldown &&
                    killTargetChar != null)
                {
                    try
                    {
                        CharacterUtilities.DoAccurateShot(Scene, new ComponentRef<CharacterComponent>(ai.Entity), ai.KillTarget);
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e.Message, nameof(AiCharacterSystem));
                        return;
                    }
                }
                else
                {
                    if (equipped.Data.Automatic)
                    {
                        if (ai.WantsToShoot.Value)
                            equipped.IsFiring = true;
                    }
                    else if (ai.WantsToShoot.BecameTrue)
                        equipped.IsFiring = true;
                }
            }

            var shouldBeDirectedRight = character.AimTargetPosition.X > transform.Position.X;
            character.IsMeleeBlocking = false;

            // MELEE CONTROL
            if (!experimentMode && !ai.IsDocile && !ai.TooBusyToAttack && (!character.HasWeaponEquipped || equipped?.Data.WeaponType == WeaponType.Melee))
            {
                if (!character.AnimationConstrainsAny(AnimationConstraint.PreventBlock))
                    character.IsMeleeBlocking = character.HasWeaponEquipped && character.IsIronSighting;

                if (!character.IsMeleeBlocking && ai.WantsToShoot.Value && !ai.IsDoingAccurateShot && !MadnessUtils.IsTargetedByAccurateShot(Scene, ai.KillTarget.Entity))
                {
                    if (ai.HasKillTarget && !killTargetChar.Positioning.IsFlying)
                        MeleeUtils.TryPerformMeleeAttack(Scene, equipped, character);
                }

                if (!character.AnimationConstrainsAny(AnimationConstraint.PreventFlip))
                    character.Positioning.IsFlipped = !shouldBeDirectedRight;
            }

            if (!experimentMode && (ai.WantsToWalkLeft.BecameTrue || ai.WantsToWalkRight.BecameTrue))
            {
                Scene.GetComponentFrom<LifetimeComponent>(entity).Lifetime = 0;
                character.Positioning.HopAnimationTimer = -1;
                character.Positioning.HopStartingPosition = transform.Position.X;
            }

            if (!character.IsPlayingAnimation) // no need to check for animation constraint here because this wont pass if any animation is playing in the first place
                character.Positioning.IsFlipped = !shouldBeDirectedRight;

            if (!Scene.HasComponent<ExitDoorComponent>(character.Entity))
            {
                character.WalkAcceleration = Vector2.Zero;

                if (!experimentMode && !(ai.WantsToWalkRight.Value && ai.WantsToWalkLeft.Value) && character.AllowWalking)
                {
                    float multiplier = character.IsIronSighting ? 0.5f : 1;

                    if (ai.WantsToWalkRight.Value)
                    {
                        if (!character.AnimationConstrainsAny(AnimationConstraint.PreventFlip))
                            character.Positioning.IsFlipped = !shouldBeDirectedRight;

                        if (character.Positioning.IsFlipped)
                            multiplier *= 0.5f;

                        character.WalkAcceleration = new Vector2(character.Positioning.TopWalkSpeed * multiplier, 0);
                    }

                    if (ai.WantsToWalkLeft.Value)
                    {
                        if (!character.AnimationConstrainsAny(AnimationConstraint.PreventFlip))
                            character.Positioning.IsFlipped = !shouldBeDirectedRight;

                        if (!character.Positioning.IsFlipped)
                            multiplier *= 0.5f;

                        character.WalkAcceleration = new Vector2(-character.Positioning.TopWalkSpeed * multiplier, 0);
                    }
                }
            }
            //else
            //{
            //    character.Positioning.IsFlipped = aimingDirection.X < 0;
            //}

            if (character.Positioning.IsFlipped != prevFlipped)
                character.NeedsLookUpdate = true;

            originalPos = Scene.GetComponentFrom<TransformComponent>(character.Positioning.Head.Entity).Position;

            ai.PanicLevel = Utilities.Clamp(ai.PanicLevel - Time.DeltaTime, 0, 1);

            ai.SyncVariables();

            ai.WantsToShoot.Value = false;
            ai.WantsToWalkRight.Value = false;
            ai.WantsToWalkLeft.Value = false;
            ai.WantsToDoAccurateShot.Value = false;
            ai.WantsToInteract.Value = false;
            ai.WantsToIronSight.Value = ai.IsDoingAccurateShot;

            if (!experimentMode)
            {

                if (ai.HasKillTarget && killTargetChar != null)
                {
                    if (character.AnimationConstrainsAny(AnimationConstraint.FaceForwards))
                        ai.AimingPosition = (character.Positioning.Head.GlobalPosition + new Vector2(character.Positioning.FlipScaling * 10000, 0));
                    else if (!character.AnimationConstrainsAny(AnimationConstraint.PreventAiming))
                    {
                        ai.AimingPosition = Noise.GetValue(-113.234f, Time.SecondsSinceLoad * 0.05f, ai.Seed) > -30
                            ? killTargetChar.Positioning.Head.GlobalPosition
                            : killTargetChar.Positioning.Body.ComputedVisualCenter;

                        var nonRandomAimingPos = ai.AimingPosition;

                        if (character.HasWeaponEquipped)
                        {
                            var aimRandom = MadnessUtils.Noise2D(Time.SecondsSinceLoad * 0.343f, ai.Seed);
                            ai.AimingPosition += aimRandom *
                                                 (character.Stats.AimingRandomness / 2) *
                                                 Vector2.Distance(ai.AimingPosition, transform.Position);
                        }

                        character.RelativeAimTargetPosition = ai.AimingPosition - originalPos;
                    }
                    else
                        ai.AimingPosition = originalPos + character.RelativeAimTargetPosition;

                    if (!character.AnimationConstrainsAny(AnimationConstraint.PreventBlock))
                    {
                        if (character.HasWeaponEquipped && equipped != null)
                        {
                            if (!ai.IsDoingAccurateShot && character.Stats.CanDeflect)
                            {
                                if (killTargetChar.HasWeaponOfType(Scene, WeaponType.Melee))
                                    ai.WantsToIronSight.Value = Noise.GetValue(ai.Seed * 23.323f, 1, Time * 0.2f) < 0.5f && killTargetChar.IsPlayingAnimationGroup("melee");
                                else
                                    ai.WantsToIronSight.Value = Noise.GetValue(ai.Seed * -23.323f, ai.Seed, Time * 0.2f) < 0.5f &&
                                        (ai.PanicLevel > 0 || character.DodgeMeter < character.Stats.DodgeAbility) && equipped.Data.CanDeflectBullets;
                            }
                        }
                        else
                            ai.WantsToIronSight.Value = killTargetChar.IsPlayingAnimationGroup("melee");
                    }

                    if (!character.AnimationConstrainsAll(AnimationConstraint.PreventAllAttacking))
                        if (equipped != null)
                        {
                            ProcessAttacking(ai, character, equipped, killTargetChar);
                        }
                        else if (!character.AnimationConstrainsAny(AnimationConstraint.PreventMelee)) // unarmed :)
                        {
                            ai.AttackModeDuration = 0;
                            //ai.WantsToIronSight.Value = false;

                            if (!ai.HasItemTarget)
                            {
                                if (ai.WantsToPickupWeapon)
                                {
                                    FindItemTarget(ai, transform);
                                    if (!ai.HasItemTarget)
                                        ai.WantsToPickupWeapon = false; //still no weapon? i will go in with my fists
                                }
                                else //no weapon and doesnt want to pick up a weapon, so the AI probaby wants to have a fist fight
                                {
                                    if (ai.HasKillTarget && MadnessUtils.TimeSafeRandom(ai.Seed * 0.932f) > 0.1f)
                                    {
                                        if (!ai.TooBusyToAttack)
                                        {
                                            ai.WantsToShoot.Value = MathF.Abs(transform.Position.X - ai.AimingPosition.X) < CharacterConstants.MaxHandRange * 2;
                                            //if (!ai.WantsToShoot.Value)
                                            WalkTowards(ai.AimingPosition.X, ConVars.Instance.EnemyMeleeDistance.X, ConVars.Instance.EnemyMeleeDistance.Y, ai);
                                        }
                                        else
                                        {
                                            ai.WantsToShoot.Value = false;
                                            WalkTowards(ai.AimingPosition.X, ConVars.Instance.EnemySafeDistanceFromPlayer, Level.CurrentLevel?.LevelBounds.Width ?? 6000, ai);
                                        }
                                    }
                                    else
                                        ai.WantsToPickupWeapon = true;
                                }
                            }
                            else if (!character.AnimationConstrainsAny(AnimationConstraint.PreventWalking | AnimationConstraint.PreventWorldInteraction))
                            {
                                //walk to and pick up item
                                var itemTransform = Scene.GetComponentFrom<TransformComponent>(ai.ItemTarget.Entity);
                                if (itemTransform.Position.X > transform.Position.X)
                                    ai.WantsToWalkRight.Value = true;
                                else
                                    ai.WantsToWalkLeft.Value = true;

                                if (Vector2.Distance(itemTransform.Position, transform.Position) < character.HandPickupRange * 2)
                                    ai.WantsToInteract.Value = true;

                                ai.AimingPosition = itemTransform.Position;
                            }
                        }
                }
                else if (ai.HasItemTarget && equipped == null)
                {
                    if (!character.AnimationConstrainsAny(AnimationConstraint.PreventWalking | AnimationConstraint.PreventWorldInteraction))
                    {
                        //walk to and pick up item
                        var itemTransform = Scene.GetComponentFrom<TransformComponent>(ai.ItemTarget.Entity);
                        if (itemTransform.Position.X > transform.Position.X)
                            ai.WantsToWalkRight.Value = true;
                        else
                            ai.WantsToWalkLeft.Value = true;

                        if (Vector2.Distance(itemTransform.Position, transform.Position) < character.HandPickupRange * 2)
                            ai.WantsToInteract.Value = true;

                        ai.AimingPosition = itemTransform.Position;
                    }
                }
                else
                {
                    Wander(ai, character);
                }

            }
            // TODO slow
            //const float repulseDistance = 200;
            //foreach (var other in allAi)
            //{
            //    if (other.Entity == ai.Entity)
            //        continue;

            //    var otherPos = Scene.GetComponentFrom<CharacterComponent>(other.Entity).Positioning.GlobalCenter.X;

            //    var delta = character.Positioning.GlobalCenter.X - otherPos;
            //    var dist = float.Abs(delta);
            //    if (dist < repulseDistance)
            //        character.WalkAcceleration.X += 200 * float.Sign(delta);
            //}
        }
    }

    private void Wander(AiComponent ai, CharacterComponent character)
    {
        ref float target = ref ai.WanderTargetPosition;
        var ally = FindAlly(ai, character);
        if (ally != null)
        {
            ai.WanderTargetRemainingTime = Utilities.RandomFloat(2, 20);
            target = ally.Positioning.GlobalCenter.X;
        }
        else if (Level.CurrentLevel != null && ai.WanderTargetRemainingTime <= 0)
        {
            ai.WanderTargetRemainingTime = Utilities.RandomFloat(2, 20);
            target = Utilities.RandomFloat(Level.CurrentLevel.FloorLine.FirstOrDefault().X, Level.CurrentLevel.FloorLine.LastOrDefault().X);
        }

        ai.WanderTargetRemainingTime -= Time.DeltaTime;

        if (float.Abs(target - character.Positioning.GlobalCenter.X) > 400)
        {
            ai.AimingPosition = character.Positioning.Head.GlobalPosition + new Vector2(float.Sign(target - character.Positioning.GlobalCenter.X) * 1000, 0);
        }

        WalkTowards(target, 0, 1000, ai);
    }

    private void ProcessAttacking(AiComponent ai, CharacterComponent character, WeaponComponent equipped, CharacterComponent killTargetChar)
    {
        switch (equipped.Data.WeaponType)
        {
            case WeaponType.Firearm:
                {
                    if (!character.AnimationConstrainsAny(AnimationConstraint.PreventShooting))
                    {
                        if (ai.AttackModeDuration > character.Stats.ShootingTimeout && (ai.TooBusyToAttack ||
                                float.Abs(ai.AimingPosition.X - character.Positioning.GlobalCenter.X) < 1800))
                        {
                            var barrel = WeaponSystem.GetBarrel(equipped, Scene.GetComponentFrom<TransformComponent>(equipped.Entity));

                            var lineOfSight = Scene.GetSystem<PhysicsSystem>().Raycast(
                                barrel.position, barrel.direction, out var hit,
                                ignore: character.AttackIgnoreCollision.Concat(Scene.GetAllComponentsOfType<IgnoreLineOfSightComponent>().Select(static e => e.Entity)));
                            // TODO is this... slow?

                            if (lineOfSight &&
                                (hit.Entity == killTargetChar.Positioning.Head.Entity ||
                                 hit.Entity == killTargetChar.Positioning.Body.Entity || ai.TooBusyToAttack))
                            {
                                var shootChance = Noise.GetValue(ai.Seed, Time.SecondsSinceLoad * 3.412f, 0) * 0.5f + 0.5f;

                                if (ai.TooBusyToAttack)
                                    shootChance *= 0.75f;

                                shootChance *= 0.9f;

                                var enoughTimeSinceAccurateShot =
                                    (Time.SecondsSinceLoad - AiComponent.LastAccurateShotTime) >
                                    CharacterConstants.AccurateShotWarningDuration;

                                if (!ai.TooBusyToAttack && enoughTimeSinceAccurateShot &&
                                    character.Stats.AccurateShotChance > shootChance * 1.5f)
                                    ai.WantsToDoAccurateShot.Value = true;
                                else
                                    ai.WantsToShoot.Value = shootChance > 0.5f;
                            }
                        }
                    }

                    if (ai.TooBusyToAttack)
                        WalkTowards(ai.AimingPosition.X, ConVars.Instance.EnemySafeDistanceFromPlayer, Level.CurrentLevel?.LevelBounds.Width ?? 6000, ai);
                    else
                    {
                        var minDist = ConVars.Instance.EnemyGunDistance.X;
                        var maxDist = ConVars.Instance.EnemyGunDistance.Y;

                        var targetWeaponLength = killTargetChar.GetWeaponBarrelDistance(Scene);
                        minDist = MathF.Max(targetWeaponLength, minDist);
                        maxDist = MathF.Max(targetWeaponLength, maxDist);

                        WalkTowards(ai.AimingPosition.X, minDist, maxDist, ai);
                    }
                }
                break;
            case WeaponType.Melee:
                //if (!character.AnimationConstrainsAny(AnimationConstraint.PreventMelee))
                ai.WantsToShoot.Value =
                    Noise.GetValue(ai.Seed, 512.534f, Time.SecondsSinceLoad * 0.4f) > 0 &&
                    MathF.Abs(character.Positioning.GlobalCenter.X - killTargetChar.Positioning.GlobalCenter.X) < equipped.Data.Range + 200;

                // if (Noise.GetValue(ai.Seed, -512.534f, Time.SecondsSinceLoad * 0.1f) > 0.5f)
                //if (false)
                {
                    if (!ai.TooBusyToAttack)
                        WalkTowards(ai.AimingPosition.X, equipped.Data.Range, ConVars.Instance.EnemyMeleeDistance.Y, ai);
                    else
                        WalkTowards(ai.AimingPosition.X, ConVars.Instance.EnemySafeDistanceFromPlayer, Level.CurrentLevel?.LevelBounds.Width ?? 6000, ai);
                }
                break;
        }
    }

    private void FindKillTarget(AiComponent ai, CharacterComponent character, TransformComponent transform)
    {
        if (ai.IsDocile)
        {
            ai.HasKillTarget = false;
            return;
        }

        float minDistance = float.MaxValue;
        Entity? found = null;

        foreach (var other in Scene.GetAllComponentsOfType<CharacterComponent>())
        {
            if (other == character
                || !other.IsAlive
                || !character.Faction.IsEnemiesWith(other.Faction))
                continue;

            var otherTransform = Scene.GetComponentFrom<TransformComponent>(other.Entity);
            var d = Vector2.DistanceSquared(transform.Position, otherTransform.Position);
            if (d < minDistance)
            {
                minDistance = d;
                found = other.Entity;
            }
        }

        ai.HasKillTarget = found.HasValue;
        if (found.HasValue)
            ai.KillTarget = new ComponentRef<CharacterComponent>(found.Value);
    }

    private void FindItemTarget(AiComponent ai, TransformComponent transform)
    {
        const float maxSpeedSqrd = 25 * 25;
        float minDistance = ConVars.Instance.EnemyWeaponSearchRange * ConVars.Instance.EnemyWeaponSearchRange;
        Entity? found = null;

        foreach (var weapon in Scene.GetAllComponentsOfType<WeaponComponent>())
        {
            if (!weapon.HasRoundsLeft ||
                weapon.StuckInsideParams.HasValue ||
                weapon.IsAttachedToWall ||
                Scene.HasComponent<ThrowableProjectileComponent>(weapon.Entity) ||
                weapon.Wielder.IsValid(Scene))
                continue;

            // cant pick up weapons that have only just dropped on the floor
            if (Scene.TryGetComponentFrom<DespawnComponent>(weapon.Entity, out var dd))
                if (dd.Timer < 1) // TODO convar
                    continue;

            var otherTransform = Scene.GetComponentFrom<TransformComponent>(weapon.Entity);
            var d = Vector2.DistanceSquared(transform.Position, otherTransform.Position);
            if (d < minDistance)
            {
                if (Scene.TryGetComponentFrom<VelocityComponent>(weapon.Entity, out var vel) && vel.Velocity.LengthSquared() > maxSpeedSqrd)
                    continue;

                minDistance = d;
                found = weapon.Entity;
            }
        }

        ai.HasItemTarget = found.HasValue;
        if (found.HasValue)
            ai.ItemTarget = new(found.Value);
    }

    private CharacterComponent? FindAlly(AiComponent ai, CharacterComponent character)
    {
        // we should really only follow the player if they are our ally
        if (MadnessUtils.FindPlayer(Scene, out _, out var player) && character.Faction.IsAlliedTo(player.Faction))
        {
            return player;
        }
        return null;
    }

    private void WalkTowards(float x, float minimumDistance, float maximumDistance, AiComponent ai)
    {
        var pos = Scene.GetComponentFrom<CharacterComponent>(ai.Entity).Positioning.GlobalCenter;

        float distanceToTarget = MathF.Abs(pos.X - x);
        ai.WantsToWalkLeft.Value = false;
        ai.WantsToWalkRight.Value = false;
        if (distanceToTarget < minimumDistance && !(Level.CurrentLevel?.IsCloseToEdge(pos.X, 1000) ?? false)) // too close to target and not in the wall
        {
            if (x > pos.X)
                ai.WantsToWalkLeft.Value = true;
            else
                ai.WantsToWalkRight.Value = true;
        }
        else if (distanceToTarget > maximumDistance) // too far from target
        {
            if (x < pos.X)
                ai.WantsToWalkLeft.Value = true;
            else
                ai.WantsToWalkRight.Value = true;
        }
        else if (Noise.GetSimplex(-1439.234f, Time.SecondsSinceLoad * 0.05f, ai.Seed) > 0.2f)
        {
            if (Noise.GetValue(0, Time.SecondsSinceLoad * 0.25f, ai.Seed) > 0.0f)
                ai.WantsToWalkRight.Value = true;
            else
                ai.WantsToWalkLeft.Value = true;
        }
    }

    private void PickupNearestWeapon(Vector2 point, CharacterComponent character, AiComponent ai)
    {
        if (Scene.HasComponent<CharacterPickupComponent>(character.Entity))
            return;

        if (character.HasWeaponEquipped)
            return;

        float minDistanceSoFar = (character.HandPickupRange * character.HandPickupRange) * character.Stats.Scale + 150;
        WeaponComponent? nearest = null;

        foreach (var item in Scene.GetAllComponentsOfType<WeaponComponent>())
        {
            if (item.Wielder.IsValid(Scene) || !item.HasRoundsLeft || item.StuckInsideParams.HasValue || item.IsAttachedToWall)
                continue;

            var vel = Scene.GetComponentFrom<VelocityComponent>(item.Entity);
            if (vel.Velocity.LengthSquared() > 25 * 25)
                continue;

            var transform = Scene.GetComponentFrom<TransformComponent>(item.Entity);
            var d = Vector2.DistanceSquared(transform.Position, point);
            if (d < minDistanceSoFar)
            {
                minDistanceSoFar = d;
                nearest = item;
            }
        }


        // TODO why not use CharacterUtilities.PickupWithAnimation? because of ai.WantsToPickupWeapon = false;

        if (nearest != null)
        {
            float delay = 0;

            if (!Scene.HasComponent<JumpDodgeComponent>(character.Entity))
            {
                var d = Scene.AttachComponent(character.Entity, new CharacterPickupComponent
                {
                    Target = new(nearest.Entity)
                });
                delay = d.PickupTime * d.Duration;
            }

            ai.WantsToPickupWeapon = false;
            MadnessUtils.DelayPausable(delay, () => // TODO what if the scene changes? ideally, these routines should be erased on scene change
            {
                if (character.EquipWeapon(Scene, nearest))
                {
                    var pickupAssets = Assets.EnumerateFolder("sounds/pickup");
                    var data = Assets.Load<FixedAudioData>(Utilities.PickRandom(pickupAssets));
                    Audio.PlayOnce(SoundCache.Instance.LoadSoundEffect(data));
                }
            });
        }
    }
}