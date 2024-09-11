using MIR.Serialisation;
using System;
using System.IO;
using System.Xml.Serialization;
using Walgelijk.AssetManager.Deserialisers;
using Walgelijk.AssetManager;
using System.Linq;
using System.Text;

namespace MIR;

/// <summary>
/// Load <see cref="CharacterStats"/> from a file.
/// </summary>
public static class CharacterStatsDeserialiser
{
    private static readonly KeyValueDeserialiser<CharacterStats> deserialiser = new(nameof(CharacterStatsDeserialiser));

    static CharacterStatsDeserialiser()
    {
        deserialiser.RegisterFloat("scale", (s, v) => s.Scale = float.Clamp(v, 0.1f, 4));
        deserialiser.RegisterFloat("hop_duration", (s, v) => s.WalkHopDuration = v);
        deserialiser.RegisterFloat("aiming_randomness", (s, v) => s.AimingRandomness = v);
        deserialiser.RegisterFloat("shooting_timeout", (s, v) => s.ShootingTimeout = v);
        deserialiser.RegisterFloat("recoil_handling", (s, v) => s.RecoilHandlingAbility = v);
        deserialiser.RegisterFloat("accurate_shot_chance", (s, v) => s.AccurateShotChance = v);
        deserialiser.RegisterFloat("dodge", (s, v) => s.DodgeAbility = v);
        deserialiser.RegisterFloat("melee", (s, v) => s.MeleeSkill = v);
        deserialiser.RegisterFloat("melee_knockback", (s, v) => s.MeleeKnockback = v);
        deserialiser.RegisterFloat("panic", (s, v) => s.PanicIntensity = v);
        deserialiser.RegisterFloat("head_health", (s, v) => s.HeadHealth = v);
        deserialiser.RegisterFloat("body_health", (s, v) => s.BodyHealth = v);
        deserialiser.RegisterFloat("jump_dodge_duration", (s, v) => s.JumpDodgeDuration = v);

        deserialiser.RegisterBool("can_deflect", (s, v) => s.CanDeflect = v);
        deserialiser.RegisterBool("dodge_oversaturate", (s, v) => s.DodgeOversaturate = v);
        deserialiser.RegisterStringArray("abilities", (s, v) => s.Abilities = [.. v.Select(GetAbilityTypeFromString)]);

        deserialiser.RegisterString("name", (s, v) => s.Name = v);
        deserialiser.RegisterStringArray("unarmed_seq", (s, v) => s.UnarmedSeq = [..v]);
        deserialiser.RegisterStringArray("sword_seq", (s, v) => s.SwordSeq = [.. v]);
        deserialiser.RegisterStringArray("two_handed_seq", (s, v) => s.TwoHandedSeq = [.. v]);
        deserialiser.RegisterStringArray("two_handed_gun_seq", (s, v) => s.TwoHandedGunSeq = [.. v]);
        deserialiser.RegisterStringArray("blunt_seq", (s, v) => s.BluntSeq = [.. v]);
        deserialiser.RegisterString("agility_skill_level", (s, v) => s.AgilitySkillLevel = Enum.Parse<AgilitySkillLevel>(v, true));
    }

    public static CharacterStats Load(string path)
        => deserialiser.Deserialise(path);

    public class AssetDeserialiser : IAssetDeserialiser<CharacterStats>
    {
        public CharacterStats Deserialise(Func<Stream> stream, in AssetMetadata assetMetadata)
        {
            using var s = stream();
            return deserialiser.Deserialise(s, assetMetadata.Path);
        }

        public bool IsCandidate(in AssetMetadata assetMetadata)
            => assetMetadata.Path.EndsWith(".stats", StringComparison.InvariantCultureIgnoreCase);
    }

    /// <summary>
    /// Save the given stats to the given path. 
    /// </summary>
    /// <exception cref="IOException"></exception>
    public static void Save(CharacterStats stats, string path)
    {
        StringBuilder writer = new();

        writer.AppendLine(GameVersion.Version.ToString());
        writer.AppendLine();
        writer.AppendLineFormat("scale {0}", stats.Scale);
        writer.AppendLineFormat("hop_duration {0}", stats.WalkHopDuration);
        writer.AppendLineFormat("aiming_randomness {0}", stats.AimingRandomness);
        writer.AppendLineFormat("shooting_timeout {0}", stats.ShootingTimeout);
        writer.AppendLineFormat("recoil_handling {0}", stats.RecoilHandlingAbility);
        writer.AppendLineFormat("accurate_shot_chance {0}", stats.AccurateShotChance);
        writer.AppendLineFormat("dodge {0}", stats.DodgeAbility);
        writer.AppendLineFormat("melee {0}", stats.MeleeSkill);
        writer.AppendLineFormat("melee_knockback {0}", stats.MeleeKnockback);
        writer.AppendLineFormat("panic {0}", stats.PanicIntensity);
        writer.AppendLineFormat("head_health {0}", stats.HeadHealth);
        writer.AppendLineFormat("body_health {0}", stats.BodyHealth);
        writer.AppendLineFormat("jump_dodge_duration {0}", stats.JumpDodgeDuration);
        writer.AppendLine();

        writer.AppendLineFormat("can_deflect {0}", stats.CanDeflect);
        writer.AppendLineFormat("dodge_oversaturate {0}", stats.DodgeOversaturate);
        if (stats.Abilities != null && stats.Abilities.Length > 0)
        {
            writer.AppendLine();
            writer.AppendLine("abilities");
            foreach (var item in stats.Abilities)
                writer.AppendLineFormat("\t{0}", item.FullName);
        }
        writer.AppendLine();

        writer.AppendLineFormat("name {0}", stats.Name);
        writer.AppendLineFormat("unarmed_seq {0}", stats.UnarmedSeq);
        writer.AppendLineFormat("sword_seq {0}", stats.SwordSeq);
        writer.AppendLineFormat("two_handed_seq {0}", stats.TwoHandedSeq);
        writer.AppendLineFormat("two_handed_gun_seq {0}", stats.TwoHandedGunSeq);
        writer.AppendLineFormat("blunt_seq {0}", stats.BluntSeq);
        writer.AppendLineFormat("agility_skill_level {0}", stats.AgilitySkillLevel.ToString());

        File.WriteAllText(path, writer.ToString());
    }

    public static Type GetAbilityTypeFromString(string a)
    {
        var t = Type.GetType(a, false, true) ?? throw new Exception($"Type \"{a}\" not found");
        if (t.IsAssignableTo(typeof(CharacterAbilityComponent)))
            return t;
        throw new Exception($"Type \"{a}\" is not a {nameof(CharacterAbilityComponent)}");
    }
}
