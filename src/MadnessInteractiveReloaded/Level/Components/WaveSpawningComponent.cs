using MIR.LevelEditor.Objects;
using System.Collections.Generic;
using System.Numerics;
using Walgelijk;

namespace MIR;

/// <summary>
/// Handles spawning enemies according to a <see cref="WaveSequence"/>
/// </summary>
[SingleInstance]
public class WaveSpawningComponent(WaveSequence sequence) : Component, IEnemySpawnProvider
{
    public WaveSequence Sequence = sequence;
    public int WaveIndex = -1;

    public bool IsFinished;
    public bool Enabled = true;
    public float SpawnTimer = 0;

    /// <summary>
    /// If the wave mode (<see cref="WaveSequence.Wave.Mode"/> is set to <see cref="WaveMode.Sequential"/>, this value keeps track of the instruction array index
    /// </summary>
    public int WaveInstrSeqIndex = 0;

    /// <summary>
    /// Amount of enemies to kill this wave
    /// </summary>
    public int ActiveWaveEnemyCount;    

    /// <summary>
    /// Amount of wave-spawned enemies killed during the active wave
    /// </summary>
    public int ActiveWaveBodyCount;

    public List<Vector2> SpawnPoints = [];

    public WaveSequence.Wave? ActiveWave => WaveIndex < 0 || WaveIndex >= Sequence.Waves.Length ? null : Sequence.Waves[WaveIndex];

    IList<ISpawnInstructions> IEnemySpawnProvider.SpawnInstructions => ActiveWave?.Instructions ?? [];
    IList<string> IEnemySpawnProvider.Weapons => ActiveWave?.Weapons ?? [];
    IList<Vector2> IEnemySpawnProvider.SpawnPoints => SpawnPoints;
    float IEnemySpawnProvider.Interval => ActiveWave?.SpawnInterval ?? 1;
    float IEnemySpawnProvider.WeaponChance => ActiveWave?.WeaponChance ?? 0.5f;
    int IEnemySpawnProvider.TotalEnemies => ActiveWave?.TargetCount ?? 0;
    bool IEnemySpawnProvider.Enabled => Enabled;
}

public interface IEnemySpawnProvider
{
    IList<ISpawnInstructions> SpawnInstructions { get; }
    IList<string> Weapons { get; }
    IList<Vector2> SpawnPoints { get; }
    float Interval { get; }
    float WeaponChance { get; }
    int TotalEnemies { get; }
    bool Enabled { get; }
}