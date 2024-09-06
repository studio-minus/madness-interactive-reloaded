using System.Numerics;

namespace MIR;

/// <summary>
/// Sharp boxes are collision boxes for sharp weapons like swords and knives.
/// We use this to check overlaps and then sticking the blade in walls, holes in bodies, and inflicting damage to characters.
/// </summary>
/// <param name="TopLeft"></param>
/// <param name="TopRight"></param>
/// <param name="BottomLeft"></param>
/// <param name="BottomRight"></param>
public record struct WorldSharpBox(Vector2 TopLeft, Vector2 TopRight, Vector2 BottomLeft, Vector2 BottomRight)
{
    public static implicit operator (Vector2 TopLeft, Vector2 TopRight, Vector2 BottomLeft, Vector2 BottomRight)(WorldSharpBox value)
    {
        return (value.TopLeft, value.TopRight, value.BottomLeft, value.BottomRight);
    }

    public static implicit operator WorldSharpBox((Vector2 TopLeft, Vector2 TopRight, Vector2 BottomLeft, Vector2 BottomRight) value)
    {
        return new WorldSharpBox(value.TopLeft, value.TopRight, value.BottomLeft, value.BottomRight);
    }
}
