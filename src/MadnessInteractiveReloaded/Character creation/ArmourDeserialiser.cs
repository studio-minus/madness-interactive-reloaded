using MIR.Exceptions;
using MIR.Serialisation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Walgelijk;
using Walgelijk.AssetManager;

namespace MIR;

/// <summary>
/// Load <see cref="ArmourPiece"/> from a file.
/// </summary>
public static class ArmourDeserialiser
{
    private const string Name = "name"; // display name
    private const string MenuOrder = "order"; // order in character customation menu
    private const string Type = "type"; // armour type
    private const string TextureScale = "texture-scale"; // texture rendering scale (1 is normal size)
    private const string Left = "left"; // left texture asset
    private const string Right = "right"; // right texture asset
    private const string Category = "category"; // which character customisation category is this in?
    private const string Detachable = "detach"; // can the armour piece be ripped off on impact?
    private const string Hidden = "hidden"; // is this visible in the character creator?
    private const string Broken = "broken"; // optional armour piece to switch to when broken (shot, hit by melee)
    private const string DeflectChance = "deflect-chance"; // optional bullet deflect chance from 0 to 1, where 1 is always deflect
    private const string ProceduralDamage = "damage-size"; // optional procedural damage scale multiplier, see ArmourPiece.ProceduralDamageScale

    private const string Offset = "offset"; // determines offset-left & offset-right. it is a shorthand
    private const string OffsetLeft = "offset-left"; // determines offset of Left texture
    private const string OffsetRight = "offset-right"; // determines offset of Right texture

    private static readonly KeyValueDeserialiser<ArmourPiece> deserialiser = new(nameof(ArmourDeserialiser));

    static ArmourDeserialiser()
    {
        deserialiser.RegisterString(Name, static (p, v) => p.Name = v);
        deserialiser.RegisterInt(MenuOrder, static (p, v) => p.MenuOrder = v);
        deserialiser.RegisterString(Type, static (p, v) => p.Type = Enum.Parse<ArmourPieceType>(v, true));
        deserialiser.RegisterFloat(TextureScale, static (p, v) => p.TextureScale = v);

        deserialiser.RegisterFloat(DeflectChance, static (p, v) => p.DeflectChance = v);

        deserialiser.RegisterString(Left, static (p, v) => p.Left = new(v));
        deserialiser.RegisterString(Right, static (p, v) => p.Right = new(v));
        deserialiser.RegisterStringArray(Broken, static (p, v) => p.BrokenKeys = [.. v]);

        deserialiser.RegisterString(Category, static (p, v) => p.Category = Enum.Parse<CharacterCreationCategory>(v, true));
        deserialiser.RegisterBool(Detachable, static (p, v) => p.Detachable = v);
        deserialiser.RegisterBool(Hidden, static (p, v) => p.Hidden = v);
        deserialiser.RegisterFloat(ProceduralDamage, static (p, v) => p.ProceduralDamageScale = v);

        deserialiser.RegisterVector2(OffsetLeft, static (p, v) => p.OffsetLeft = v);
        deserialiser.RegisterVector2(OffsetRight, static (p, v) => p.OffsetRight = v);
        deserialiser.RegisterVector2(Offset, static (p, v) => p.OffsetLeft = p.OffsetRight = v);
    }

    /// <summary>
    /// Load a <see cref="ArmourPiece"/> from the given input stream
    /// </summary>
    /// <exception cref="Exceptions.SerialisationException"></exception>
    public static ArmourPiece Load(Stream input, in string debugLocation)
    {
        var piece = deserialiser.Deserialise(input, debugLocation);

        if (string.IsNullOrWhiteSpace(piece.Name))
            throw new Exceptions.SerialisationException($"Armour piece {debugLocation} has no name.");

        if (piece.Right == AssetRef<Texture>.None)
            throw new Exceptions.SerialisationException($"Armour piece {debugLocation} requires a \"right\" texture.");

        if (piece.Type == ArmourPieceType.Unknown)
            throw new Exceptions.SerialisationException($"Armour piece {debugLocation} is of an unknown type.");

        return piece;
    }

    /// <summary>
    /// Load a <see cref="ArmourPiece"/> from the given input path
    /// </summary>
    /// <exception cref="Exceptions.SerialisationException"></exception>
    public static ArmourPiece Load(string path)
    {
        using var s = new FileStream(path, FileMode.Open, FileAccess.Read);
        return Load(s, Path.GetFileName(path));
    }

    public static bool Save(ArmourPiece piece, string path)
        => KeyValueDeserialiser<ArmourPiece>.Serialise(GenerateKeyValues(piece), path);

    private static IEnumerable<KeyValuePair<string, string>> GenerateKeyValues(ArmourPiece piece)
    {
        if (!piece.Right.IsValid)
            throw new SerialisationException("Piece has invalid right property");
        if (!piece.Left.IsValid)
            throw new SerialisationException("Piece has invalid left property");

        yield return new(Name, piece.Name);

        if (piece.MenuOrder != 0)
            yield return new(MenuOrder, piece.MenuOrder.ToString());

        yield return new(Type, piece.Type.ToString());
        yield return new(Left, piece.Left.ToString());
        yield return new(Right, piece.Right.ToString());
        yield return new(TextureScale, piece.TextureScale.ToString());
        yield return new(Category, piece.Category.ToString());
        yield return new(Detachable, piece.Detachable.ToString());
        yield return new(Hidden, piece.Hidden.ToString());
        yield return new(ProceduralDamage, piece.ProceduralDamageScale.ToString());

        if (piece.BrokenKeys != null)
        {
            var brokenValues = new StringBuilder();
            // Have to do this because the array values go below the key.
            brokenValues.AppendLine();
            foreach (var broken in piece.BrokenKeys)
                brokenValues.Append(string.Format("{0}{1}{2}", "\t", broken, "\n"));

            yield return new(Broken, brokenValues.ToString());
        }

        if (piece.OffsetLeft == piece.OffsetRight)
        {
            yield return new(Offset, string.Format("{0} {1}", piece.OffsetLeft.X, piece.OffsetLeft.Y));
            yield break;
        }

        yield return new(OffsetLeft, string.Format("{0} {1}", piece.OffsetLeft.X, piece.OffsetLeft.Y));
        yield return new(OffsetRight, string.Format("{0} {1}", piece.OffsetRight.X, piece.OffsetRight.Y));
        yield break;
    }
}

