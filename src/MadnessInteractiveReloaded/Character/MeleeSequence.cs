namespace MIR;

/// <summary>
/// Sequence of animations and their hitboxes
/// </summary>
public class MeleeSequence
{
    /// <summary>
    /// Array of all keyframes
    /// </summary>
    public readonly MeleeSequenceKey[] Keys;

    public MeleeSequence(params MeleeSequenceKey[] keys)
    {
        Keys = keys;
    }
}