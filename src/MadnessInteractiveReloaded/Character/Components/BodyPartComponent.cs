namespace MIR;

using System;
using Walgelijk;

/// <summary>
/// For doing damage to specific body parts.
/// </summary>
public class BodyPartComponent : Component
{
    /// <summary>
    /// The character entity the body part belongs to.
    /// </summary>
    public ComponentRef<CharacterComponent> Character;
    
    /// <summary>
    /// The maximum health this body part can have.
    /// </summary>
    public float MaxHealth = 5;

    /// <summary>
    /// The current health for this body part.
    /// </summary>
    public float Health = 5;
    
    /// <summary>
    /// Cause damage to this body part.
    /// </summary>
    /// <param name="damage"></param>
    public void Damage(float damage)
    {
        Health = float.Min(Health - damage, MaxHealth);
    }

    /// <summary>
    /// Undo damage on this body part.
    /// Clamped to <see cref="MaxHealth"/>
    /// </summary>
    /// <param name="hp"></param>
    public void Heal(float hp)
    {
        Health = float.Min(Health + hp, MaxHealth);
    }
}
