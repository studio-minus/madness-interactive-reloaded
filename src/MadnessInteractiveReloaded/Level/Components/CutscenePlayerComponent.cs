using MIR.Cutscenes;
using System.Numerics;
using Walgelijk;

namespace MIR;

/// <summary>
/// Plays a cutscene if the player is far enough away from the
/// entity this component is on.
/// </summary>
public class CutscenePlayerComponent : Component
{
    /// <summary>
    /// The <see cref="Cutscene"/> data to play.
    /// </summary>
    public Cutscene Cutscene;

    /// <summary>
    /// Compared to the player distance for determining if the cutscene should play.
    /// </summary>
    public Vector2 Position;

    /// <summary>
    /// If the game should advance to the next level when the cutscene is over.
    /// </summary>
    public bool ProgressLevelOnEnd;

    public CutscenePlayerComponent(Cutscene cutscene, Vector2 position)
    {
        Cutscene = cutscene;
        Position = position;
    }
}
