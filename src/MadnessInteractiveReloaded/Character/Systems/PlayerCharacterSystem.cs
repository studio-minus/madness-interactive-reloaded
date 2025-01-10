using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.Onion;

namespace MIR;

/// <summary>
/// Manages the player character.
/// </summary>
public class PlayerCharacterSystem : Walgelijk.System
{
    private KonamiCodeDetector easterEgg = new();

    public override void Update()
    {
        if (MadnessUtils.IsPaused(Scene) || !MadnessUtils.FindPlayer(Scene, out var player, out var character) || MadnessUtils.IsCutscenePlaying(Scene))
            return;

        var experimentMode = MadnessUtils.EditingInExperimentMode(Scene);

        WeaponComponent? equipped = null;
        var uiBlock = Onion.Navigator.IsBeingUsed;

        if (!experimentMode && !character.IsAlive)
        {
            if (!player.IsDoingDyingSequence)
            {
                // increment deaths stat
                if (Scene.FindAnyComponent<GameModeComponent>(out var mode) && mode.Mode == GameMode.Campaign)
                    if (CampaignProgress.TryGetCurrentStats(out var stats) && Level.CurrentLevel != null)
                        stats.IncrementDeaths(Level.CurrentLevel);

                player.IsDoingDyingSequence = true;
                Prefabs.CreatePlayerDeathSequence(Scene);
            }
            return;
        }

        character.EquippedWeapon.TryGet(Scene, out equipped);

        bool prevFlipped = character.Positioning.IsFlipped;

        character.IsIronSighting =
            !experimentMode &&
            character.HasWeaponEquipped &&
            player.RespondToUserInput &&
            Input.ActionHeld(GameAction.BlockAim);

        character.AimOrigin = character.Positioning.GlobalCenter;
        if (equipped != null && equipped.Data.WeaponType is WeaponType.Firearm && Scene.TryGetComponentFrom<TransformComponent>(equipped.Entity, out var equippedTransform))
        {
            var a = character.AimDirection;
            var th = float.Atan2(a.Y, a.X);

            // TODO this is fucked up because its actually determined in CharacterPositionSystem.PositionHands
            // and I just copied the easing and everything to here.
            var ironSightOffset = CharacterConstants.IronsightOffset.Y * Easings.Quad.InOut(character.Positioning.IronSightProgress) * character.Positioning.Scale;
            var barrel = equipped.BarrelEndPoint - equippedTransform.LocalPivot;
            var o = barrel.Y + ironSightOffset;
            character.AimOrigin += Vector2.TransformNormal(new Vector2(0, o * character.Positioning.FlipScaling), Matrix3x2.CreateRotation(th));
        }

        if (!experimentMode)
        {
            if (character.AnimationConstrainsAny(AnimationConstraint.FaceForwards))
                character.AimTargetPosition = (character.Positioning.Head.GlobalPosition + new Vector2(character.Positioning.FlipScaling * 10000, 0));
            else if (!character.AnimationConstrainsAny(AnimationConstraint.PreventAiming))
            {
                character.AimTargetPosition = (player.RespondToUserInput) ?
                    Utilities.NanFallback(Input.WorldMousePosition) :
                    (character.Positioning.Head.GlobalPosition + new Vector2(character.Positioning.FlipScaling * 10000, 0));

                character.RelativeAimTargetPosition = character.AimTargetPosition - character.AimOrigin;
            }
            else
                character.AimTargetPosition = character.AimOrigin + character.RelativeAimTargetPosition;

            if (easterEgg.Detect(Game.State))
            {
                MadnessUtils.Flash(Colors.Yellow.WithAlpha(0.5f), 0.2f);
                foreach (var item in Scene.GetAllComponentsOfType<CharacterComponent>())
                    if (item.Faction.IsEnemiesWith(character.Faction) && item != character)
                    {
                        item.Kill();
                        MadnessUtils.TurnIntoRagdoll(Scene, item, Utilities.RandomPointInCircle() * 140, Utilities.RandomFloat(-90, 90));
                    }
            }
        }

        var targetHandPosition = character.AimTargetPosition - character.AimOrigin;
        float maxHandRange = CharacterConstants.MaxHandRange * (equipped?.Data.MaxHandRangeMultiplier ?? 1) * character.Positioning.Scale;
        if (targetHandPosition.LengthSquared() > (maxHandRange * maxHandRange))
            targetHandPosition = Vector2.Normalize(targetHandPosition) * maxHandRange;
        character.Positioning.HandMousePosition = targetHandPosition;

        if (!experimentMode && character.HasWeaponEquipped && equipped != null)
        {
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
            //character.Positioning.HandPositionMode = HandPositionMode.TwoHands;
            equipped = null;
        }

        var shouldBeDirectedRight = character.AimTargetPosition.X > character.Positioning.GlobalCenter.X;
        character.WalkAcceleration = Vector2.Zero;

        if (!experimentMode && player.RespondToUserInput)
        {
            var nearestWeapon = GetNearestWeapon(character, out var nearestWeaponDistance, out var nearestWeaponEntity);
            if (nearestWeapon != null)
            {
                nearestWeapon.ShouldBeHighlighted = true;
                if (player.LastWeaponHoveredOver != nearestWeaponEntity)
                {
                    player.LastWeaponHoveredOver = nearestWeaponEntity;
                    // Prefabs.CreateNotification(
                    //     Scene,
                    //     Scene.GetComponentFrom<TransformComponent>(phys.Head.Entity).Position + new Vector2(0, 250),
                    //     nearestWeapon.Stats.Name, 0.4f);
                }
            }

            if (Input.IsKeyHeld(Key.LeftControl) && float.Abs(Input.MouseScrollDelta) > float.Epsilon)
            {
                if (Input.MouseScrollDelta > 0)
                    player.ZoomLevel /= 0.9f;
                else
                    player.ZoomLevel *= 0.9f;

                player.ZoomLevel = Utilities.Clamp(player.ZoomLevel, 1, 2);
            }

            //if (Scene.FindAnyComponent<CameraMovementComponent>(out var cm))
            //{
            //    cm.DeltaZoom = Utilities.SmoothApproach(cm.DeltaZoom, 1 / player.ZoomLevel, 15, Time.DeltaTime);
            //    cm.DeltaPosition = Utilities.Lerp(Vector2.Zero, character.Positioning.Body.ComputedVisualCenter - cm.Position, Utilities.MapRange(1, 2, 0, 0.5f, player.ZoomLevel));
            //}
            //cm.DeltaZoom = Utilities.Lerp(cm.DeltaZoom, 1 / player.ZoomLevel, Utilities.LerpDt(0.9995f, Time.DeltaTime));

            if (Input.ActionPressed(GameAction.Interact) && !character.AnimationConstrainsAny(AnimationConstraint.PreventWorldInteraction))
            {
                PickUpWeapon(character, nearestWeapon);
                character.EquippedWeapon.TryGet(Scene, out equipped);
            }

            //FIREARM CONTROL
            if (!uiBlock && character.HasWeaponEquipped && equipped != null && equipped.Data.WeaponType == WeaponType.Firearm && !character.AnimationConstrainsAny(AnimationConstraint.PreventShooting))
            {
                if (Input.ActionPressed(GameAction.Attack) && !equipped.HasRoundsLeft && !ImprobabilityDisks.IsEnabled("infinite_ammo"))
                {
                    var weaponPos = Scene.GetComponentFrom<TransformComponent>(character.EquippedWeapon.Entity).Position;
                    //Prefabs.CreateNotification(
                    //    Scene,
                    //    weaponPos + new Vector2(0, 90),
                    //    "Out of ammo", 0.5f);

                    Audio.PlayOnce(Sounds.DryFire);
                }

                if (equipped.Data.Automatic)
                {
                    if (Input.ActionHeld(GameAction.Attack))
                        equipped.IsFiring = true;
                }
                else if (Input.ActionPressed(GameAction.Attack))
                    equipped.IsFiring = true;
            }

            // MELEE CONTROL
            character.IsMeleeBlocking = false;
            if (!character.HasWeaponEquipped || equipped?.Data.WeaponType == WeaponType.Melee)
            {
                character.IsMeleeBlocking = character.HasWeaponEquipped && character.IsIronSighting && !character.AnimationConstrainsAny(AnimationConstraint.PreventBlock);

                if (!uiBlock && (Input.ActionPressed(GameAction.Attack) || Input.ActionPressed(GameAction.Melee)))
                    MeleeUtils.TryPerformMeleeAttack(Scene, equipped, character);

                if (!character.AnimationConstrainsAny(AnimationConstraint.PreventFlip))
                    character.Positioning.IsFlipped = !shouldBeDirectedRight;
            }
            else if (character.HasWeaponEquipped && equipped != null && equipped.Data.WeaponType == WeaponType.Firearm)
            {
                if (Input.ActionPressed(GameAction.Melee))
                {
                    if (!character.AnimationConstrainsAny(AnimationConstraint.PreventFlip))
                        character.Positioning.IsFlipped = !shouldBeDirectedRight;
                    MeleeUtils.TryPerformMeleeAttack(Scene, equipped, character);
                }
            }

            if (Input.ActionPressed(GameAction.Throw))
                CharacterUtilities.TryThrowWeapon(Scene, character);

            if (character.AllowWalking && (!character.AnimationConstrainsAny(AnimationConstraint.PreventWalking) || character.Stats.AgilitySkillLevel is AgilitySkillLevel.Master)) // TODO this is not pretty
            {
                if (Input.ActionPressed(GameAction.Left) || Input.ActionPressed(GameAction.Right))
                {
                    Scene.GetComponentFrom<LifetimeComponent>(player.Entity).Lifetime = 0;
                    character.Positioning.HopAnimationTimer = -1;
                    character.Positioning.HopStartingPosition = character.Positioning.GlobalCenter.X;
                }

                if (!(Input.ActionHeld(GameAction.Right) && Input.ActionHeld(GameAction.Left)))
                {
                    float multiplier = character.IsIronSighting ? 0.5f : 1;

                    if (ImprobabilityDisks.IsEnabled("fast_walking"))
                        multiplier *= 1.5f;

                    if (Input.ActionHeld(GameAction.Right))
                    {
                        if (!character.IsPlayingAnimation)
                            character.Positioning.IsFlipped = !shouldBeDirectedRight;
                        character.WalkAcceleration = new Vector2(character.Positioning.TopWalkSpeed * multiplier, 0);
                    }

                    if (Input.ActionHeld(GameAction.Left))
                    {
                        if (!character.IsPlayingAnimation)
                            character.Positioning.IsFlipped = !shouldBeDirectedRight;
                        character.WalkAcceleration = new Vector2(-character.Positioning.TopWalkSpeed * multiplier, 0);
                    }
                }
            }

            //dit moet hier beneden zijn want Acceleration moet al aangepast zijn door de controls :)
            if (Input.ActionPressed(GameAction.JumpDodge))
                CharacterUtilities.TryJumpDodge(Scene, character);
        }

        if (character.IsIronSighting && !character.IsPlayingAnimation)
            character.Positioning.IsFlipped = !shouldBeDirectedRight;

        if (character.Positioning.IsFlipped != prevFlipped)
            character.NeedsLookUpdate = true;
    }

    private WeaponComponent? GetNearestWeapon(CharacterComponent character, out float distance, out Entity weaponEntity)
    {
        float minDistanceSoFar = float.MaxValue;
        WeaponComponent? nearest = null;
        weaponEntity = default;

        foreach (var w in Scene.GetAllComponentsOfType<WeaponComponent>())
        {
            if (!w.HasRoundsLeft || w.Wielder.IsValid(Scene))
                continue;

            if (Scene.HasComponent<ThrowableProjectileComponent>(w.Entity))
                continue;

            var transform = Scene.GetComponentFrom<TransformComponent>(w.Entity);
            if (Vector2.DistanceSquared(transform.Position, character.Positioning.GlobalCenter) > (character.HandPickupRange * character.HandPickupRange) * character.Stats.Scale)
                continue;

            var d = Vector2.DistanceSquared(transform.Position, Input.WorldMousePosition);

            // scoring
            if (w.Data.WeaponType is WeaponType.Melee)
                d *= 0.5f;
            else
            {
                d /= float.Lerp(w.RemainingRounds, 1, 0.5f);
                d /= float.Lerp(w.Data.Damage, 1, 0.5f);
            }

            if (d < minDistanceSoFar)
            {
                minDistanceSoFar = d;
                weaponEntity = w.Entity;
                nearest = w;
            }
        }

        distance = minDistanceSoFar;
        return nearest;
    }

    private void PickUpWeapon(CharacterComponent character, WeaponComponent? weapon)
    {
        CharacterUtilities.PickupWithAnimation(Scene, character, weapon);
    }
}
