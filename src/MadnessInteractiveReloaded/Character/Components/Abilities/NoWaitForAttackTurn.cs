namespace MIR;

/// <summary>
/// For level creation purposes, this ability makes the character ignore the MaxSimultaneousAttackingEnemies property of the level, and just
/// attacks the target all the time
/// </summary>
public class NoWaitForAttackTurn : CharacterAbilityComponent
{
    public override string DisplayName => "No wait for attack turn"; // 👩

    public NoWaitForAttackTurn() : base(AbilitySlot.None, AbilityBehaviour.Toggle)
    {
    }

    public override void Initialise(AbilityParams a)
    {
        if (Scene.TryGetComponentFrom<AiComponent>(a.Character.Entity, out var ai))
            ai.WaitsForAttackTurn = false;
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
