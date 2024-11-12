using Walgelijk;

namespace MIR;

/// <summary>
/// A <see cref="Component"/> for running a <see cref="MeleeSequence"/>.
/// </summary>
public class  MeleeSequenceComponent : Component
{
    /// <summary>
    /// The sequence to run.
    /// </summary>
    public readonly MeleeSequence Sequence;
    public int CurrentIndex;
    public bool IsComplete;
    public bool CanContinue = false;
    public float Speed = 1;
    public int HitframesSpent = 0;
    public float AnimationTimer = 0;

    public ActiveCharacterAnimation? LastAnim;

    public MeleeSequenceComponent(MeleeSequence sequence, float speed = 1)
    {
        Sequence = sequence;
        Speed = speed;
    }
}
