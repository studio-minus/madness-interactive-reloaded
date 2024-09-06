using System;
using System.Numerics;
using Walgelijk;

namespace MIR;

/// <summary>
/// A class for handling everything about positioning characters.
/// Their position, if they're flipped (left/right), their walk speed,
/// where their head and body is, etc.
/// </summary>
public class CharacterPositioning
{
    // general
    public Vector2 GlobalCenter;
    /// <summary>
    /// The vertical offset to pretend like we're flying
    /// </summary>


    /// <summary>
    /// The scale of this character. This is determined when it is created and cannot be changed afterwards. Used for mag agents and such.
    /// </summary>
    public readonly float Scale;

    /// <summary>
    /// This position is <b>relative to the floor</b>, meaning Y = 0 is at floor level. This is different from <see cref="GlobalCenter"/>, which is an absolute position.
    /// </summary>
    public Vector2 GlobalTarget;
    public bool IsFlipped = false;
    public float CurrentRecoil;
    public float SmoothRecoil;
    public float RandomHeightOffset = Utilities.RandomFloat(-25, 5);
    public float NoiseOffset = Utilities.RandomFloat(-1000f, 1000f);
    public int FlipScaling => IsFlipped ? -1 : 1;

    // walking
    public float TopWalkSpeed = 500;
    public float HopStartingPosition = -1;
    public float NextHopPosition = 0;
    public float HopAnimationTimer = -1;
    public float HopAnimationDuration = 0.33f;
    public float HopTargetHeight = 10;
    public float CurrentHoppingHeight = 0;
    public float HopAcceleration = 1;
    public bool IsBusyHopping => HopAnimationTimer >= 0;
    public float TiltIntensity = 0;

    // flying
    public float FlyingOffset = 0;
    public bool IsFlying => FlyingOffset > float.Epsilon;
    public float FlyingVelocity = 0;
    public float FlyingAnimationOffset = 0;

    // head 
    public readonly HeadAnimatedLimb Head;
    public readonly Entity[] HeadDecorations = new Entity[3];
    public float LookOffsetSpeed = 15; // smoothapproach speeds
    public float LookDirectionSpeed = 20;

    // body
    public readonly BodyAnimatedLimb Body;
    public Entity[] BodyDecorations = new Entity[2];

    // hands
    public readonly TwoOfThem<HandAnimatedLimb> Hands;
    public HandPositionMode HandPositionMode = HandPositionMode.None;
    public float IronSightProgress;
    public float MeleeBlockProgress;
    public Vector2 HandMousePosition;
    public Vector2 RecoilPositionOffset;
    public float RecoilAngleOffset;
    public bool SecondaryHandFollowsPrimary = false; // (duston): if the second hand follows the primary hand. (usually including an offset).
    public Action<HandPoseParams>? HandPoseFunctionOverride;

    public float MeleeBlockImpactIntensity = 0; // used by HandPosingFunctions
    public float SmoothedMeleeBlockImpactIntensity = 0; // used by HandPosingFunctions

    // feet
    public readonly TwoOfThem<FootLimb> Feet;
    public bool ShouldFeetFollowBody = true;

    public CharacterPositioning(float scale, Vector2 globalCenterTarget, HeadAnimatedLimb head, BodyAnimatedLimb body, TwoOfThem<HandAnimatedLimb> hands, TwoOfThem<FootLimb> feet)
    {
        GlobalCenter = GlobalTarget = globalCenterTarget;
        Scale = scale;
        Head = head;
        Body = body;
        Hands = hands;
        Feet = feet;

        // calculating the InitialOffset based on how this instance was created
        Body.InitialOffset = Body.GlobalPosition - GlobalCenter;

        Feet.First.InitialOffset = Feet.First.GlobalPosition - GlobalCenter;
        Feet.Second.InitialOffset = Feet.Second.GlobalPosition - GlobalCenter;

        Hands.First.InitialOffset = Hands.First.GlobalPosition - GlobalCenter;
        Hands.Second.InitialOffset = Hands.Second.GlobalPosition - GlobalCenter;
    }
}