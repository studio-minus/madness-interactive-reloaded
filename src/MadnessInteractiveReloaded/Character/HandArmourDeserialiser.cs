using MIR.Serialisation;
using System;
using System.IO;
using Walgelijk;
using Walgelijk.AssetManager;

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
    public static readonly string[] TextureIdentifiers = Enum.GetNames<HandLook>();

    /// <summary>
    /// Load a <see cref="HandArmourPiece"/> from the given path
    /// </summary>
    /// <exception cref="Exceptions.SerialisationException"></exception>
    public static HandArmourPiece Load(string path)
    {
        using var file = File.OpenText(path);

        var piece = new HandArmourPiece();
        HandLook? currentIdentifier = null;

        foreach (var d in BaseDeserialiser.Read(file))
        {
            if (TryParseIdentifier(d.String, ref currentIdentifier))
                continue;

            if (currentIdentifier != null)
            {
                var l = currentIdentifier.Value;
                if (!MadnessUtils.GetValueFromString(d.String, out ReadOnlySpan<char> lineValue))
                    throw new Exceptions.SerialisationException($"Failed to process line#{d.LineNumber} in {Path.GetFileName(path)}: 'unknown value: {d.String}'");

                if (d.String.StartsWith(BackIdentifier, StringComparison.InvariantCultureIgnoreCase))
                    piece.GetByLook(l).FacingLeft = Assets.Load<Texture>(lineValue);
                else if (d.String.StartsWith(FrontIdentifier, StringComparison.InvariantCultureIgnoreCase))
                    piece.GetByLook(l).FacingRight = Assets.Load<Texture>(lineValue);
                else
                    throw new Exceptions.SerialisationException($"Failed to process line#{d.LineNumber} in {Path.GetFileName(path)}: 'unknown value: {d.String}'");
            }
            else
            {
                if (d.String.StartsWith(MenuOrderIdentifier, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!MadnessUtils.GetValueFromString(d.String, out piece.MenuOrder))
                        throw new Exceptions.SerialisationException($"Failed to process line#{d.LineNumber} in {Path.GetFileName(path)}: 'failed to read order: {d.String}'");
                }
                else if (d.String.StartsWith(NameIdentifier, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!MadnessUtils.GetValueFromString(d.String, out piece.Name))
                        throw new Exceptions.SerialisationException($"Failed to process line#{d.LineNumber} in {Path.GetFileName(path)}: 'failed to read name: {d.String}'");
                }
            }
        }

        file.Close();
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
}
