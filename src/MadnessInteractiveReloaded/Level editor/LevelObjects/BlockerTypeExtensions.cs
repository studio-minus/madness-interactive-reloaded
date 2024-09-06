using Walgelijk;

namespace MIR.LevelEditor.Objects;

/// <summary>
/// Some helpers for <see cref="BlockerType"/>.
/// </summary>
public static class BlockerTypeExtensions
{
    /// <summary>
    /// For getting a color to debug draw and differentiate blockers.
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public static Color GetColour(this BlockerType t)
    {
        return t switch
        {
            BlockerType.All => Colors.Purple,
            BlockerType.Characters => Colors.Cyan,
            BlockerType.Bullets => Colors.Red,
            _ => Colors.Gray,
        };
    }

    /// <summary>
    /// Get the <see cref="CollisionLayers"/>
    /// for this blocker.
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public static uint GetFilterMask(this BlockerType t)
    {
        return t switch
        {
            BlockerType.All => CollisionLayers.BlockAll,
            BlockerType.Characters => CollisionLayers.BlockMovement,
            BlockerType.Bullets => CollisionLayers.BlockBullets,
            _ => CollisionLayers.None,
        };
    }
}
