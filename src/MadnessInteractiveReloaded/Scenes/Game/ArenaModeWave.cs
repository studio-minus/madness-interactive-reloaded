using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using Walgelijk.AssetManager;
using Walgelijk.AssetManager.Deserialisers;

namespace MIR;

public class ArenaModeWave
{
    public EnemySpawnInstructions[] Enemies = [];
    public string[] Weapons = [];
    public float WeaponChance = 0.5f;
    public int EnemyCount;

    public class AssetDeserialiser : IAssetDeserialiser<ArenaModeWave[]>
    {
        public ArenaModeWave[] Deserialise(Func<Stream> stream, in AssetMetadata assetMetadata)
        {
            using var s = stream();
            using var reader = new StreamReader(s);
            var json = reader.ReadToEnd() ?? throw new Exception("arena mode wave file is empty");
            return JsonConvert.DeserializeObject<ArenaModeWave[]>(json) ?? [];
        }

        public bool IsCandidate(in AssetMetadata assetMetadata)
        {
            return assetMetadata.Tags?.Contains("arena_waves") ?? false;
        }
    }
}//🎈