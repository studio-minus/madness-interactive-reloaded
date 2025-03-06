using System.Collections.Generic;
using System.Numerics;
using Walgelijk;

namespace MIR;

/// <summary>
/// Handles spawning enemies according to a <see cref="WaveSequence"/>
/// </summary>
[SingleInstance]
public class WaveSpawningComponent(WaveSequence sequence) : Component
{
    public WaveSequence Sequence = sequence;
    public int WaveIndex = -1;
    public bool IsFinished;
    public bool Enabled = true;
    public float SpawnTimer = 0;
    public int RemainingEnemiesThisWave;

    public List<Vector2>? SpawnPoints;
    public List<LevelEditor.Objects.Door>? Doors;
}
