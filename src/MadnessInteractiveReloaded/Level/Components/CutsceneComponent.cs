using MIR.Cutscenes;
using Walgelijk;

namespace MIR;

/// <summary>
/// Stores data for the <see cref="CutsceneSystem"/>.
/// </summary>
[SingleInstance]
public class CutsceneComponent : Component
{
    /// <summary>
    /// The cutsene data.
    /// <see cref="Cutscene"/>
    /// </summary>
    public Cutscene Cutscene;

    /// <summary>
    /// Is the cutscene done?
    /// </summary>
    public bool IsComplete;

    /// <summary>
    /// The slide index.
    /// </summary>
    public int Index;

    /// <summary>
    /// How long has it been running in seconds (unscaled).
    /// </summary>
    public float Time;

    /// <summary>
    /// If the entity with this component gets deleted when the cutscene is over.
    /// </summary>
    public bool DestroyEntityOnCompletion = true;

    public RenderOrder RenderOrder = RenderOrders.UserInterface;

    public bool CurrentSlideIsUninitialised = true;

    public float SkipTimer;

    public CutsceneComponent(Cutscene cutscene)
    {
        Cutscene = cutscene;
    }
}
