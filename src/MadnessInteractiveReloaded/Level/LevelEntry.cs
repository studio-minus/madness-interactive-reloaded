using Newtonsoft.Json;
using System;
using Walgelijk;
using Walgelijk.AssetManager;

namespace MIR;

public class LevelEntry
{
    // TODO this entire class is a fucking hack! 🎅🎅
    public readonly string Id;
    public readonly string DisplayName;
    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public readonly LevelType LevelType;
    public readonly AssetRef<Texture> Thumbnail = default;
    public readonly Lazy<Level> Level;

    public LevelEntry(string id, string displayName, AssetRef<Texture> thumbnail, LevelType levelType, Lazy<Level> level)
    {
        Id = id;
        DisplayName = displayName;
        Thumbnail = thumbnail;
        Level = level;
        LevelType = levelType;
    }
}
