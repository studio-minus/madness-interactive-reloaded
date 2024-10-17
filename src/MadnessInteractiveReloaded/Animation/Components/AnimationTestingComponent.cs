using System;
using Walgelijk;
using Walgelijk.AssetManager;

namespace MIR;

public class AnimationTestingComponent : Component
{
    public bool BlendAnimationFlag = false;
    public bool LoopAnimationFlag = false;
    public string Filter = string.Empty;
    public CharacterAnimation? LastAnimation = null;
    public AssetRef<CharacterAnimation>[] Animations = [];
    public bool ShowCurveDebugger;
}
