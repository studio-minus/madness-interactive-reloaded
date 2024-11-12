using System.Collections.Generic;
using System.Numerics;
using Walgelijk;
using Walgelijk.Physics;

namespace MIR;

public class TeleportDashAbilityComponent : CharacterAbilityComponent
{
    public TeleportDashAbilityComponent(AbilitySlot slot) : base(slot, AbilityBehaviour.Hold)
    {
    }

    //TODO idk if this ability makes any sense ³¤
    public override string DisplayName => "Teleport";
    public float Distance = 1500;
    private bool flipFlop = false;
    private QueryResult[] buffer = new QueryResult[4];
    private bool routine = false;

    public override AnimationConstraint Constraints => routine ?
        AnimationConstraint.PreventDying | AnimationConstraint.PreventAllMovement :
        AnimationConstraint.AllowAll;

    public override void StartAbility(AbilityParams a)
    {
        SetAllAdditionalTransforms(a.Character.Positioning, null);
    }

    public override void UpdateAbility(AbilityParams a)
    {
        if (!IsUsing || a.Character.IsPlayingAnimation)
        {
            flipFlop = false;
            return;
        }

        var level = Level.CurrentLevel;
        if (level == null || routine)
            return;

        var phys = a.Scene.GetSystem<PhysicsSystem>();
        var l = CollisionLayers.BlockMovement | CollisionLayers.BlockPhysics;

        if (flipFlop && float.Abs(a.Character.WalkAcceleration.X) < 100)
        {
            flipFlop = false;
        }
        else if (!flipFlop && float.Abs(a.Character.WalkAcceleration.X) > 10)
        {
            flipFlop = true;
            var sign = float.Sign(a.Character.WalkAcceleration.X);
            var p = a.Character.Positioning;
            a.Character.DodgeMeter = a.Character.Stats.DodgeAbility;
            routine = true;

            Distance = float.Abs((a.Character.AimTargetPosition.X - p.GlobalCenter.X) * 0.9f);
            if (Distance < 500) // just go forwards
                Distance = 1500;
            RoutineScheduler.Start(Teleport());

            //var charBounds = a.Character.GetBoundingBox(Scene);
            //var overlapRect = new Rect(float.MaxValue, float.MaxValue, float.MinValue, float.MinValue);
            //overlapRect = overlapRect.StretchToContain(charBounds);
            //overlapRect = overlapRect.StretchToContain(charBounds.Translate(sign * Distance, 0));

            //// we stun/kill enemies in our way
            //foreach (var c in Scene.GetAllComponentsOfType<CharacterComponent>())
            //{
            //    if (c.Entity == a.Character.Entity)
            //        continue;

            //    if (overlapRect.IntersectsRectangle(c.GetBoundingBox(Scene)))
            //    {
            //        if (c.IsPlayingAnimationGroup("stun"))
            //            c.Kill();
            //        else
            //            CharacterUtilities.StunHeavy(Scene, c, c.Positioning.IsFlipped == sign > 0);
            //    }
            //}

            IEnumerator<IRoutineCommand> Teleport()
            {
                var bounds = a.Character.GetBoundingBox(Scene);

                Audio.PlayOnce(Sounds.TrickyTeleport, 0.9f, Utilities.RandomFloat(0.9f, 1.1f), AudioTracks.SoundEffects);
                var start = p.GlobalCenter;
                var end = p.GlobalCenter;
                var initialPos = start;
                end.X = float.Clamp(end.X + Distance * sign, level.FloorLine[0].X, level.FloorLine[^1].X);
                end.Y = Level.CurrentLevel!.GetFloorLevelAt(end.X) + CharacterConstants.GetFloorOffset(p.Scale);
                float duration = float.Clamp(Distance / 30000, 0.05f, 0.1f);
                float t = 0;
                while (true)
                {
                    yield return new GameSafeRoutineDelay();

                    t += Time.DeltaTime;
                    float progress = t / duration;
                    Blink(p, start, end, progress);

                    var pivot = Vector2.Zero;
                    if (sign > 0)
                        pivot = bounds.BottomRight + new Vector2(p.GlobalCenter.X - initialPos.X);
                    else
                        pivot = bounds.BottomLeft + new Vector2(p.GlobalCenter.X - initialPos.X);

                    var transform = Matrix3x2.CreateScale((1 - float.Abs(progress - 0.5f) * 2) * float.Abs(start.X - end.X) * 0.5f / bounds.Width + 1, 1, pivot);
                    SetAllAdditionalTransforms(p, transform);

                    if (t > duration)
                        break;
                }

                SetAllAdditionalTransforms(p, null);
                routine = false;
            }
        }
    }

    private static void Blink(CharacterPositioning p, Vector2 a, Vector2 b, float f)
    {
        p.GlobalCenter.X = float.Lerp(a.X, b.X, f);
        p.GlobalCenter.Y = float.Lerp(a.Y, b.Y, f);
        p.GlobalTarget = p.GlobalCenter;
        p.HopStartingPosition = p.GlobalCenter.X;
        p.NextHopPosition = p.GlobalCenter.X;
        p.HopAnimationTimer = 0;
    }

    private void SetAllAdditionalTransforms(CharacterPositioning p, Matrix3x2? transform)
    {
        if (Scene.TryGetComponentFrom<QuadShapeComponent>(p.Head.Entity, out var q))
            q.AdditionalTransform = transform;

        if (Scene.TryGetComponentFrom(p.Body.Entity, out q))
            q.AdditionalTransform = transform;

        foreach (var b in p.BodyDecorations)
            if (Scene.TryGetComponentFrom(b, out q))
                q.AdditionalTransform = transform;

        foreach (var b in p.HeadDecorations)
            if (Scene.TryGetComponentFrom(b, out q))
                q.AdditionalTransform = transform;

        //foreach (var b in p.Hands)
        //    if (Scene.TryGetComponentFrom(b.Entity, out q))
        //        q.AdditionalTransform = transform;

        foreach (var b in p.Feet)
            if (Scene.TryGetComponentFrom(b.Entity, out q))
                q.AdditionalTransform = transform;
    }

    public override void EndAbility(AbilityParams a)
    {
        SetAllAdditionalTransforms(a.Character.Positioning, null);
    }
}