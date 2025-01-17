using Walgelijk;

namespace MIR;

/// <summary>
/// For level creation purposes, this ability makes the character unable to pick up weapons. If holding a weapon, this character will either drop it or throw it.
/// </summary>
public class NoWeapons : CharacterAbilityComponent
{
    public override string DisplayName => "No weapons allowed"; // 🎗

    public NoWeapons() : base(AbilitySlot.None, AbilityBehaviour.Toggle)
    {
    }

    public override void Initialise(AbilityParams a)
    {
        if (a.Ai.TryGet(a.Scene, out var ai))
            ai.AllowPickup = false;
    }

    public override void StartAbility(AbilityParams a)
    {
    }

    public override void UpdateAbility(AbilityParams a)
    {
        if (a.Character.HasWeaponEquipped)
        {
            if (Utilities.RandomFloat() > 0.5f)
                CharacterUtilities.TryThrowWeapon(Scene, a.Character);
            else if (!a.Character.AnimationConstrainsAny(AnimationConstraint.PreventThrowing))
                a.Character.DropWeapon(a.Scene);
        }
    }

    public override void EndAbility(AbilityParams a)
    {
    }
}
