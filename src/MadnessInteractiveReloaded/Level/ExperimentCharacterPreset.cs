using Walgelijk;

namespace MIR;

/// <summary>
/// For spawning enemies in Experiment Mode.
/// </summary>
public class ExperimentCharacterPreset : ISpawnInstructions
{
    public string Name;
    public CharacterLook Look;
    public CharacterStats Stats;

    public bool Mutable;

    CharacterStats ISpawnInstructions.Stats => Stats;
    CharacterLook ISpawnInstructions.Look => Look;
    Faction ISpawnInstructions.Faction => Registries.Factions["aahw"];

    public ExperimentCharacterPreset(bool mutable, string name, CharacterLook look, CharacterStats stats)
    {
        Name = name;
        Look = look;
        Stats = stats;
        Mutable = mutable;
    }

    public ExperimentCharacterPreset()
    {
        Name = "Untitled";
        Look = null;
        Stats = null;
        Mutable = false;
    }

    public object Clone()
    {
        throw new System.NotImplementedException();
    }
}
