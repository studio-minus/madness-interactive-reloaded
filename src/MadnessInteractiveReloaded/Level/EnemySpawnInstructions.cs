using Newtonsoft.Json;
using System;

namespace MIR;

/// <summary>
/// Implement to create a preset for spawning an NPC.
/// </summary>
public interface ISpawnInstructions : ICloneable
{
    [JsonIgnore]
    public CharacterStats Stats { get; }
    [JsonIgnore]
    public CharacterLook Look { get; }  
    [JsonIgnore]
    public Faction Faction { get; }
}

public struct InstancedSpawnInstructions : ISpawnInstructions
{
    public CharacterStats Stats { get; }
    public CharacterLook Look { get; }
    public Faction Faction { get; }

    public InstancedSpawnInstructions(CharacterStats stats, CharacterLook look, Faction faction)
    {
        Stats = stats;
        Look = look;
        Faction = faction;
    }

    public object Clone()
    {
        return new InstancedSpawnInstructions(Stats, Look, Faction);
    }
}

/// <summary>
/// What the <see cref="EnemySpawningSystem"/> will use for spawning enemies.
/// Read from: <see cref="EnemySpawningComponent.SpawnInstructions"/>
/// </summary>
public class EnemySpawnInstructions: ISpawnInstructions
{
    public string StatsKey = "grunt";
    public string LookKey = "grunt";
    public string FactionKey = "aahw";

    public EnemySpawnInstructions(string stats, string look, string factionKey)
    {
        StatsKey = stats;
        LookKey = look;
        FactionKey = factionKey;
    }

    public EnemySpawnInstructions()
    {
        
    }

    public EnemySpawnInstructions Clone() => new(StatsKey, LookKey, FactionKey);

    object ICloneable.Clone() => Clone();

    [JsonIgnore]
    public CharacterLook Look => Registries.Looks.Get(LookKey);

    [JsonIgnore]
    public CharacterStats Stats => Registries.Stats.Get(StatsKey);

    [JsonIgnore]
    public Faction Faction => Registries.Factions.Get(FactionKey ?? "aahw");
}
