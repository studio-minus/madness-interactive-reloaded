using MIR.Serialisation;
using System;
using System.IO;
using Walgelijk;
using Walgelijk.AssetManager.Deserialisers;
using Walgelijk.AssetManager;

namespace MIR;

/// <summary>
/// Loads <see cref="MeleeSequence"/> from a file.
/// </summary>
public static class MeleeSequenceDeserialiser
{
    private static readonly string[] delimiters = ["\t", " "];

    /// <summary>
    /// Load a <see cref="MeleeSequence"/>.
    /// </summary>
    public static MeleeSequence Load(Stream stream)
    {
        using var s = new StreamReader(stream);

        var keyIndex = 0;
        var keys = new MeleeSequenceKey[16]; //TODO maak het duidelijk dat je er maar 16 kan hebben

        foreach (var line in BaseDeserialiser.Read(s))
        {
            var text = line.String.AsSpan().Trim();
            var parts = text.ToString().Split(delimiters, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries); //TODO had beter gekund
            if (parts.Length == 3)
            {
                var animId = parts[0];
                var transitionFrame = parts[1];
                var hitFrameText = parts[2];

                var key = new MeleeSequenceKey(null!, 0, null!);

                if (!Registries.Animations.TryGet(animId, out var anim))
                {
                    //maybe its a double sided thing
                    if (animId.StartsWith('[') && animId.EndsWith(']'))
                    {
                        var arr = animId.AsSpan()[1..^1].ToString().Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                        if (arr.Length != 2)
                        {
                            Logger.Warn($"Invalid melee sequence line found. \"{animId}\" has to have 2 entries");
                            continue;
                        }

                        key.Animation = new DoubleSided<CharacterAnimation>();
                        if (!Registries.Animations.TryGet(arr[0], out anim))
                        {
                            Logger.Warn($"Invalid melee sequence line found: \"{animId}\". \"{arr[0]}\" is not a valid animation");
                            continue;
                        }
                        key.Animation.FacingRight = anim;

                        if (!Registries.Animations.TryGet(arr[1], out anim))
                        {
                            Logger.Warn($"Invalid melee sequence line found: \"{animId}\". \"{arr[1]}\" is not a valid animation");
                            continue;
                        }
                        key.Animation.FacingLeft = anim;
                    }
                    else
                    {
                        Logger.Warn($"Invalid melee sequence line found. \"{animId}\" is not a valid animation");
                        continue;
                    }
                }
                else
                    key.Animation = new DoubleSided<CharacterAnimation>(anim, anim);

                if (!int.TryParse(transitionFrame, out key.TransitionFrame))
                {
                    Logger.Warn($"Invalid melee sequence line found. \"{animId}\" is not a valid animation");
                    continue;
                }

                if (!hitFrameText.StartsWith('[') || !hitFrameText.EndsWith(']'))
                {
                    Logger.Warn($"Invalid melee sequence line found. \"{hitFrameText}\" is not a valid array");
                    continue;
                }
                else
                {
                    //                             remove [ and ]
                    var hitFrameList = hitFrameText[1..^1].Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    int[] hitFrames = new int[hitFrameList.Length];
                    for (int i = 0; i < hitFrames.Length; i++)
                    {
                        if (int.TryParse(hitFrameList[i], out var ii))
                            hitFrames[i] = ii;
                        else
                            Logger.Warn($"Invalid melee sequence line found. \"{hitFrameText}\" contains invalid value \"{hitFrameList[i]}\"");
                    }
                    key.HitFrames = hitFrames;
                }

                keys[keyIndex++] = key;
            }
            else
                Logger.Warn("Invalid melee sequence line found. It must contain 3 values delimited by whitespace");
        }

        Array.Resize(ref keys, keyIndex);

        s.Dispose();
        return new MeleeSequence(keys);
    }

    public class AssetDeserialiser : IAssetDeserialiser<MeleeSequence>
    {
        public MeleeSequence Deserialise(Func<Stream> stream, in AssetMetadata assetMetadata)
        {
            using var s = stream();
            return Load(s);
        }

        public bool IsCandidate(in AssetMetadata assetMetadata)
        {
            return assetMetadata.Path.EndsWith(".seq");
        }
    }
}