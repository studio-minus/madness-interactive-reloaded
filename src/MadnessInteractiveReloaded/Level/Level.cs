using MIR.LevelEditor.Objects;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Numerics;
using Walgelijk;
using Walgelijk.AssetManager;

namespace MIR;

/// <summary>
/// The place you kill all those people in.
/// </summary>
public class Level
{
    /// <summary>
    /// The current level being played.
    /// </summary>
    public static Level? CurrentLevel = null;

    /// <summary>
    /// The level ID according to the registry. Should be equal to the filename without extension.
    /// </summary>
    [JsonIgnore]
    public string Id { get; internal set; } = string.Empty;

    public string DisplayName = "untitled";

    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public LevelType LevelType;

    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public ProgressionType ProgressionType = ProgressionType.BodyCount;

    public float EnemySpawnInterval = 2f;
    public int MaxEnemyCount = 4;
    public int MaxSimultaneousAttackingEnemies = 2;
    public AssetRef<StreamAudioData> BackgroundMusic;
    public float WeaponChance = 0.5f;
    public Rect LevelBounds;
    //public bool EquipWeaponFromLastLevel = true;
    public int BodyCountToWin = 50;
    public float TimeLimitInSeconds = -1;
    public bool FullZoom = false;
    public AssetRef<Texture> Thumbnail;
    public bool OpeningTransition = true;
    public bool ExitingTransition = true;

    // heavy stuff beyond this point!

    public List<EnemySpawnInstructions> EnemySpawnInstructions = new();
    public List<string> Weapons = new();
    public List<Vector2> FloorLine = new();
    public List<LevelObject> Objects = new();

    public float GetFloorLevelAt(float x)
    {
        if (FloorLine.Count == 0)
            return 0;

        if (FloorLine.Count == 1)
            return FloorLine[0].Y;

        for (int i = 1; i < FloorLine.Count; i++)
        {
            var current = FloorLine[i];
            if (current.X > x)
            {
                //ik heb em
                var previous = FloorLine[i - 1];
                return Utilities.MapRange(previous.X, current.X, previous.Y, current.Y, x);
            }
        }
        return FloorLine[^1].Y;
    }

    public bool IsFlatAt(float x)
    {
        // (duston): Sample the floorline to determine the angle.
        const float threshold = 24;
        const float width = 132;

        var p1 = GetFloorLevelAt(x - width);
        var p2 = GetFloorLevelAt(x + width);

        return p1 > p2 ? p1 - p2 < threshold : p2 - p1 < threshold;
    }

    public bool IsCloseToEdge(float x, float expand = 200)
    {
        return x < LevelBounds.MinX + expand || x > LevelBounds.MaxX - expand;
    }

    [Command(Alias = "CurrentLevel", HelpString = "Returns the current level name")]
    public static CommandResult GetCurrentLevelName()
    {
        if (CurrentLevel != null)
            return CurrentLevel.Id ?? $"Untitled {CurrentLevel.LevelType} level";
        return CommandResult.Warn("No current level is set");
    }
}
