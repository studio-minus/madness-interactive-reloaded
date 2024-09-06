using Walgelijk;
using System.Numerics;

namespace MIR;

/// <summary>
/// Some extensions for working with <see cref="Rect"/>s.
/// </summary>
public static class WindowExtensions
{
    public static Rect WorldToWindowRect(this Window window, in Rect rect)
    {
        var min = window.WorldToWindowPoint(new Vector2(rect.MinX, rect.MinY));
        var max = window.WorldToWindowPoint(new Vector2(rect.MaxX, rect.MaxY));

        return new Rect(min.X, min.Y, max.X, max.Y);
    }

    public static Rect WindowToWorldRect(this Window window, in Rect rect)
    {
        var min = window.WindowToWorldPoint(new Vector2(rect.MinX, rect.MinY));
        var max = window.WindowToWorldPoint(new Vector2(rect.MaxX, rect.MaxY));

        return new Rect(min.X, min.Y, max.X, max.Y);
    }
}