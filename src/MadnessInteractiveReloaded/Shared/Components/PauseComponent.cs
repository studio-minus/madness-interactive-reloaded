using System.Numerics;
using Walgelijk;

namespace MIR;

/// <summary>
/// Used for pausing the game.<br></br>
/// See: <see cref="PauseSystem"/>.
/// </summary>
public class PauseComponent : Component
{
    /// <summary>
    /// If the game is paused or not.
    /// </summary>
    public bool Paused;

    /// <summary>
    /// How long (in seconds) since the game was either paused or unpaused.
    /// </summary>
    public float TimeSinceChange;

    /// <summary>
    /// The "openness" of the pause menu (0 to 1) 
    /// </summary>
    public float AnimationProgress;

    /// <summary>
    /// Is the settings menu opened?
    /// </summary>
    public bool SettingsMenuOpen = false;

    public SettingTab SettingTab;
    public Vector2 PauseMenuSize;
    public float PauseMenuSizeAnimationTime;
}
