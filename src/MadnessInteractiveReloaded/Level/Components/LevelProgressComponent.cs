using Walgelijk;

namespace MIR;

public class LevelProgressComponent : Component
{
    public struct BodyCountGoal
    {
        public int Current;
        public int Target;
    }

    public struct TimedGoal
    {
        public float Time;
    }

    /// <summary>
    /// Only applicable if the level has <see cref="ProgressionType.BodyCount"/>
    /// </summary>
    public BodyCountGoal BodyCount;

    /// <summary>
    /// Only applicable if the level has <see cref="ProgressionType.Time"/>
    /// </summary>
    public TimedGoal Timed;

    /// <summary>
    /// True is the level goal is reached. It is now possible to progress to the next level.
    /// </summary>
    public bool GoalReached;
}

