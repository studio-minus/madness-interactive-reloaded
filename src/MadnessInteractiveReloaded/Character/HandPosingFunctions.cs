using System;
using System.Numerics;
using Walgelijk;
using MIR;

namespace MIR;

/// <summary>
/// Static class for posing hands.
/// </summary>
public static class HandPosingFunctions
{
    /// <summary>
    /// For holding pump shotguns.
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="equipped"></param>
    /// <param name="fallback"></param>
    /// <returns></returns>
    private static Vector2 GetPumpActionHandPos(Scene scene, WeaponComponent? equipped, Vector2 fallback)
    {
        if (equipped?.AnimatedParts?.Length > 0)
            return scene.GetComponentFrom<TransformComponent>(equipped.AnimatedParts[0]).Position - new Vector2(0, 15);
        return fallback;
    }

    /// <summary>
    /// For holding a sword in a pose to deflect bullets.
    /// </summary>
    private static void DeflectionPose(in HandPoseParams poseParams, ref Vector2 position, ref float angle)
    {
        //if (Utilities.RandomByte() is 50 or 30)
        //     poseParams.Character.Positioning.DeflectionImpactIntensity += Utilities.RandomFloat(-1, 1); ;

        var pos = poseParams.Character.Positioning;
        var flipScaling = pos.FlipScaling;
        var deflectIntensity = pos.SmoothedMeleeBlockImpactIntensity * 4;

        var deflectPosition = new Vector2(pos.IsFlipped ? -150 : -50, pos.IsFlipped ? 200 : 150) + poseParams.Character.AimDirection * 48 * pos.Scale;
        var deflectAngle = 230f;

        deflectPosition.X -= flipScaling * float.Abs(deflectIntensity) * 32;
        deflectAngle += Utilities.VectorToAngle(new Vector2(float.Abs(poseParams.Character.AimDirection.X), poseParams.Character.AimDirection.Y)) * -0.2f;

        deflectAngle -= flipScaling * deflectIntensity * 8;

        if (pos.IsFlipped)
        {
            //deflectAngle *= flipScaling;
            deflectAngle -= 190;
        }

        var deflectionProgressFactor = Easings.Cubic.InOut(pos.MeleeBlockProgress);
        position = Utilities.Lerp(position, deflectPosition, deflectionProgressFactor);
        angle = Utilities.LerpAngle(angle, deflectAngle, deflectionProgressFactor);

        if (deflectionProgressFactor > 0.5f)
            pos.Hands.Second.Look = HandLook.Open;
    }

    public static void UnarmedProtective(in HandPoseParams poseParams, ref Vector2 position, ref float angle, int index)
    {
        var pos = poseParams.Character.Positioning;
        var flipScaling = pos.FlipScaling;
        var deflectionProgressFactor = Easings.Cubic.InOut(pos.MeleeBlockProgress);
        var yOffset = poseParams.Character.AimDirection.Y * 40;

        var targetPos = new Vector2(flipScaling * (150 + 50 * index), 30 + 60 * index + yOffset);
        var targetTh = pos.Head.GlobalRotation + 60 + 25 * index;

        if (pos.IsFlipped)
        {
            targetTh *= -1;
            targetTh -= 180;
        }

        position = Utilities.Lerp(position, targetPos, deflectionProgressFactor);
        angle = Utilities.LerpAngle(angle, targetTh, deflectionProgressFactor);
    }

    /// <summary>
    /// Poses hands for punching without a weapon.
    /// </summary>
    /// <param name="poseParams"></param>
    public static void FistFight(in HandPoseParams poseParams)
    {
        var charPos = poseParams.Character.Positioning;
        var direction = poseParams.Character.AimDirection;
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

        charPos.Hands[0].PosePosition = flipScaling < 0 ? pos2 : pos1;
        charPos.Hands[0].PoseRotation = angle;
        charPos.Hands[0].Look = HandLook.Fist;
        for (int i = 1; i < charPos.Hands.Length; i++)
        {
            charPos.Hands[i].PosePosition = flipScaling < 0 ? pos1 : pos2;
            charPos.Hands[i].PoseRotation = angle;
            charPos.Hands[i].Look = HandLook.Fist;
        }

        UnarmedProtective(poseParams, ref charPos.Hands[0].PosePosition, ref charPos.Hands[0].PoseRotation, charPos.IsFlipped ? 1 : 0);
        UnarmedProtective(poseParams, ref charPos.Hands[1].PosePosition, ref charPos.Hands[1].PoseRotation, charPos.IsFlipped ? 0 : 1);
    }

    /// <summary>
    /// Generic two handed guns like rifles.
    /// </summary>
    /// <param name="poseParams"></param>
    public static void TwoHandedGun(in HandPoseParams poseParams)
    {
        var charPos = poseParams.Character.Positioning;
        charPos.SecondaryHandFollowsPrimary = true;

        var mainHand = charPos.Hands[0];
        mainHand.PosePosition = charPos.HandMousePosition;
        mainHand.PoseRotation = Utilities.VectorToAngle(poseParams.Character.AimDirection);
        mainHand.Look = HandLook.HoldPistol;
        mainHand.ShouldFollowRecoil = true;

        var secondHand = charPos.Hands[1];
        if (poseParams.Equipped != null && poseParams.EquippedTransform != null && poseParams.Equipped.HoldPoints.Length >= 2)
        {
            secondHand.ShouldFollowRecoil = false;

            var equipped = poseParams.Equipped;
            var hp = equipped.HoldPoints[1];
            hp -= poseParams.EquippedTransform.LocalPivot;

            if (equipped.Data.IsPumpAction)
            {
                var pumpPos = GetPumpActionHandPos(poseParams.Scene, equipped, hp);
                hp += pumpPos;
                hp.Y *= charPos.FlipScaling;
                secondHand.PosePosition = hp;
            }
            else
            {
                hp.Y *= charPos.FlipScaling;
                secondHand.PosePosition = hp;
            }

            secondHand.Look = equipped.HoldForGrip ? HandLook.HoldRifle : HandLook.HoldUnderside;

            if (equipped.HoldStockHandPose)
                mainHand.Look = HandLook.HoldStock;
        }
        else
        {
            secondHand.PosePosition = mainHand.PosePosition + new Vector2(-10, 10);
            secondHand.Look = HandLook.HoldPistol;
            secondHand.ShouldFollowRecoil = mainHand.ShouldFollowRecoil;
        }

        secondHand.PoseRotation = 0;
    }

    /// <summary>
    /// Things like pistols.
    /// </summary>
    /// <param name="poseParams"></param>
    public static void OneHandedGun(in HandPoseParams poseParams)
    {
        var charPos = poseParams.Character.Positioning;
        var mainHand = charPos.Hands[0];
        mainHand.PosePosition = charPos.HandMousePosition;
        mainHand.PoseRotation = Utilities.VectorToAngle(poseParams.Character.AimDirection);
        mainHand.Look = (poseParams.Equipped?.HoldStockHandPose ?? false) ? HandLook.HoldStock : HandLook.HoldPistol;
        mainHand.ShouldFollowRecoil = true;

        var ironSightCurveY = Easings.Circ.InOut(charPos.IronSightProgress);
        var ironSightCurveX = Easings.Circ.InOut(charPos.IronSightProgress + 0.1f);

        for (int i = 1; i < charPos.Hands.Length; i++)
        {
            var hand = charPos.Hands[i];
            hand.ShouldFollowRecoil = charPos.IronSightProgress >= 0.9f;
            hand.PosePosition = new Vector2((charPos.IsFlipped ? 50 : 100), -80);
            hand.PoseRotation = charPos.IsFlipped ? 180 : 0;

            hand.PosePosition.X = Utilities.Lerp(hand.PosePosition.X, mainHand.PosePosition.X, ironSightCurveX);
            hand.PosePosition.Y = Utilities.Lerp(hand.PosePosition.Y, mainHand.PosePosition.Y, ironSightCurveY);
            hand.PoseRotation = Utilities.LerpAngle(hand.PoseRotation, mainHand.PoseRotation, ironSightCurveX);

            // set secondary hand to open position if it is in the middle of moving to/from the weapon 
            if (charPos.IronSightProgress > 0.1f && charPos.IronSightProgress < 0.9f)
                hand.Look = HandLook.Open;
            else if (poseParams.Equipped != null)
            {
                hand.Look = 
                    ((charPos.IronSightProgress > 0.9f 
                    && poseParams.Equipped.HoldStockHandPose)
                    || poseParams.Equipped.HoldPoints.Length > 1)
                    
                    ? HandLook.HoldStock : HandLook.Fist;
            }
            else
                hand.Look = HandLook.Fist;
        }
    }

    /// <summary>
    /// Smaller swords that only require one hand.
    /// </summary>
    /// <param name="poseParams"></param>
    public static void OneHandedSword(in HandPoseParams poseParams)
    {
        var charPos = poseParams.Character.Positioning;
        float flipScaling = charPos.IsFlipped ? -1 : 1;

        float angle = (charPos.IsFlipped ? 180 : 0);
        var pos1 = new Vector2(CharacterConstants.HandOffset1.X * flipScaling, CharacterConstants.HandOffset1.Y) + charPos.Head.Direction * -24 * charPos.Scale;
        var pos2 = new Vector2(CharacterConstants.HandOffset2.X * flipScaling, CharacterConstants.HandOffset2.Y) + charPos.Head.Direction * 24 * charPos.Scale;

        charPos.CurrentRecoil = 0;

        var mainHand = charPos.Hands[0];
        mainHand.PosePosition = flipScaling < 0 ? pos2 : pos1;
        var t = angle - 17.76f * flipScaling;
        mainHand.PoseRotation = t;
        mainHand.Look = HandLook.HoldPistol;

        for (int i = 1; i < charPos.Hands.Length; i++)
        {
            var h = charPos.Hands[i];
            h.PosePosition = (flipScaling < 0 ? pos1 : pos2);
            h.PosePosition.X -= flipScaling * 50;
            h.PosePosition.Y -= 100;
            h.PoseRotation = angle;
            h.Look = HandLook.Fist;
        }

        poseParams.Character.DeflectionLine.A = mainHand.GlobalPosition;
        poseParams.Character.DeflectionLine.B = mainHand.GlobalPosition + Utilities.AngleToVector(mainHand.PoseRotation + 90) * 500 * flipScaling;
        DeflectionPose(poseParams, ref mainHand.PosePosition, ref mainHand.PoseRotation);
        //a DeflectionPose(poseParams, ref charPos.Hands[1].PosePosition, ref charPos.Hands[1].PoseRotation);
    }

    /// <summary>
    /// Large swords big enough for two hands.
    /// </summary>
    /// <param name="poseParams"></param>
    public static void TwoHandedSword(in HandPoseParams poseParams)
    {
        var charPos = poseParams.Character.Positioning;
        float flipScaling = charPos.IsFlipped ? -1 : 1;

        for (int i = 0; i < charPos.Hands.Length; i++)
        {
            var hand = charPos.Hands[i];

            float angle = (charPos.IsFlipped ? 180 : 0);
            var pos1 = new Vector2(CharacterConstants.HandOffset1.X * flipScaling, CharacterConstants.HandOffset1.Y) + charPos.Head.Direction * -24 * charPos.Scale;
            var pos2 = new Vector2(CharacterConstants.HandOffset2.X * flipScaling, CharacterConstants.HandOffset2.Y) + charPos.Head.Direction * 24 * charPos.Scale;

            charPos.CurrentRecoil = 0;

            var t = angle - 17.76f * flipScaling;
            hand.PosePosition = (flipScaling < 0 ? pos2 : pos1);
            hand.PoseRotation = t;
            hand.Look = HandLook.HoldPistol;
        }

        poseParams.Character.DeflectionLine.A = charPos.Hands.First.GlobalPosition;
        poseParams.Character.DeflectionLine.B = charPos.Hands.First.GlobalPosition + Utilities.AngleToVector(charPos.Hands.First.PoseRotation + 90) * 500 * flipScaling;
        DeflectionPose(poseParams, ref charPos.Hands[0].PosePosition, ref charPos.Hands[0].PoseRotation);
        DeflectionPose(poseParams, ref charPos.Hands[1].PosePosition, ref charPos.Hands[1].PoseRotation);
    }

    /// <summary>
    /// Just uses <see cref="OneHandedSword(in HandPoseParams)"/>.
    /// </summary>
    /// <param name="poseParams"></param>
    public static void OneHandedAxe(in HandPoseParams poseParams)
        => OneHandedSword(poseParams);

    /// <summary>
    /// Big axes.
    /// </summary>
    /// <param name="poseParams"></param>
    public static void TwoHandedAxe(in HandPoseParams poseParams)
    {
        var charPos = poseParams.Character.Positioning;
        charPos.SecondaryHandFollowsPrimary = true;

        var mainHand = charPos.Hands.First;
        var offHand = charPos.Hands.Second;

        float angle = (charPos.IsFlipped ? 180 : 0);

        var pos1 = new Vector2(-CharacterConstants.HandOffset1.X * 2f, CharacterConstants.HandOffset1.Y) + charPos.Head.Direction * -24 * charPos.Scale;
        var pos2 = new Vector2(CharacterConstants.HandOffset1.X * 2f, CharacterConstants.HandOffset1.Y) + charPos.Head.Direction * 24 * charPos.Scale;

        charPos.CurrentRecoil = 0;

        var t = angle - 17.76f * charPos.FlipScaling;
        mainHand.PosePosition = (charPos.IsFlipped ? pos2 : pos1);
        mainHand.PoseRotation = t;
        mainHand.Look = HandLook.HoldPistol;

        if (poseParams.Equipped != null)
        {
            offHand.PosePosition = poseParams.Equipped.HoldPoints[1 % poseParams.Equipped.HoldPoints.Length]; // mod just in case we are out of bounds
            offHand.PosePosition -= poseParams.EquippedTransform!.LocalPivot;
            offHand.PosePosition.Y *= charPos.FlipScaling;
        }
        else
            offHand.PosePosition = pos2;

        offHand.PoseRotation = 0;
        offHand.Look = HandLook.HoldPistol;

        poseParams.Character.DeflectionLine.A = charPos.Hands.First.GlobalPosition;
        poseParams.Character.DeflectionLine.B = charPos.Hands.First.GlobalPosition + Utilities.AngleToVector(charPos.Hands.First.PoseRotation + 90) * 500 * charPos.FlipScaling;
        DeflectionPose(poseParams, ref charPos.Hands[0].PosePosition, ref charPos.Hands[0].PoseRotation);
        //DeflectionPose(poseParams, ref charPos.Hands[1].PosePosition, ref charPos.Hands[1].PoseRotation);
    }

    /// <summary>
    /// Uses <see cref="OneHandedAxe(in HandPoseParams)"/>.
    /// </summary>
    /// <param name="poseParams"></param>
    public static void OneHandedBlunt(in HandPoseParams poseParams) => OneHandedAxe(poseParams);

    /// <summary>
    /// Uses <see cref="TwoHandedAxe(in HandPoseParams)"/>.
    /// </summary>
    /// <param name="poseParams"></param>
    public static void TwoHandedBlunt(in HandPoseParams poseParams) => TwoHandedAxe(poseParams);
}