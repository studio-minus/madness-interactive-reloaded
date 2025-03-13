using System;
using Walgelijk;

namespace MIR;

/// <summary>
/// Put this on a character if they are allied to the player and have to persist across levels (if they survive)
/// </summary>
[RequiresComponents(typeof(CharacterComponent), typeof(AiComponent))]
public class PersistentBuddyComponent : Component
{

}

public struct SerialisedBuddy : ISpawnInstructions
{
    public Tag Tag;
    public string Name;
    public string Stats, Look, Faction;
    public PersistentEquippedWeapon? Weapon;

    readonly CharacterStats ISpawnInstructions.Stats => Registries.Stats[Stats];
    readonly CharacterLook ISpawnInstructions.Look => Registries.Looks[Look];
    readonly Faction ISpawnInstructions.Faction => Registries.Factions[Faction];
    readonly PersistentEquippedWeapon? ISpawnInstructions.Weapon => Weapon;

    readonly object ICloneable.Clone() => MemberwiseClone();
}