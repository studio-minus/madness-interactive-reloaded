using MIR.Serialisation;
using System;
using System.IO;
using System.Numerics;
using System.Reflection;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.AssetManager.Deserialisers;

namespace MIR;

/// <summary>
/// Contains a bunch of values related to gameplay balancing throughout the codebase. 
/// It is intended to put all previously magic numbers in one place and assign to them a concrete definition
/// 
/// This is NOT "console variables". It's "config variables".
/// </summary>
public class ConVars
{
    public static readonly AssetId DataPath = new("data/convars.txt");
    public static ConVars Instance { get; private set; } = new();

    public static void Initialise()
    {
        Instance = Assets.Load<ConVars>(DataPath);
    }

    [Command(HelpString = "Set or get a gameplay configuration variable by key. Pass ?? as an argument to print every available key.")]
    public static CommandResult Convar(string key, string value = "")
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;

        if (key == "??")
        {
            var fields = typeof(ConVars).GetFields(flags);
            foreach (var item in fields)
                Game.Main.Console.WriteLine(item.Name, ConsoleMessageType.Debug);
            return string.Empty;
        }

        var f = typeof(ConVars).GetField(key, flags);
        if (f is null)
            return CommandResult.Error("Invalid convar");

        if (!string.IsNullOrEmpty(value))
        {
            if (f.FieldType == typeof(float))
                f.SetValue(Instance, float.Parse(value));
            else if (f.FieldType == typeof(int))
                f.SetValue(Instance, int.Parse(value));
            else if (f.FieldType == typeof(string))
                f.SetValue(Instance, value);
            else if (f.FieldType == typeof(bool))
                f.SetValue(Instance, bool.Parse(value));
            else if (f.FieldType == typeof(System.Enum))
                f.SetValue(Instance, System.Enum.Parse(f.FieldType, value));
            else
                return CommandResult.Error("Invalid type: only float, int, string, bool, and enum are allowed");
        }

        return $"{f.Name} = {f.GetValue(Instance)}";
    }

    /* Dear Reader,
     * 
     * The ACTUAL default values are found in the base/data/convars.txt file!
     * Default values in code will only be used if they are absent in convars.txt
     */

    public float PanicMultiplier = 0.15f;

    public float DodgeFromBehindCostMultiplier = 1.2f;
    public float DodgePointBlankCostMultiplier = 50;
    public float PointBlankDistance = 250;
    public float DeflectDodgeCost = 0.1f;

    public float JumpDodgeChance = 0.1f;

    public Vector2 EnemyMeleeDistance = new Vector2(250, 700);
    public Vector2 EnemyGunDistance = new Vector2(1000, 2000);
    public float EnemySafeDistanceFromPlayer = 1000;
    public float EnemyWeaponSearchRange = 500;

    public float InaccuracyMaxDistance = 1000;
}

public class ConVarsDeserialiser : IAssetDeserialiser<ConVars>
{
    private readonly KeyValueDeserialiser<ConVars> deserialiser;

    public ConVarsDeserialiser()
    {
        deserialiser = new KeyValueDeserialiser<ConVars>(nameof(ConVars));
        deserialiser.AutoRegisterAllFields();
    }

    public ConVars Deserialise(Func<Stream> stream, in AssetMetadata assetMetadata)
    {
        using var s = stream();
        return deserialiser.Deserialise(s, nameof(ConVarsDeserialiser));
    }

    public bool IsCandidate(in AssetMetadata assetMetadata) => assetMetadata.Path.EndsWith("convars.txt");
}