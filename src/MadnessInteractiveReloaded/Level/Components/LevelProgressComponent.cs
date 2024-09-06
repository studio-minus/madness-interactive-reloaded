using Walgelijk;

namespace MIR;

public class LevelProgressComponent : Component
{
    /// <summary>
    /// Only applicable if the level has ProgressionType.BodyCount
    /// </summary>
    public int CurrentBodyCount;

    /// <summary>
    /// Only applicable if the level has ProgressionType.BodyCount
    /// </summary>
    public int BodyCountToWin;

    /// <summary>
    /// Get level win state. Use <see cref="LevelProgressSystem.Win"/> to force win
    /// </summary>
    public bool CanProgress;

    public float TimeTracker;
}