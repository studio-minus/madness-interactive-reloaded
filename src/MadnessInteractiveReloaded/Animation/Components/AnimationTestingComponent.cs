using System;
using Walgelijk;
using Walgelijk.AssetManager;
using static MIR.AnimationTestingSystem;

namespace MIR;

public class AnimationTestingComponent : Component
{
    public bool BlendAnimationFlag = false;
    public bool LoopAnimationFlag = false;
    public string Filter = string.Empty;
    public CharacterAnimation? LastAnimation = null;
    public AssetRef<CharacterAnimation>[] Animations = [];

    public bool ShowCurveDebugger;
    public Limb CurveDebuggerLimb;
    public Curve CurveDebuggerCurve;

    public enum Limb
    {
        Head,
        Body,
        Hand1,
        Hand2
    }

    public enum Curve
    {
        PositionX,
        PositionY,
        Rotation,
        ScaleX,
        ScaleY,
        Order
    }
}
