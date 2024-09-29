using System.Numerics;
using Walgelijk;

namespace MIR;

public class CharacterPickupSystem : Walgelijk.System
{
    public override void Update()
    {
        foreach (var p in Scene.GetAllComponentsOfType<CharacterPickupComponent>())
        {
            var ch = Scene.GetComponentFrom<CharacterComponent>(p.Entity);

            if (!ch.Positioning.HandPoseFunctionOverride.Contains(PickupHandPose))
            {
                var f = false;

                if (p.Target.TryGet(Scene, out var targetTransform))
                {
                    f = targetTransform.Position.X > ch.Positioning.GlobalCenter.X != ch.Positioning.IsFlipped;
                    p.InitialTargetRotation = targetTransform.Rotation;
                }

                ch.Positioning.HandPoseFunctionOverride.Add(PickupHandPose);
                ch.PlayAnimation(Animations.Pickup.PickRandom(f), 1.2f);
            }

            p.Time += Time.DeltaTime;
            if (p.Time > p.Duration)
            {
                Scene.DetachComponent<CharacterPickupComponent>(p.Entity);
                ch.Positioning.HandPoseFunctionOverride.Remove(PickupHandPose);
            }
        }
    }

    private static void PickupHandPose(HandPoseParams p)
    {
        if (!p.Scene.TryGetComponentFrom<CharacterPickupComponent>(p.Character.Entity, out var pickup))
            return;

        if (!pickup.Target.TryGet(p.Scene, out var target) || 
            !p.Scene.TryGetComponentFrom<WeaponComponent>(target.Entity, out var wpn))
            return;

        float midPoint = pickup.PickupTime;

        var charPos = p.Character.Positioning;
        var secondHand = charPos.Hands.Second;
        var firstHand = charPos.Hands.First;

        {
            float t = (pickup.Time / pickup.Duration);
            var tEnter = Utilities.MapRange(0, midPoint, 0, 1, t);
            var tExit = Utilities.MapRange(midPoint, 1, 1, 0, t);
            bool isExit = t > midPoint;
            float animTime = isExit ? Easings.Cubic.In(tExit) : Easings.Cubic.InOut(tEnter);

            Vector2 targetPos;
            if (!isExit)
            {
                targetPos = Vector2.Transform(-wpn.HoldPoints[0], target.LocalToWorldMatrix) - charPos.GlobalCenter;
                pickup.LastHandPosePositions[0] = targetPos;
                firstHand.Look = HandLook.Open;
            }
            else
                targetPos = pickup.LastHandPosePositions[0];

            firstHand.PosePosition = Vector2.Lerp(firstHand.PosePosition, targetPos, animTime);
            firstHand.PoseRotation = Utilities.LerpAngle(pickup.InitialTargetRotation, firstHand.PoseRotation, 1 - animTime);
        }

        {
            float t = ((pickup.Time) / pickup.Duration);
            var tEnter = Utilities.MapRange(0, midPoint, 0, 1, t);
            var tExit = Utilities.MapRange(midPoint, 1, 1, 0, t);
            bool isExit = t > midPoint;
            float animTime = Easings.Circ.In(isExit ? Easings.Cubic.In(tExit) : Easings.Cubic.InOut(tEnter));

            // this hand will follow the other if the weapon is two-handed so we dont have to do much
            if (wpn.HoldPoints.Length > 1 && p.Equipped == null)
            {
                var targetPos = Vector2.Transform(wpn.HoldPoints[1], target.LocalToWorldMatrix) - charPos.GlobalCenter;
                secondHand.PosePosition.X = float.Lerp(secondHand.PosePosition.X, targetPos.X, animTime * animTime * animTime);
                secondHand.PosePosition.Y = float.Lerp(secondHand.PosePosition.Y, targetPos.Y, animTime);
            }
            else if (wpn.HoldPoints.Length < 2 || p.Equipped == null)
            {
                // TODO this is already done somewhere else so... its kind of ugly. but it works :)
                var direction = p.Character.AimDirection;
                float flipScaling = charPos.FlipScaling;
                var clampedDir = MadnessVector2.Normalize(new Vector2(
                    float.Sign(direction.X) * float.Max(float.Abs(direction.X), 0.5f),
                    Utilities.Clamp(direction.Y, -0.2f, 0.2f)
                ));
                var rad = float.Atan2(clampedDir.Y, clampedDir.X);
                var angle = rad * Utilities.RadToDeg;
                var rot = Matrix3x2.CreateRotation(rad);

                var pos1 = Vector2.TransformNormal(CharacterConstants.HandOffset1 with { Y = CharacterConstants.HandOffset1.Y * flipScaling }, rot) * charPos.Scale;
                var pos2 = Vector2.TransformNormal(CharacterConstants.HandOffset2 with { Y = CharacterConstants.HandOffset2.Y * flipScaling }, rot) * charPos.Scale;

                var pos = flipScaling < 0 ? pos1 : pos2;

                secondHand.PosePosition = Vector2.Lerp(secondHand.PosePosition, pos, Easings.Quad.InOut(tExit));
            }
        }
    }
}
