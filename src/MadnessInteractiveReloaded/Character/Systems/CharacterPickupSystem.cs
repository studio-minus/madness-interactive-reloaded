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
                    f = targetTransform.Position.X > ch.Positioning.GlobalCenter.X != ch.Positioning.IsFlipped;

                ch.Positioning.HandPoseFunctionOverride.Add(PickupHandPose);
                ch.PlayAnimation(Animations.Pickup.PickRandom(f), 1.7f);
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

        var target = pickup.Target.Get(p.Scene);

        if (!p.Scene.TryGetComponentFrom<WeaponComponent>(target.Entity, out var wpn))
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
        }

        if (wpn.HoldPoints.Length > 1 && p.Equipped == null)
        {
            float t = ((pickup.Time) / pickup.Duration);
            var tEnter = Utilities.MapRange(0, midPoint, 0, 1, t);
            var tExit = Utilities.MapRange(midPoint, 1, 1, 0, t);
            bool isExit = t > midPoint;
            float animTime = Easings.Circ.In(isExit ? Easings.Cubic.In(tExit) : Easings.Cubic.InOut(tEnter));

            // this hand will follow the other if the weapon is two-handed so we dont have to do much
            var targetPos = Vector2.Transform(wpn.HoldPoints[1], target.LocalToWorldMatrix) - charPos.GlobalCenter;
            secondHand.PosePosition.X = float.Lerp(secondHand.PosePosition.X, targetPos.X, animTime * animTime * animTime);
            secondHand.PosePosition.Y = float.Lerp(secondHand.PosePosition.Y, targetPos.Y, animTime );
        }
    }
}
