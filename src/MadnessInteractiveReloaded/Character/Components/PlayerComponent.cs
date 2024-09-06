using Walgelijk;

namespace MIR;

/// <summary>
/// The <see cref="Component"/> for the player.
/// Used globally as a "singleton component". As in, don't try to have multiple of these in a Scene at once.
/// </summary>
public class PlayerComponent : Component
{
    /// <summary>
    /// If user input is accepted and processed.
    /// Used for things like cutscenes or displaying the character in the main menu.
    /// </summary>
    public bool RespondToUserInput = true;

    /// <summary>
    /// Player is dying.
    /// </summary>
    public bool IsDoingDyingSequence = false;

    /// <summary>
    /// Zoom multiplier 0 - 1
    /// </summary>
    public float ZoomLevel = 1;

    /// <summary>
    /// The last weapon we moused over. 
    /// </summary>
    public Entity? LastWeaponHoveredOver; // TODO this is probably a completely unnnecessary field. what do we even use it for
}
