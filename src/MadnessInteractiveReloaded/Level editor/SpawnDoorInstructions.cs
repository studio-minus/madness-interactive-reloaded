using System;
using System.Numerics;

namespace MIR.LevelEditor;

[Obsolete("Use Door and DoorProperties instead")]
public struct OLD_SpawnDoorInstructions
{
    public const float DoorSpawnOffsetDistance = 100;

    public Vector2 TopLeft;
    public Vector2 TopRight;
    public Vector2 BottomLeft;
    public Vector2 BottomRight;

    public Vector2 FacingDirection;

    public bool IsLevelProgressionDoor;
    public bool Disabled;

    public Vector2 GetCenter() => (TopRight + TopLeft + BottomRight + BottomLeft) / 4;

    public Vector2 GetBottomCenter()
    {
        return (BottomLeft + BottomRight) / 2;
    }

    public void SetBottomCenter(Vector2 c)
    {
        var offset = c - (BottomLeft + BottomRight) / 2;
        TopLeft += offset;
        TopRight += offset;
        BottomLeft += offset;
        BottomRight += offset;
    }

    public static Vector2 CalculateWorldSpawnPoint(Vector2 bottomLeft, Vector2 bottomRight)
    {
        var spawningPoint = (bottomLeft + bottomRight) / 2;
        var dir = (bottomRight - bottomLeft);
        var spawnOffset = Vector2.Reflect(new Vector2(0, 1), Vector2.Normalize(dir));
        spawnOffset.X = (MathF.Abs(spawnOffset.X) > 0.1f ? (spawnOffset.X > 0 ? 1 : -1) : 0) * DoorSpawnOffsetDistance;
        spawnOffset.Y = 0;

        return spawningPoint + spawnOffset;
    }

    public void SetCenter(Vector2 center)
    {
        var offset = center - GetCenter();
        TopRight += offset;
        TopLeft += offset;
        BottomRight += offset;
        BottomLeft += offset;
    }

    /// <summary>
    /// Migration function
    /// </summary>
    public DoorProperties ConvertToDoorProperties()
        => new()
        {
            BottomLeft = BottomLeft,
            BottomRight = BottomRight,
            TopRight = TopRight,
            TopLeft = TopLeft,
            FacingDirection = FacingDirection,
            SpawnPoint = CalculateWorldSpawnPoint(BottomLeft, BottomRight),
            IsPortal = false,
            EnemySpawnerDoor = !Disabled,
            IsLevelProgressionDoor = IsLevelProgressionDoor
        };
}
