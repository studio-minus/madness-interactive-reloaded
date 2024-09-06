using System.Numerics;
using Walgelijk;

namespace MIR;

/// <summary>
/// Used for when enemies spawn out of a door. 
/// </summary>
public class ExitDoorComponent : Component
{
    /// <summary>
    /// How long is the spawning in animation?
    /// </summary>
    public float DurationSeconds;
    public Vector2 Start, End;

    public float CurrentTime;
    public bool IsVertical;
    public Vector2 ForcedWalkAcceleration;

    public ExitDoorComponent(Vector2 start, Vector2 end, float durationSeconds = 1)
    {
        Start = start;
        End = end;
        DurationSeconds = durationSeconds;
    }
}