using Walgelijk;

namespace MIR;

/// <summary>
/// Accurate shots are telegraphed shots that typically do a lot more damage.
/// </summary>
public class AccurateShotComponent : Component
{
    /// <summary>
    /// How long has this been running? (in seconds)
    /// </summary>
    public float Time;

    /// <summary>
    /// How long the telegraph is.
    /// </summary>
    public float Lifespan = 2;

    /// <summary>
    /// The character performing the shot.
    /// </summary>
    public ComponentRef<CharacterComponent> OriginCharacter;

    /// <summary>
    /// The character being shot.
    /// </summary>
    public ComponentRef<CharacterComponent> TargetCharacter;

    /// <summary>
    /// 
    /// </summary>
    public bool Finished;

    public AccurateShotComponent(ComponentRef<CharacterComponent> originCharacter, ComponentRef<CharacterComponent> targetCharacter, float lifespan = CharacterConstants.AccurateShotWarningDuration)
    {
        Lifespan = lifespan;
        OriginCharacter = originCharacter;
        TargetCharacter = targetCharacter;
    }
}
