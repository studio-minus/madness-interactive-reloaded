using System;
using System.Numerics;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.Physics;

namespace MIR;

/// <summary>
/// A <see cref="Walgelijk.System"/> for making characters move.
/// </summary>
public class CharacterMovementSystem : Walgelijk.System
{
    private QueryResult[] resultBuffer = new QueryResult[1];

    public override void Update()
    {
        if (MadnessUtils.IsPaused(Scene) || MadnessUtils.EditingInExperimentMode(Scene) || MadnessUtils.IsCutscenePlaying(Scene))
            return;

        foreach (var character in Scene.GetAllComponentsOfType<CharacterComponent>())
        {
            if (!character.IsAlive || Scene.HasComponent<ExitDoorComponent>(character.Entity))
                continue;

            ProcessWalking(character);
        }

        Array.Clear(resultBuffer); // ensure GC 
    }

    private void ProcessWalking(CharacterComponent character)
    {
        bool noCollide = false;
        var shouldNoCollideWithOpenPortals = Scene.TryGetTag(character.Entity, out var tag) && tag == Tags.Player; // so far only the player needs to phase through open portals (doors that lead to other levels & level exits)
        shouldNoCollideWithOpenPortals &= !character.Positioning.IsFlying;

        if (shouldNoCollideWithOpenPortals)
            noCollide = OverlapsWithOpenPortal(character); // enable no collide if character overlaps with an open portal / level exit

        bool isWalking = !character.AnimationConstrainsAny(AnimationConstraint.PreventWalking) && character.WalkAcceleration.LengthSquared() > 1;
        var charPos = character.Positioning;

        if (character.Positioning.IsBusyHopping && isWalking)
        {
            var pos = charPos.GlobalTarget;
            pos.X = Utilities.Lerp(charPos.HopStartingPosition, charPos.NextHopPosition, Easings.Quad.InOut(charPos.HopAnimationTimer));
            charPos.CurrentHoppingHeight = MadnessUtils.NormalisedSineWave(charPos.HopAnimationTimer) * charPos.HopTargetHeight * charPos.HopAcceleration;
            pos.Y = charPos.CurrentHoppingHeight;
            charPos.GlobalTarget = pos;
            charPos.FlyingVelocity = 0;
            charPos.HopAnimationTimer += Time.DeltaTime / charPos.HopAnimationDuration;
            if (charPos.HopAnimationTimer >= 1)
            {
                pos = charPos.GlobalTarget;
                pos.X = charPos.NextHopPosition;
                pos.Y = 0;
                charPos.GlobalTarget = pos;
                charPos.HopAnimationTimer = -1;
                if (character.Positioning.Scale > 1)
                {
                    var audio = Utilities.PickRandom(Assets.EnumerateFolder("sounds/mag_footstep"));
                    var snd = SoundCache.Instance.LoadSoundEffect(Assets.Load<FixedAudioData>(audio));
                    Audio.PlayOnce(snd, character.Positioning.Scale * 0.2f, 1, AudioTracks.SoundEffects);

                    MadnessUtils.Shake((character.Positioning.Scale - 1) * 2);
                }
            }
        }
        else if (charPos.IsFlying)
        {
            // they fly now

            const float padding = 250;

            charPos.FlyingVelocity += character.WalkAcceleration.X * Time.DeltaTime * 10;
            charPos.FlyingVelocity = Utilities.SmoothApproach(charPos.FlyingVelocity, 0, 4, Time.DeltaTime);

            var pos = charPos.GlobalTarget;
            var delta = charPos.FlyingVelocity * Time.DeltaTime * 2;

            var pointToTestCollisionFor = charPos.GlobalCenter with { Y = charPos.GlobalCenter.Y + CharacterConstants.HalfHeight / 3 * charPos.Scale };
            var physics = Scene.GetSystem<PhysicsSystem>();
            var isAlreadyInsideWall = !noCollide && (IsOutOfBounds(pointToTestCollisionFor) || physics.QueryPoint(pointToTestCollisionFor, resultBuffer, CollisionLayers.BlockMovement) > 0);

            if (isAlreadyInsideWall)
            {
                var escapeTarget = Level.CurrentLevel?.LevelBounds.GetCenter() ?? resultBuffer[0].Collider.Bounds.GetCenter();
                var dir = Vector2.Normalize(escapeTarget - charPos.GlobalCenter);
                delta = dir.X > 0 ? 10 : -10;
                pos.X += delta;
            }
            else if (physics.Raycast(pointToTestCollisionFor, new Vector2(float.Sign(delta), 0), out var hit, padding, CollisionLayers.BlockMovement))
                pos.X = hit.Position.X + float.Sign(delta) * -padding;
            else
                pos.X += delta;

            pos.Y = 0;
            charPos.CurrentHoppingHeight = 0;
            charPos.GlobalTarget = pos;
        }
        else if (isWalking)
        {
            charPos.HopAcceleration = Utilities.Clamp(charPos.HopAcceleration + 0.4f, 0, 1);
            var delta = character.WalkAcceleration.X * Utilities.RandomFloat(0.6f, 1.2f) * charPos.HopAcceleration;
            charPos.HopAnimationTimer = 0;
            charPos.HopTargetHeight = Utilities.RandomFloat(20, 40);
            charPos.HopStartingPosition = charPos.GlobalTarget.X;

            var pointToTestCollisionFor = charPos.GlobalCenter with { Y = charPos.GlobalCenter.Y + CharacterConstants.HalfHeight / 3 * charPos.Scale };
            var physics = Scene.GetSystem<PhysicsSystem>();
            var isAlreadyInsideWall = !noCollide && (IsOutOfBounds(pointToTestCollisionFor) || physics.QueryPoint(pointToTestCollisionFor, resultBuffer, CollisionLayers.BlockMovement) > 0);

            if (isAlreadyInsideWall)
            {
                var escapeTarget = Level.CurrentLevel?.LevelBounds.GetCenter() ?? resultBuffer[0].Collider.Bounds.GetCenter();
                var dir = Vector2.Normalize(escapeTarget - charPos.GlobalCenter);
                character.WalkAcceleration.X = dir.X > 0 ? 500 : -500;
                delta = character.WalkAcceleration.X;
                charPos.NextHopPosition = charPos.GlobalTarget.X + delta;
                charPos.GlobalTarget += Vector2.UnitX * character.WalkAcceleration.X * 0.2f;
            }
            else if (!noCollide && physics.Raycast(pointToTestCollisionFor, new Vector2(delta > 0 ? 1 : -1, 0), out var hit, MathF.Abs(delta), CollisionLayers.BlockMovement))
                charPos.NextHopPosition = hit.Position.X + (delta > 0 ? -100 : 100);
            else
                charPos.NextHopPosition = charPos.GlobalTarget.X + delta;
        }

        if (isWalking)
        {
            var lf = Scene.GetComponentFrom<LifetimeComponent>(character.Entity);
            float walkCycleSpeed = 18;

            walkCycleSpeed /= character.Positioning.Scale;

            if (character.WalkAcceleration.X > 0)
                walkCycleSpeed *= -1;

            for (int i = 0; i < charPos.Feet.Length; i++)
            {
                var foot = charPos.Feet[i];
                const float offsetPerFoot = MathF.PI;

                int index = charPos.IsFlipped ? i : i + 1;

                var target = new Vector2(
                    (MathF.Cos(lf.Lifetime * walkCycleSpeed + offsetPerFoot * index)) * 10 * charPos.HopAcceleration,
                    (MathF.Sin(lf.Lifetime * walkCycleSpeed + offsetPerFoot * index)) * 10 * charPos.HopAcceleration);

                foot.Offset = Utilities.SmoothApproach(foot.Offset, target, 25, Time.DeltaTime);
                foot.Offset = target;
            }
        }
        else
        {
            charPos.HopAnimationTimer = -1;
            var pos = charPos.GlobalTarget;
            pos.Y = 0;
            charPos.GlobalTarget = pos;
            charPos.HopAcceleration = 0.1f;

            for (int i = 0; i < charPos.Feet.Length; i++)
                charPos.Feet[i].Offset = Utilities.SmoothApproach(charPos.Feet[i].Offset, Vector2.Zero, 25, Time.DeltaTime);
        }
    }

    private bool OverlapsWithOpenPortal(CharacterComponent character)
    {
        float extend = 1000;
        var charBounds = character.GetBoundingBox(Scene);
        foreach (var door in Scene.GetAllComponentsOfType<DoorComponent>())
        {
            if (!(door.Properties.IsPortal || door.Properties.IsLevelProgressionDoor) || !door.IsOpen) // find every open portal
                continue;
            var doorBounds = door.Properties.GetBoundingBox();

            // extend bounding box in opposite direction of door facing direction to create a "tunnel" of some sort
            if (door.Properties.FacingDirection.X < 0)
                doorBounds.MaxX += extend;
            else
                doorBounds.MinX -= extend;

            if (doorBounds.IntersectsRectangle(charBounds))
                return true;
        }
        return false;
    }

    private static bool IsOutOfBounds(Vector2 point) => (Level.CurrentLevel != null && !Level.CurrentLevel.LevelBounds.ContainsPoint(point));
}
