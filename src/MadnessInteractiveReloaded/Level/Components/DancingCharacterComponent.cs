using Walgelijk;

namespace MIR;

/// <summary>
/// Make a character dance.
/// They're docile.
/// </summary>
public class DancingCharacterComponent : Component
{
    /// <summary>
    /// The dance animation.
    /// </summary>
    public readonly CharacterAnimation Dance;

    /// <summary>
    /// Are we still dancing?
    /// </summary>
    public bool Enabled = true;

    /// <summary>
    /// How long have we been dancing?
    /// </summary>
    public float Life = 0;

    /// <summary>
    /// How long after this component has been added do we make the
    /// ai docile and make them play the dance animation.
    /// </summary>
    public float LifeThreshold = Utilities.RandomFloat(1, 4);

    public DancingCharacterComponent(CharacterAnimation dance)
    {
        Dance = dance;
    }
}
