using System;
using Walgelijk;

namespace MIR;

/// <summary>
/// Component that stores some data needed to render and process the main menu
/// </summary>
public class MainMenuComponent : Component, IDisposable
{
    /// <summary>
    /// Horizontal and vertical resolution of the render target that contains the player
    /// </summary>
    public const int PlayerTargetTextureSize = 1024;

    /// <summary>
    /// The most recently played character animation for the player
    /// </summary>
    public CharacterAnimation? LastCharAnimation = null;

    /// <summary>
    /// Value from 0 to 1 that follows an easing curve during the first second of the main menu being displayed.
    /// Used for some UI animations. Based on <see cref="Time"/>
    /// </summary>
    public float AnimationProgress;
    /// <summary>
    /// The time in seconds that the menu screen has been active for
    /// </summary>
    public float Time = 0;
    /// <summary>
    /// Used by some elements on the menu for the initial "flicker" effect. It is determined by <see cref="Time"/>
    /// </summary>
    public bool AnimationFlicker = false;
    /// <summary>
    /// The rectangle in screen space that determines where on the screen the player should be drawn
    /// </summary>
    public Rect PlayerDrawRect;
    /// <summary>
    /// The render target that contains the player
    /// </summary>
    public RenderTexture PlayerDrawTarget = new(PlayerTargetTextureSize, PlayerTargetTextureSize, flags: RenderTargetFlags.None);

    public void Dispose()
    {
        PlayerDrawTarget.Dispose();
    }
}
