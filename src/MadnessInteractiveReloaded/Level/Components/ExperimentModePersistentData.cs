using static MIR.ExperimentModeComponent;

namespace MIR;

public static class ExperimentModePersistentData
{
    public static FactionOption SelectedFaction = new("aahw");
    public static string CurrentFilter = string.Empty;
    public static bool DisableAI = false;
    public static bool AutoSpawn = false;
    public static WaveSequence.Wave AutoSpawnWave = new()
    {
        TargetCount = int.MaxValue
    };
}
// 🎈