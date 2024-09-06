using Walgelijk;

namespace MIR;

public class ElevatorLevelComponent : Component
{
    public float VerticalOffset;
    public float Time;
    public float StartDelay = 3;
    public float MaxSpeed = 10;
    public float DtMultiplier;
}
