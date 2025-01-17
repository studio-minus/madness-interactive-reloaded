using MIR.Serialisation;
using System;
using System.IO;
using System.Text;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.AssetManager.Deserialisers;

namespace MIR;

/// <summary>
/// For loading a character look from a file.
/// </summary>
public static class CharacterLookDeserialiser
{
    public const string NameIdentifier = "name";
    public const string BodyIdentifier = "body";
    public const string HeadIdentifier = "head";
    public const string BloodIdentifier = "blood";
    public const string HandsIdentifier = "hands";
    public const string FeetIdentifier = "feet";
    public const string JitterIdentifier = "jitter";
    public const string HeadFleshIdentifier = "head_flesh";
    public const string BodyFleshIdentifier = "body_flesh";   
    public const string HeadGoreIdentifier = "head_gore";
    public const string BodyGoreIdentifier = "body_gore";

    private static readonly KeyValueDeserialiser<CharacterLook> deserialiser = new(nameof(CharacterLookDeserialiser));

    static CharacterLookDeserialiser()
    {
        deserialiser.RegisterString(NameIdentifier, static (look, name) =>
        {
            look.Name = name;
        });

        deserialiser.RegisterString(BloodIdentifier, static (look, bloodColour) =>
        {
            look.BloodColour = new Color(bloodColour.Trim());
        });

        deserialiser.RegisterBool(JitterIdentifier, static (look, jitterFlag) =>
        {
            look.Jitter = jitterFlag;
        });

        deserialiser.RegisterString(HeadFleshIdentifier, static (look, id) =>
        {
            look.HeadFlesh = new AssetRef<Texture>(id);
        });

        deserialiser.RegisterString(BodyFleshIdentifier, static (look, id) =>
        {
            look.BodyFlesh = new AssetRef<Texture>(id);
        });

        deserialiser.RegisterString(HeadGoreIdentifier, static (look, id) =>
        {
            look.HeadGore = new AssetRef<Texture>(id);
        });

        deserialiser.RegisterString(BodyGoreIdentifier, static (look, id) =>
        {
            look.BodyGore = new AssetRef<Texture>(id);
        });

        deserialiser.RegisterString(FeetIdentifier, static (look, id) =>
        {
            look.Feet = new AssetRef<Texture>(id);
        });

        deserialiser.RegisterString(HandsIdentifier, static (look, armourKey) =>
        {
            if (!Registries.Armour.HandArmour.TryGet(armourKey, out var handArmourPiece))
                throw new Exceptions.SerialisationException($"Attempt to load a unregistered glove armour piece: {armourKey}");

            look.Hands = handArmourPiece;
        });

        deserialiser.RegisterStringArray(BodyIdentifier, static (look, armours) =>
        {
            ArmourPiece? piece = null;
            for (int i = 0; i < armours.Count; i++)
            {
                int bodyLayerIndex = i - 1;
                var armourKey = armours[i];
                if (bodyLayerIndex == -1)
                {
                    if (!Registries.Armour.Body.TryGet(armourKey, out piece))
                        throw new Exceptions.SerialisationException($"Attempt to load an unregistered Body armour piece: {armourKey}");
                }
                else
                {
                    if (!Registries.Armour.BodyAccessory.TryGet(armourKey, out piece))
                        throw new Exceptions.SerialisationException($"Attempt to load an unregistered BodyAccessory armour piece: {armourKey}");
                }

                if (piece == null)
                    continue;
                if (piece.Type is ArmourPieceType.BodyAccessory or ArmourPieceType.Body)
                    look.SetBodyLayer(bodyLayerIndex, piece);
                else
                    throw new Exceptions.SerialisationException($"Attempt to load a non-body armour piece for a body layer");
            }
        });

        deserialiser.RegisterStringArray(HeadIdentifier, static (look, armours) =>
        {
            ArmourPiece? piece = null;
            for (int i = 0; i < armours.Count; i++)
            {
                int headLayerIndex = i - 1;
                var armourKey = armours[i];
                if (headLayerIndex == -1)
                {
                    if (!Registries.Armour.Head.TryGet(armourKey, out piece))
                        throw new Exceptions.SerialisationException($"Attempt to load an unregistered Head armour piece: {armourKey}");
                }
                else
                {
                    if (!Registries.Armour.HeadAccessory.TryGet(armourKey, out piece))
                        throw new Exceptions.SerialisationException($"Attempt to load an unregistered HeadAccessory armour piece: {armourKey}");
                }

                if (piece == null)
                    continue;
                if (piece.Type is ArmourPieceType.HeadAccessory or ArmourPieceType.Head)
                    look.SetHeadLayer(headLayerIndex, piece);
                else
                    throw new Exceptions.SerialisationException($"Attempt to load a non-head armour piece for a head layer");
                headLayerIndex++;
            }
        });
    }

    /// <summary>
    /// Save the given look to the given path. 
    /// This method will throw an exception if any <see cref="ArmourPiece"/>s or <see cref="HandArmourPiece"/>s are given that aren't registered to their respective registries.
    /// </summary>
    /// <exception cref="Exceptions.SerialisationException"></exception>
    /// <exception cref="IOException"></exception>
    public static void Save(CharacterLook look, string path)
    {
        StringBuilder writer = new();

        var bodyAccessory = Registries.Armour.BodyAccessory;
        var headAccessory = Registries.Armour.HeadAccessory;

        writer.AppendLine(GameVersion.Version.ToString());
        writer.AppendLine();

        // Write body
        writer.AppendLine(BodyIdentifier);
        if (Registries.Armour.Body.TryGetKeyFor(look.Body, out string? key) && !string.IsNullOrWhiteSpace(key))
            writer.AppendFormat("   {0}\n", key);
        else
            throw new Exceptions.SerialisationException("Attempt to save characterlook with a non-existent body");

        if (look.BodyLayer1 != null && TryGetKey(bodyAccessory, look.BodyLayer1, out key))
            writer.AppendFormat("   {0}\n", key);
        if (look.BodyLayer2 != null && TryGetKey(bodyAccessory, look.BodyLayer2, out key))
            writer.AppendFormat("   {0}\n", key);
        writer.AppendLine();

        // Write head
        writer.AppendLine(HeadIdentifier);
        if (Registries.Armour.Head.TryGetKeyFor(look.Head, out key) && !string.IsNullOrWhiteSpace(key))
            writer.AppendFormat("   {0}\n", key);
        else
            throw new Exceptions.SerialisationException("Attempt to save characterlook with a non-existent head");

        if (look.HeadLayer1 != null && TryGetKey(headAccessory, look.HeadLayer1, out key))
            writer.AppendFormat("   {0}\n", key);
        if (look.HeadLayer2 != null && TryGetKey(headAccessory, look.HeadLayer2, out key))
            writer.AppendFormat("   {0}\n", key);
        if (look.HeadLayer3 != null && TryGetKey(headAccessory, look.HeadLayer3, out key))
            writer.AppendFormat("   {0}\n", key);
        writer.AppendLine();

        // Write hands
        if (look.Hands != null && Registries.Armour.HandArmour.TryGetKeyFor(look.Hands, out key))
        {
            writer.Append(HandsIdentifier);
            writer.Append(' ');
            writer.AppendLine(key);
        }

        // Write feet
        if (look.Feet.HasValue && Assets.HasAsset(look.Feet.Value.Id))
            writer.AppendFormat("{0} {1}\n", FeetIdentifier, look.Feet.Value);

        // Write flesh
        if (look.HeadFlesh.HasValue)
            writer.AppendFormat("{0} {1}\n", HeadFleshIdentifier, look.HeadFlesh.Value);
        if (look.BodyFlesh.HasValue)
            writer.AppendFormat("{0} {1}\n", BodyFleshIdentifier, look.BodyFlesh.Value);  
        
        // Write gore
        if (look.HeadGore.HasValue)
            writer.AppendFormat("{0} {1}\n", HeadGoreIdentifier, look.HeadGore.Value);
        if (look.BodyGore.HasValue)
            writer.AppendFormat("{0} {1}\n", BodyGoreIdentifier, look.BodyGore.Value);

        writer.AppendLineFormat("{0} {1}", BloodIdentifier, look.BloodColour.ToHexCode());

        if (look.BodyFlesh.HasValue && Assets.HasAsset(look.BodyFlesh.Value.Id))
            writer.AppendFormat("{0} {1}\n", BodyFleshIdentifier, look.BodyFlesh.Value.Id);

        if (look.HeadFlesh.HasValue && Assets.HasAsset(look.HeadFlesh.Value.Id))
            writer.AppendFormat("{0} {1}\n", HeadFleshIdentifier, look.HeadFlesh.Value.Id);

        File.WriteAllText(path, writer.ToString());
    }

    private static bool TryGetKey<T2>(IRegistry<string, T2> registry, in T2 obj, out string? key) where T2 : class
    {
        if (!registry.TryGetKeyFor(obj, out key) || string.IsNullOrWhiteSpace(key))
        {
#if DEBUG
            throw new Exceptions.SerialisationException($"Attempt to serialise invalid asset ({obj.GetType().Name}) because it does not exist in the registry!");
#else
            Logger.Error($"Attempt to serialise invalid asset ({obj.GetType().Name}) because it does not exist in the registry!");
            return false;
#endif
        }

        return true;
    }

    /// <summary>
    /// Load and return a <see cref="CharacterLook"/> from the given path.
    /// </summary>
    /// <exception cref="Exceptions.SerialisationException"></exception>
    /// <exception cref="IOException"></exception>
    public static CharacterLook Load(string path) => deserialiser.Deserialise(path);

    public class AssetDeserialiser : IAssetDeserialiser<CharacterLook>
    {
        public CharacterLook Deserialise(Func<Stream> stream, in AssetMetadata assetMetadata)
        {
            using var s = stream();
            return deserialiser.Deserialise(s, assetMetadata.Path);
        }

        public bool IsCandidate(in AssetMetadata assetMetadata)
            => assetMetadata.Path.EndsWith(".look", StringComparison.InvariantCultureIgnoreCase);
    }
}
