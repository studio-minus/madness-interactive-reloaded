using MIR.Disks;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Walgelijk;
using Walgelijk.AssetManager;

namespace MIR;

/// <summary>
/// Cheats.
/// </summary> 
public static class ImprobabilityDisks
{
    /// <summary>
    /// A list of all modifiers in the game
    /// </summary>
    public readonly static Dictionary<string, ImprobabilityDisk> All = new()
    {
        {"tricky", new TrickyDisk()},
        {"fast_melee", new FastMeleeDisk()},
        {"very_strong", new BoostMeleeDisk()},
        {"infinite_ammo", L("FLCL.png", "FLCL", "Your weapons will never run out.")},
        {"grunt", new GruntDisk()},
        {"agent", new AgentDisk()},
        {"engineer", new EngineerDisk()},
        {"soldat", new SoldatDisk()},
        {"more_dodge", new WickDisk()},
        {"fast_walking", L("Gump.png", "Gump", "Increases your movement speed.")},
        {"auditor", new AuditorDisk()},
        {"telekinesis", new TelekinesisDisk()},
        {"jebus", new JebusDisk()},
        {"god", L("Willis.png", "Willis", "You will be invincible.")},
        {"more_enemies", L("Menagerie.png", "Menagerie", "Increases enemy count.")},
        {"fewer_enemies", L("GhostProtocol.png", "Ghost Protocol", "Decreases enemy count.")},
        {"everyone_evil", L("STATEOFEMERGENCY.png", "STATEOFEMERGENCY", "There is no allegiance.")},
    };

    static ImprobabilityDisks()
    {
        SetIncompatible("grunt", "agent", "engineer", "soldat");
        SetIncompatible("auditor", "jebus", "tricky");
        SetIncompatible("jebus", "telekinesis");
        SetIncompatible("more_enemies", "fewer_enemies");
    }

    /// <summary>
    /// Ensures that the two modifiers can't be enabled simultaneously.
    /// </summary>
    public static void SetIncompatible(string a, string b)
    {
        All[a].IncompatibleWith.Add(b);
        All[b].IncompatibleWith.Add(a);
    }

    /// <summary>
    /// Ensures that the all given modifiers can't be enabled simultaneously.
    /// </summary>
    public static void SetIncompatible(params string[] ids)
    {
        for (int a = 0; a < ids.Length; a++)
            for (int b = a + 1; b < ids.Length; b++)
            {
                var ak = ids[a];
                var bk = ids[b];

                All[ak].IncompatibleWith.Add(bk);
                All[bk].IncompatibleWith.Add(ak);
            }
    }

    public static bool IsIncompatible(string a, string b) => All[a].IncompatibleWith.Contains(b) || All[b].IncompatibleWith.Contains(a);

    public static bool IsIncompatibleWithEnabled(string a)
    {
        foreach (var m in All.Values)
            if (m.Enabled && m.IncompatibleWith.Contains(a))
                return true;
        return false;
    }

    public static bool IsEnabled(in string key) => All.TryGetValue(key, out var m) && m.Enabled;

    public static IEnumerable<ImprobabilityDisk> Enabled => All
        .Where(p => IsEnabled(p.Key))
        .Select(p => p.Value);

    public static void Unlock(string id)
    {
        if (UserData.Instances.UnlockedImprobabilityDisks.Add(id))
            SaveUnlocked(UserData.Instances.UnlockedImprobabilityDisks);
    }

    public static bool IsUnlocked(string id) => UserData.Instances.UnlockedImprobabilityDisks.Contains(id);

    public static void SaveUnlocked(IEnumerable<string> unlocked)
    {
        File.WriteAllLines(UserData.Paths.UnlockedImprobabilityDisk, unlocked);
    }

    public static string[] LoadUnlocked()
    {
        if (File.Exists(UserData.Paths.UnlockedImprobabilityDisk))
            return File.ReadAllLines(UserData.Paths.UnlockedImprobabilityDisk);
        return [];
    }

    private static ImprobabilityDisk L(string texture, string name, string description)
    {
        return new ImprobabilityDisk(
            name,
            Assets.Load<Texture>("textures/ui/modifiers/" + texture).Value,
            description
        );
    }

    [Command(HelpString = "List all improbability disks")]
    public static string ListDisks()
    {
        var b = new StringBuilder();
        foreach (var item in All)
            b.AppendLine(item.Key);
        return b.ToString();
    }


    [Command(HelpString = "Unlock the improbability disk with the given ID, or \"all\" to unlock all disks.")]
    public static CommandResult UnlockDisk(string id)
    {
        if (id.Equals("all", System.StringComparison.InvariantCultureIgnoreCase))
        {
            foreach (var k in All.Keys)
                Unlock(k);

            return CommandResult.Info($"Unlocked all disks");
        }

        if (IsUnlocked(id))
            return CommandResult.Error($"Already unlocked {All[id].DisplayName}.");
        Unlock(id);
        return CommandResult.Info($"Unlocked {All[id].DisplayName}");
    }

    [Command(HelpString = "Lock the improbability disk with the given ID")]
    public static CommandResult LockDisk(string id)
    {
        if (!IsUnlocked(id))
            return CommandResult.Error($"Already locked {All[id].DisplayName}.");
        UserData.Instances.UnlockedImprobabilityDisks.Remove(id);
        return CommandResult.Info($"Locked {All[id].DisplayName}");
    }
}
