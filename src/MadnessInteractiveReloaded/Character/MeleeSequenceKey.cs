namespace MIR;

/// <summary>
/// Melee sequence keyframe. It contains the a <see cref="CharacterAnimation"/>, the transition frame, and an array of hitframes
/// /// </summary>
public class MeleeSequenceKey
{
    public DoubleSided<CharacterAnimation> Animation;
    public int TransitionFrame;
    public int[] HitFrames;

    public MeleeSequenceKey(CharacterAnimation animation, int transitionFrame, int[] hitFrames)
    {
        Animation = new DoubleSided<CharacterAnimation>(animation, animation);
        TransitionFrame = transitionFrame;
        HitFrames = hitFrames;
    }

    public MeleeSequenceKey(DoubleSided<CharacterAnimation> animation, int transitionFrame, int[] hitFrames)
    {
        Animation = animation;
        TransitionFrame = transitionFrame;
        HitFrames = hitFrames;
    }
}