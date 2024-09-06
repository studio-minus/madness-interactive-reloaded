using System.Numerics;

namespace MIR;

public class JumpDodgeComponent : Walgelijk.Component
{
    public readonly CharacterAnimation Animation;

    public JumpDodgeComponent(CharacterAnimation animation)
    {
        Animation = animation;
    }

    public Vector2 InitialAcceleration;
    public float Time;
    public float Duration;
    public bool ShouldDash;
}
