namespace MIR;

public enum ProgressionType
{
    /// <summary>
    /// The game will always be won, but only transition to the next level if explicitly told to do so
    /// </summary>
    Always,

    /// <summary>
    /// The game will progress if the BodyCountToWin is reached
    /// </summary>
    BodyCount,

    /// <summary>
    /// Progress once the time limit is reached
    /// </summary>
    Time,

    /// <summary>
    /// Progress only when a script says so
    /// </summary>
    Explicit,
}
