using Newtonsoft.Json;
using System;
using System.Numerics;
using Walgelijk;
using Walgelijk.AssetManager;

namespace MIR.LevelEditor;

/// <summary>
/// Properties shared across the <see cref="DoorComponent"/> and the <see cref="LevelObjects.Door"/>
/// </summary>
public struct DoorProperties
{
    /// <summary>
    /// The point to spawn the entities at
    /// </summary>
    public Vector2 SpawnPoint;

    /// <summary>
    /// The direction vector that this door is facing
    /// </summary>
    public Vector2 FacingDirection;

    /// <summary>
    /// Should this door be considered by the <see cref="EnemySpawningSystem"/>?
    /// </summary>
    public bool EnemySpawnerDoor;

    /// <summary>
    /// A level can have a level progression door. This door will open when the level win conditions are met.
    /// </summary>
    public bool IsLevelProgressionDoor;

    /// <summary>
    /// A portal door is one through which the player can travel to other levels, as determined by <see cref="DestinationLevel"/>. 
    /// The door will open when the player gets close to it.
    /// </summary>
    public bool IsPortal;

    /// <summary>
    /// The portal ID is used to link two doors together across levels. This ensures correct player spawn positions. 
    /// If no matching door is found on the other side, the player will spawn in the default position;
    /// </summary>
    public string? PortalID;

    /// <summary>
    /// If this door is a portal (<see cref="IsPortal"/>, this is the level that will be loaded upon entering it
    /// </summary>
    public string? DestinationLevel;

    /// <summary>
    /// The texture to use for the door. Will fall back to default if null.
    /// </summary>
    public AssetRef<Texture>? Texture;

    /// <summary>
    /// Returns <see cref="Texture"/> or <see cref="Textures.Door"/>
    /// </summary>
    public readonly Texture EffectiveTexture => Texture ?? Textures.Door;

    public Vector2 TopLeft;
    public Vector2 TopRight;
    public Vector2 BottomLeft;
    public Vector2 BottomRight;

    [JsonIgnore]
    public Vector2 Center
    {
        get => 0.25f * (TopLeft + TopRight + BottomLeft + BottomRight);

        set
        {
            var offset = value - Center;
            TopRight += offset;
            TopLeft += offset;
            BottomRight += offset;
            BottomLeft += offset;
        }
    }

    public Rect GetBoundingBox()
    {
        return new Rect(
            MathF.Min(BottomLeft.X, TopLeft.X),
            MathF.Min(BottomLeft.Y, BottomRight.Y),
            MathF.Max(TopRight.X, BottomRight.X),
            MathF.Max(TopRight.Y, TopLeft.Y)).SortComponents();
    }
}
