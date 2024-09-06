using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.AssetManager.Deserialisers;

namespace MIR.Cutscenes;

/// <summary>
/// Load a cutscene from an asset.
/// </summary>
public class CutsceneDeserialiser : IAssetDeserialiser<Cutscene>
{
    //private struct SerialisableSlide
    //{
    //    public string Type;
    //}

    //public static Cutscene Load(string path)
    //{
    //    var raw = File.ReadAllText(path);

    //    var slides = JsonConvert.DeserializeObject<SerialisableSlide[]>() ;

    //    return new Cutscene(slides.Select(s =>
    //    {
    //        ISlide slide;

    //        var textureMetadata = Assets.GetMetadata(s.Texture);

    //        if (textureMetadata.MimeType.Contains("video", StringComparison.InvariantCultureIgnoreCase))
    //            slide = new VideoSlide(new(s.Texture), s.Duration ?? throw new Exception("Cutscene deserialiser: a video slide must provide a duration"), s.Music);
    //        else // must be a texture
    //            slide = new TextureSlide(new(s.Texture), s.Duration ?? throw new Exception("Cutscene deserialiser: a texture slide must provide a duration"), s.Music);

    //        return slide;
    //    }).ToArray());
    //}

    private readonly JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings
    {
        TypeNameHandling = TypeNameHandling.Objects
    };

    public Cutscene Deserialise(Func<Stream> stream, in AssetMetadata assetMetadata)
    {
        using var reader = new StreamReader(stream());
        var raw = reader.ReadToEnd();

        var s = JsonConvert.DeserializeObject<ISlide[]>(raw, jsonSerializerSettings) ?? [];

        return new Cutscene(s);
    }

    public bool IsCandidate(in AssetMetadata assetMetadata)
    {
        return assetMetadata.Path.EndsWith(".json", StringComparison.InvariantCultureIgnoreCase);
    }
}
