using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using Walgelijk;
using Walgelijk.AssetManager.Deserialisers;
using Walgelijk.AssetManager;

namespace MIR;

/// <summary>
/// Read weapons from JSON files.
/// </summary>
public static class WeaponDeserialiser
{
    private static readonly JsonSerializerSettings serializerSettings = new()
    {
        Formatting = Formatting.Indented,
    };

    /// <summary>
    /// TODO
    /// </summary>
    /// <exception cref="Exception"></exception>
    public static WeaponInstructions LoadFromPath(string path) => Load(File.OpenRead(path));

    public static WeaponInstructions Load(Stream s)
    {
        using var reader = new StreamReader(s);
        var data = reader.ReadToEnd();
        var obj = JsonConvert.DeserializeObject<WeaponInstructions>(data, serializerSettings) ?? throw new Exception("Attempt to deserialise null weapon");
        return obj;
    }

    public static void SaveWeapon(WeaponInstructions instr, string path)
    {
        var str = JsonConvert.SerializeObject(instr, serializerSettings);
        File.WriteAllText(path, str);
    }

    public class AssetDeserialiser : IAssetDeserialiser<WeaponInstructions>
    {
        public WeaponInstructions Deserialise(Func<Stream> stream, in AssetMetadata assetMetadata)
        {
            using var s = stream();
            return Load(s);
        }

        public bool IsCandidate(in AssetMetadata assetMetadata)
            => assetMetadata.Path.EndsWith(".json", StringComparison.InvariantCultureIgnoreCase);
    }
}
