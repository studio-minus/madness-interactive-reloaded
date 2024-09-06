namespace MIR;

/// <summary>
/// Constant values we need in places.
/// </summary>
public static class MadnessConstants
{
    /// <summary>
    /// The ratio between the background texture resolution and the character/props/weapons texture resolution
    /// </summary>
    public const float BackgroundSizeRatio = CameraMovementSystem.OrthographicSizeMultiplier / (1080f / 800);
    // this ended up existing because of a communication error between me and the artists

}
