using System;
using System.Linq;
using System.Numerics;

namespace MIR.LevelEditor;

/// <summary>
/// This creates the floor line that the characters walk on.
/// </summary>
public static class FloorlineCalculator
{
    public static float MaxSteepness = 1;

    public static void Calculate(Level lvl)
    {
        const float stepSize = 2;

        var playerSpawn = lvl.Objects.OfType<Objects.PlayerSpawn>().First();
        if (playerSpawn == null)
            return;

        lvl.FloorLine.Clear();
        // TODO suppose you are going from left to right and there is a pit that you can fall into but cannot get out.
        // according to this system you can get out wherever you can, it has no concept of a one-way path

        var walker = playerSpawn.Position;
        WalkDirection(-stepSize, walker, lvl);
        WalkDirection(stepSize, walker, lvl);

        lvl.FloorLine.Sort(static (a, b) =>
        {
            if (a.X == b.X)
                return 0;
            return a.X > b.X ? 1 : -1;
        });

        Simplify(lvl);
    }

    public static void Simplify(Level lvl, float threshold = 0.99f)
    {
        for (int i = lvl.FloorLine.Count - 2; i >= 1; i--)
        {
            var angleLeft = Vector2.Normalize(lvl.FloorLine[i] - lvl.FloorLine[i - 1]);
            var angleRight = Vector2.Normalize(lvl.FloorLine[i + 1] - lvl.FloorLine[i]);

            // remove colinear points
            if (Vector2.Dot(angleLeft, angleRight) > threshold)
                lvl.FloorLine.RemoveAt(i);
        }
    }

    public static Vector2? GetSurfaceAt(Level level, Vector2 origin)
    {
        float topY = float.MinValue;
        foreach (var item in level.Objects)
        {
            if (!item.IsFloor())
                continue;
            var p = item.GetFloorPointAt(origin.X);
            if (!p.HasValue || p > origin.Y || p < origin.Y - CharacterConstants.HalfHeight * 2)
                continue;
            topY = MathF.Max(topY, p.Value);
        }

        return topY == float.MinValue ? null : new Vector2(origin.X, topY);
    }

    public static bool IsInsideFloor(Level level, Vector2 point)
    {
        foreach (var item in level.Objects)
            if (item.IsFloor() && item.ContainsPoint(point))
                return true;
        return false;
    }

    private static void WalkDirection(float stepSize, Vector2 walker, Level lvl)
    {
        Vector2? lastHit = null;
        while (true)
        {
            walker.X += stepSize;

            var hit = GetSurfaceAt(lvl, walker);
            if (!hit.HasValue)
                break;

            if (hit.Value.Y < lvl.LevelBounds.MinY || IsInsideFloor(lvl, walker))
                break;

            if (lvl.FloorLine.Count > 2 && lastHit.HasValue)
            {
                var dir = Vector2.Normalize(lastHit.Value - hit.Value);
                var steepness = MathF.Abs(dir.Y);
                if (steepness > MaxSteepness)
                    break;
            }

            lvl.FloorLine.Add(hit.Value);
            walker.Y = hit.Value.Y + CharacterConstants.HalfHeight;

            lastHit = hit;

            if (!lvl.LevelBounds.ContainsPoint(walker))
                break;
        }
    }
}
