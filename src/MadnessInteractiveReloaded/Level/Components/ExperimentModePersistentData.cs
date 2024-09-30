using static MIR.ExperimentModeComponent;

namespace MIR;

public static class ExperimentModePersistentData
{
    public static FactionOption SelectedFaction = new("aahw");
    public static string CurrentFilter = string.Empty;
    public static bool AIDisabled = false;
    public static bool AutoSpawn = false;
    public static EnemySpawningComponent? SpawningComponent;
}
// 🎈