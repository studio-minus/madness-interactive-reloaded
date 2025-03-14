using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Walgelijk.AssetManager;
using Walgelijk.AssetManager.Deserialisers;

namespace MIR;

public class WaveSequence
{
    public Wave[] Waves = [];

    [JsonIgnore]
    public int TotalTargetCount => Waves.Sum(static w => w.TargetCount);

    public record Wave
    {
        public int MaxSimultaneousEnemyCount = 4;
        public int TargetCount = 10;
        public List<string> Weapons = [];
        public float WeaponChance = 0.1f;
        public float SpawnInterval = 1;
        [JsonConverter(typeof(StringEnumConverter))]
        public WaveMode Mode = WaveMode.Random;
        [JsonConverter(typeof(DefaultSpawnInstructionsConverter))]
        public List<ISpawnInstructions> Instructions = [];
    }

    public class AssetDeserialiser : IAssetDeserialiser<WaveSequence>
    {
        public WaveSequence Deserialise(Func<Stream> stream, in AssetMetadata assetMetadata)
        {
            using var reader = new StreamReader(stream());
            var json = reader.ReadToEnd();
            var obj = JsonConvert.DeserializeObject<WaveSequence>(json) ?? throw new Exception("Can't load null wave sequence");
            return obj;
        }

        public bool IsCandidate(in AssetMetadata assetMetadata)
            => assetMetadata.Path.EndsWith(".json", StringComparison.InvariantCultureIgnoreCase);
    }
}

public class DefaultSpawnInstructionsConverter : JsonConverter<ISpawnInstructions>
{
    public override ISpawnInstructions? ReadJson(JsonReader reader, Type objectType, ISpawnInstructions? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var jsonObject = JToken.Load(reader);
        return new EnemySpawnInstructions
        {
            FactionKey = jsonObject[nameof(EnemySpawnInstructions.FactionKey)]?.ToObject<string?>() ?? "aahw",
            LookKey = jsonObject[nameof(EnemySpawnInstructions.LookKey)]?.ToObject<string?>() ?? throw new Exception("No look provided"),
            StatsKey = jsonObject[nameof(EnemySpawnInstructions.StatsKey)]?.ToObject<string?>() ?? throw new Exception("No stats provided"),
            Weapon = jsonObject[nameof(EnemySpawnInstructions.Weapon)]?.ToObject<PersistentEquippedWeapon?>() ?? new()
        };
    }

    public override void WriteJson(JsonWriter writer, ISpawnInstructions value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public enum WaveMode
{
    Random,
    Sequential
}