using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using Walgelijk;

namespace MIR;

/// <summary>
/// Read weapons from JSON files.
/// </summary>
public static class WeaponReader
{
    private static readonly JsonSerializerSettings serializerSettings = new()
    {
        Formatting = Formatting.Indented,
    };

    /// <summary>
    /// TODO
    /// </summary>
    /// <exception cref="Exception"></exception>
    public static WeaponInstructions ReadWeapon(string path)
    {
        var str = File.ReadAllText(path);
        var obj = JsonConvert.DeserializeObject<WeaponInstructions>(str, serializerSettings) ?? throw new Exception("Attempt to deserialise null weapon");
        Logger.Log("Read weapon " + obj.Id);
        return obj;
    }

    public static void SaveWeapon(WeaponInstructions instr, string path)
    {
        var str = JsonConvert.SerializeObject(instr, serializerSettings);
        File.WriteAllText(path, str);
    }
}
