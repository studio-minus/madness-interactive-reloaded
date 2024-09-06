namespace MIR;

/// <summary>
/// For level creation purposes, this ability makes the character unstunnable
/// </summary>
public class NoStunAbilityComponent : CharacterAbilityComponent
{
    public override string DisplayName => "No stun"; // 👩

    public NoStunAbilityComponent() : base(AbilitySlot.None, AbilityBehaviour.Toggle)
    {
    }

    public override void Initialise(AbilityParams a)
    {
        a.Character.Flags &= ~CharacterFlags.StunAnimationOnNonFatalShot;
    }

    public override void StartAbility(AbilityParams a)
    {
    }

    public override void UpdateAbility(AbilityParams a)
    {

    }

    public override void EndAbility(AbilityParams a)
    {
    }
}
