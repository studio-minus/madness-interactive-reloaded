namespace MIR;
using Walgelijk;

/// <summary>
/// The train engine on the train level.
/// </summary>
[RequiresComponents(typeof(IsShotTriggerComponent))]
public class TrainEngineComponent : Component
{
    public HitEvent EventData;
    public bool HasExploded = false;
    public int Health = 4;
}
