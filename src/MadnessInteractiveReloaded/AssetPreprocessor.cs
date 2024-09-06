using System;
using System.IO;
using Walgelijk.AssetManager;

namespace MIR;

public class AssetPreprocessor : IAssetBuilderProcessor
{
    public void Process(ref AssetMetadata m, ReadOnlySpan<byte> data)
    {
        SetTags(ref m);
    }

    private static void SetTags(ref AssetMetadata m)
    {
        var extension = Path.GetExtension(m.Path);

        switch (extension)
        {
            case ".armor":
                m.Tags = ["armor", .. m.Tags];
                break;
            case ".look":
                m.Tags = ["look", .. m.Tags];
                break;
            case ".stats":
                m.Tags = ["stats", .. m.Tags];
                break;
            case ".seq":
                m.Tags = ["melee_sequence", .. m.Tags];
                break;
            case ".preset":
                m.Tags = ["character_preset", .. m.Tags];
                break;
            case ".anim":
                m.Tags = ["character_animation", .. m.Tags];
                break;
        }
    }
}