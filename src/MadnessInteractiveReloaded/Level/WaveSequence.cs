using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
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
        public int TargetCount = 10;
        public string[] Weapons = [];
        public float WeaponChance = 0.1f;
        public float SpawnInterval = 1;
        [JsonConverter(typeof(StringEnumConverter))]
        public WaveMode Mode = WaveMode.Random;
        public EnemySpawnInstructions[] Instructions = [];
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

public enum WaveMode
{
    Random,
    Sequential
}