using MIR.LevelEditor.Objects;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.AssetManager.Deserialisers;

namespace MIR;

/// <summary>
/// Load a <see cref="Level"/> from disk.
/// </summary>
public static class LevelDeserialiser
{
    public readonly static JsonSerializerSettings SerializerSettings;

    static LevelDeserialiser()
    {
        SerializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            TypeNameHandling = TypeNameHandling.Auto,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple
        };
    }

    public static LevelEntry LoadLevelEntry(GlobalAssetId id)
    {
        // TODO this is a fucked up hack
        var trimmedLevel = new
        {
            DisplayName = "untitled",
            LevelType = LevelType.Unknown,
            Thumbnail = new AssetRef<Texture>(),
        };

        var metadata = Assets.GetMetadata(id);
        var json = Assets.LoadNoCache<string>(id);

        trimmedLevel = JsonConvert.DeserializeAnonymousType(json, trimmedLevel);
        if (trimmedLevel == null)
            throw new Exception("Loaded intermediate level type is null");

        var entry = new LevelEntry(
            Path.GetFileNameWithoutExtension(metadata.Path),
            trimmedLevel.DisplayName,
            trimmedLevel.Thumbnail,
            trimmedLevel.LevelType,
            new Lazy<Level>(() => Assets.Load<Level>(id).Value, true));

        return entry;
    }

    public static void Save(Level level, string path)
    {
        AssertValidLevel(level);
        File.WriteAllText(path, JsonConvert.SerializeObject(level, SerializerSettings));
    }

    public static Level Load(Stream input, string id)
    {
        using var json = new StreamReader(input, leaveOpen: false);
        var v = JsonConvert.DeserializeObject<Level>(json.ReadToEnd(), SerializerSettings) 
            ?? throw new Exception("Attempt to deserialise null level");
        v.Id = id;
        UpgradeLegacyDoors(v);
        AssertValidLevel(v);
        return v;
    }

    private static void UpgradeLegacyDoors(Level v)
    {
        var vv = v.Objects.OfType<LegacyDoor>().ToArray();

        foreach (var item in vv)
        {
            var newDoor = new Door(null!, item.Instructions.ConvertToDoorProperties());
            v.Objects.Add(newDoor);
        }

        Logger.Debug($"Replaced {vv.Length} legacy doors");

        v.Objects.RemoveAll(d => d is LegacyDoor);
    }

    /// <summary>
    /// Throws an exception if the level is invalid
    /// </summary>
    ///<exception cref="Exceptions.SerialisationException"></exception>
    public static void AssertValidLevel(Level level)
    {
        // needs a player spawn
        if (level.Objects.OfType<PlayerSpawn>().Count() != 1)
            throw new Exceptions.SerialisationException("Level needs exactly one player spawn");

        //if (level.FloorLine == null || !level.FloorLine.Any())
        //    throw new Exceptions.SerialisationException("Level needs a floor line (it is empty or null)");

        // all weapons in the weapon list need to exist
        if (level.Weapons != null)
            foreach (var wpn in level.Weapons)
                if (!string.IsNullOrEmpty(wpn) && !Registries.Weapons.Has(wpn))
                    throw new Exceptions.SerialisationException($"Level has unregistered weapon: {wpn}");

        // all enemies that are to spawn need to have valid properties
        foreach (var instr in level.EnemySpawnInstructions)
        {
            if (!Registries.Stats.Has(instr.StatsKey))
                throw new Exceptions.SerialisationException($"Level has EnemySpawnInstructions with unregistered stats: {instr.StatsKey}");
            if (!Registries.Looks.Has(instr.LookKey))
                throw new Exceptions.SerialisationException($"Level has EnemySpawnInstructions with unregistered look: {instr.LookKey}");
        }

        foreach (var item in level.Objects)
        {
            switch (item)
            {
                // all systems need to exist
                case GameSystem sys:
                    if (!string.IsNullOrEmpty(sys.SystemTypeName) && System.Type.GetType(sys.SystemTypeName) == null)
                        throw new Exceptions.SerialisationException($"Level has GameSystem with invalid system type: {sys.SystemTypeName}");
                    break;
                case NPC npc:
                    // all NPCs need to exist and their weapon needs to exist as well
                    if (!Registries.Stats.Has(npc.Instructions.Stats))
                        throw new Exceptions.SerialisationException($"Level has NPC with unregistered stats: {npc.Instructions.Stats}");
                    if (!Registries.Looks.Has(npc.Instructions.Look))
                        throw new Exceptions.SerialisationException($"Level has NPC with unregistered look: {npc.Instructions.Look}");
                    if (!string.IsNullOrEmpty(npc.Instructions.Weapon) && !Registries.Weapons.Has(npc.Instructions.Weapon))
                        throw new Exceptions.SerialisationException($"Level has NPC with unregistered weapon: {npc.Instructions.Weapon}");
                    break;
            }
        }
    }

    public class AssetDeserialiser : IAssetDeserialiser<Level>
    {
        public Level Deserialise(Func<Stream> stream, in AssetMetadata assetMetadata) 
            => Load(stream(), Path.GetFileNameWithoutExtension(assetMetadata.Path));

        public bool IsCandidate(in AssetMetadata assetMetadata) 
            => assetMetadata.MimeType.Equals("application/json", StringComparison.InvariantCultureIgnoreCase);
    }
}
