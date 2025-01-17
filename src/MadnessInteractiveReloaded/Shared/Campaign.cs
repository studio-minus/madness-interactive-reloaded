using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.AssetManager.Deserialisers;

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

    /// <summary>
    /// Campaign stats won't be saved to disk if true.
    /// </summary>
    public bool Temporary;

    /// <summary>
    /// If true, this campaign won't show up in the campaign list UI.
    /// </summary>
    public bool Hidden;

    /// <summary>
    /// Order in displayed lists.
    /// </summary>
    public int Order = 0;

    public Campaign()
    {

    }

    public static Campaign LoadFromFile(string path)
    {
        return Load(File.OpenRead(path), path);
    }

    public static Campaign Load(Stream stream, string debugPath)
    {
        using var reader = new StreamReader(stream);

        var json = reader.ReadToEnd();
        var obj = JsonConvert.DeserializeObject<Campaign>(json) ?? throw new Exception("Can't load null campaign");

        if (!obj.Levels.Distinct().SequenceEqual(obj.Levels))
        {
            throw new Exception($"Campaign {obj.Id} at \"{debugPath}\" contains duplicate levels and may not be loaded");
        }

        obj.Name = obj.Name.ReplaceLineEndings(string.Empty);
        obj.Author = obj.Author.ReplaceLineEndings(string.Empty);
        return obj;
    }

    public class AssetDeserialiser : IAssetDeserialiser<Campaign>
    {
        public Campaign Deserialise(Func<Stream> stream, in AssetMetadata assetMetadata)
        {
            using var s = stream();
            return Load(s, assetMetadata.Path);
        }

        public bool IsCandidate(in AssetMetadata assetMetadata)
        {
            return assetMetadata.Path.EndsWith(".json");
        }
    }
}