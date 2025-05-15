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

public class DefaultSpawnInstructionsConverter : JsonConverter<List<ISpawnInstructions>>
{
    public override List<ISpawnInstructions>? ReadJson(JsonReader reader, Type objectType, List<ISpawnInstructions>? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var array = JArray.Load(reader);
        var created = new List<ISpawnInstructions>();
        
        for (int i = 0; i < array.Count; i++)
        {
            var obj = array.Children().ElementAt(i);
            created.Add(new EnemySpawnInstructions
            {
                FactionKey = obj["factionKey"]?.ToObject<string?>() ?? "aahw",
                LookKey = obj["lookKey"]?.ToObject<string?>() ?? throw new Exception("No look provided"),
                StatsKey = obj["statsKey"]?.ToObject<string?>() ?? throw new Exception("No stats provided"),
                Weapon = obj["weapon"]?.ToObject<PersistentEquippedWeapon?>() ?? null
            });
        }

        return created;
    }

    public override void WriteJson(JsonWriter writer, List<ISpawnInstructions>? value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public enum WaveMode
{
    Random,
    Sequential
}