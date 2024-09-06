using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using Walgelijk;
using Walgelijk.AssetManager;

namespace MIR;

/// <summary>
/// Provides some information about a campaign, as well as all the level IDs in playing order
/// </summary>
public class Campaign
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public string Id = string.Empty;

    /// <summary>
    /// Display name
    /// </summary>
    public string Name = string.Empty;

    /// <summary>
    /// Author display name
    /// </summary>
    public string Author = string.Empty;

    /// <summary>
    /// Full path to thumbnail
    /// </summary>
    public AssetRef<Texture> Thumbnail = AssetRef<Texture>.None;

    /// <summary>
    /// Display description
    /// </summary>
    public string Description = string.Empty;

    /// <summary>
    /// Array of level IDs in playing order
    /// </summary>
    public string[] Levels = [];

    /// <summary>
    /// The id of the <see cref="CharacterLook"/> to spawn the player with
    /// </summary>
    public string? Look = null;

    /// <summary>
    /// The id of the <see cref="CharacterStats"/> to spawn the player with
    /// </summary>
    public string? Stats = null;

    public Campaign()
    {

    }

    public static Campaign Load(string path)
    {
        var json = File.ReadAllText(path);
        var obj = JsonConvert.DeserializeObject<Campaign>(json) ?? throw new Exception("Can't load null campaign");

        if (!obj.Levels.Distinct().SequenceEqual(obj.Levels))
        {
            throw new Exception($"Campaign {obj.Id} at \"{path}\" contains duplicate levels and may not be loaded");
        }

        obj.Name = obj.Name.ReplaceLineEndings(string.Empty);
        obj.Author = obj.Author.ReplaceLineEndings(string.Empty);
        return obj;
    }
}