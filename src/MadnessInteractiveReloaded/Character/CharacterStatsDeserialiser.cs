using MIR.Serialisation;
using System;
using System.IO;
using System.Xml.Serialization;
using Walgelijk.AssetManager.Deserialisers;
using Walgelijk.AssetManager;
using System.Linq;

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
        deserialiser.RegisterString("unarmed_seq", (s, v) => s.UnarmedSeq = v);
        deserialiser.RegisterString("sword_seq", (s, v) => s.SwordSeq = v);
        deserialiser.RegisterString("two_handed_seq", (s, v) => s.TwoHandedSeq = v);
        deserialiser.RegisterString("two_handed_gun_seq", (s, v) => s.TwoHandedGunSeq = v);
        deserialiser.RegisterString("blunt_seq", (s, v) => s.BluntSeq = v);
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
        using StreamWriter writer = new(path);
        writer.WriteLine(GameVersion.Version);
        writer.WriteLine();
        try
        {
            writer.WriteLine("scale {0}", stats.Scale);
            writer.WriteLine("hop_duration {0}", stats.WalkHopDuration);
            writer.WriteLine("aiming_randomness {0}", stats.AimingRandomness);
            writer.WriteLine("shooting_timeout {0}", stats.ShootingTimeout);
            writer.WriteLine("recoil_handling {0}", stats.RecoilHandlingAbility);
            writer.WriteLine("accurate_shot_chance {0}", stats.AccurateShotChance);
            writer.WriteLine("dodge {0}", stats.DodgeAbility);
            writer.WriteLine("melee {0}", stats.MeleeSkill);
            writer.WriteLine("melee_knockback {0}", stats.MeleeKnockback);
            writer.WriteLine("panic {0}", stats.PanicIntensity);
            writer.WriteLine("head_health {0}", stats.HeadHealth);
            writer.WriteLine("body_health {0}", stats.BodyHealth);
            writer.WriteLine("jump_dodge_duration {0}", stats.JumpDodgeDuration);
            writer.WriteLine();

            writer.WriteLine("can_deflect {0}", stats.CanDeflect);
            writer.WriteLine("dodge_oversaturate {0}", stats.DodgeOversaturate);
            if (stats.Abilities != null && stats.Abilities.Length > 0)
            {
                writer.WriteLine();
                writer.WriteLine("abilities");
                foreach (var item in stats.Abilities)
                    writer.WriteLine("\t{0}", item.FullName);
            }
            writer.WriteLine();

            writer.WriteLine("name {0}", stats.Name);
            writer.WriteLine("unarmed_seq {0}", stats.UnarmedSeq);
            writer.WriteLine("sword_seq {0}", stats.SwordSeq);
            writer.WriteLine("two_handed_seq {0}", stats.TwoHandedSeq);
            writer.WriteLine("two_handed_gun_seq {0}", stats.TwoHandedGunSeq);
            writer.WriteLine("blunt_seq {0}", stats.BluntSeq);
            writer.WriteLine("agility_skill_level {0}", stats.AgilitySkillLevel.ToString());
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            writer.Dispose();
        }
    }

    public static Type GetAbilityTypeFromString(string a)
    {
        var t = Type.GetType(a, false, true) ?? throw new Exception($"Type \"{a}\" not found");
        if (t.IsAssignableTo(typeof(CharacterAbilityComponent)))
            return t;
        throw new Exception($"Type \"{a}\" is not a {nameof(CharacterAbilityComponent)}");
    }
}
