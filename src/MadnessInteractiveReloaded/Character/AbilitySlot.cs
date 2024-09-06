using System;

namespace MIR;

public enum AbilitySlot
{
    None,

    Ability1,
    Ability2,
    Ability3,
    Ability4,
}

public static class AbilitySlotExtensions
{
    public static GameAction AsAction(this AbilitySlot a)
    {
        return a switch
        {
            AbilitySlot.Ability1 => GameAction.Ability1,
            AbilitySlot.Ability2 => GameAction.Ability2,
            AbilitySlot.Ability3 => GameAction.Ability3,
            AbilitySlot.Ability4 => GameAction.Ability4,
            _ => GameAction.None,
        };
    }
}