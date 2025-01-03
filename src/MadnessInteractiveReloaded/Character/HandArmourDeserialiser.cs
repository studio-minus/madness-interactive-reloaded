using MIR.Serialisation;
using System;
using System.IO;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.AssetManager.Deserialisers;

namespace MIR;

/// <summary>
/// For loading hand armour.
/// </summary>
public static class HandArmourDeserialiser
{
    public const string NameIdentifier = "name";
    public const string FrontIdentifier = "front";
    public const string BackIdentifier = "back";
    public const string MenuOrderIdentifier = "order";
    public const string HiddenIdentifier = "hidden";
    public const string TextureScale = "texture-scale";

    public static readonly string[] TextureIdentifiers = Enum.GetNames<HandLook>();

    /// <summary>
    /// Load a <see cref="HandArmourPiece"/> from the given path
    /// </summary>
    /// <exception cref="Exceptions.SerialisationException"></exception>
    public static HandArmourPiece LoadFromFile(string path) => Load(File.OpenRead(path), path);

    public static HandArmourPiece Load(Stream stream, string debugPath)
    {
        using var s = new StreamReader(stream);

        var piece = new HandArmourPiece();
        HandLook? currentIdentifier = null;

        foreach (var d in BaseDeserialiser.Read(s))
        {
            if (TryParseIdentifier(d.String, ref currentIdentifier))
                continue;

            if (currentIdentifier != null)
            {
                var l = currentIdentifier.Value;
                if (!MadnessUtils.GetValueFromString(d.String, out ReadOnlySpan<char> lineValue))
                    throw new Exceptions.SerialisationException($"Failed to process line#{d.LineNumber} in {Path.GetFileName(debugPath)}: 'unknown value: {d.String}'");

                if (d.String.StartsWith(BackIdentifier, StringComparison.InvariantCultureIgnoreCase))
                    piece.GetByLook(l).FacingLeft = Assets.Load<Texture>(lineValue);
                else if (d.String.StartsWith(FrontIdentifier, StringComparison.InvariantCultureIgnoreCase))
                    piece.GetByLook(l).FacingRight = Assets.Load<Texture>(lineValue);
                else
                    throw new Exceptions.SerialisationException($"Failed to process line#{d.LineNumber} in {Path.GetFileName(debugPath)}: 'unknown value: {d.String}'");
            }
            else
            {
                if (d.String.StartsWith(MenuOrderIdentifier, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!MadnessUtils.GetValueFromString(d.String, out piece.MenuOrder))
                        throw new Exceptions.SerialisationException
                            ($"Failed to process line#{d.LineNumber} in {Path.GetFileName(debugPath)}: 'failed to read order: {d.String}'");
                }
                else if (d.String.StartsWith(NameIdentifier, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!MadnessUtils.GetValueFromString(d.String, out piece.Name))
                        throw new Exceptions.SerialisationException
                            ($"Failed to process line#{d.LineNumber} in {Path.GetFileName(debugPath)}: 'failed to read name: {d.String}'");
                }
                else if (d.String.StartsWith(HiddenIdentifier, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!MadnessUtils.GetValueFromString(d.String, out piece.Hidden))
                        throw new Exceptions.SerialisationException
                            ($"Failed to process line#{d.LineNumber} in {Path.GetFileName(debugPath)}: 'failed to read hidden: {d.String}'");
                }
                else if (d.String.StartsWith(TextureScale, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!MadnessUtils.GetValueFromString(d.String, out piece.TextureScale))
                        throw new Exceptions.SerialisationException
                            ($"Failed to process line#{d.LineNumber} in {Path.GetFileName(debugPath)}: 'failed to read texture scale: {d.String}'");
                }
            }
        }

        s.Close();
        return piece;
    }

    private static bool TryParseIdentifier(ReadOnlySpan<char> line, ref HandLook? result)
    {
        for (int i = 0; i < TextureIdentifiers.Length; i++)
            if (line.StartsWith(TextureIdentifiers[i], StringComparison.InvariantCultureIgnoreCase))
            {
                result = (HandLook)i;
                return true;
            }

        return false;
    }

    public class AssetDeserialiser : IAssetDeserialiser<HandArmourPiece>
    {
        public HandArmourPiece Deserialise(Func<Stream> stream, in AssetMetadata assetMetadata)
        {
            using var s = stream();
            return Load(s, assetMetadata.Path);
        }

        public bool IsCandidate(in AssetMetadata assetMetadata)
        {
            return assetMetadata.Path.EndsWith(".hand");
        }
    }
}
