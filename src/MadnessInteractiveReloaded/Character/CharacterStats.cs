using System;

namespace MIR;

/// <summary>
/// Core traits for characters.
/// </summary>
public class CharacterStats
{
    /// <summary>
    /// Display name
    /// </summary>
    public string Name = "Untitled";

    /// <summary>
    /// The physical size multiplier (once the character is created, this won't have any effect)
    /// </summary>
    public float Scale = 1;

    /// <summary>
    /// The lower this number, the better this character will aim
    /// </summary>
    public float AimingRandomness = 1;

    /// <summary>
    /// The amount of time in seconds this character will wait to start shooting after spawning
    /// </summary>
    public float ShootingTimeout = 1;

    /// <summary>
    /// The ability for this character to recover from gun recoil
    /// </summary>
    public float RecoilHandlingAbility = 0.4f;

    /// <summary>
    /// The chance per shot for it to be an "accurate shot"
    /// </summary>
    public float AccurateShotChance = 0;

    /// <summary>
    /// The more dodge a character has, the more shots it can survive without taking damage
    /// </summary>
    public float DodgeAbility = 0;

    /// <summary>
    /// The jump dodge invulnerability duration in seconds while performing a jump dodge
    /// </summary>
    public float JumpDodgeDuration = 0.7f;

    /// <summary>
    /// Time in seconds for a single walking "step" 
    /// </summary>
    public float WalkHopDuration = 0.33f;

    /// <summary>
    /// Determines what animations are chosen for cool agile moves (like jump dodge)
    /// </summary>
    public AgilitySkillLevel AgilitySkillLevel = AgilitySkillLevel.None;

    /// <summary>
    /// The amount of aiming randomess added when this character is shot at
    /// </summary>
    public float PanicIntensity = 2;

    /// <summary>
    /// Speed and damage for melee attacks
    /// </summary>
    public float MeleeSkill = 0.2f;

    /// <summary>
    /// Added knockback for melee attacks
    /// </summary>
    public float MeleeKnockback = 0;

    /// <summary>
    /// Can this character deflect bullets with melee weapons?
    /// </summary>
    public bool CanDeflect = false;

    /// <summary>
    /// Can the character survive a shot that drained all dodge and still had more damage to inflict?
    /// </summary>
    public bool DodgeOversaturate = false;

    /// <summary>
    /// Amount of health for the head bodypart
    /// </summary>
    public float HeadHealth = 0.2f;

    /// <summary>
    /// Amount of health for the body bodypart
    /// </summary>
    public float BodyHealth = 0.3f;

    /// <summary>
    /// Melee sequence ID for the unarmed attack
    /// </summary>
    public string[] UnarmedSeq = ["unarmed_adept"];

    /// <summary>
    /// Melee sequences ID for the sword attack
    /// </summary>
    public string[] SwordSeq = ["sword_adept_2", "sword_adept"];

    /// <summary>
    /// Melee sequence ID for the two-handed melee attack
    /// </summary>
    public string[] TwoHandedSeq = ["twohanded_blunt"];

    /// <summary>
    /// Melee sequence ID for the blunt melee attack
    /// </summary>
    public string[] BluntSeq = ["blunt"];

    /// <summary>
    /// Melee sequence ID for the two-handed gun attack
    /// </summary>
    public string[] TwoHandedGunSeq = ["twohanded_gun"];

    /// <summary>
    /// Melee sequence ID for the one-handed gun attack
    /// </summary>
    public string[] OneHandedGunSeq = ["onehanded_gun"];

    /// <summary>
    /// Character ability component types
    /// </summary>
    public Type[] Abilities;

    /// <summary>
    /// Optional animation key for the walk cycle.
    /// </summary>
    public string? WalkAnimation = null;

    public override string ToString() => Name;

    public CharacterStats()
    {
    }

    public CharacterStats(CharacterStats stats)
    {
        Name = stats.Name;
        Scale = stats.Scale;
        AimingRandomness = stats.AimingRandomness;
        ShootingTimeout = stats.ShootingTimeout;
        RecoilHandlingAbility = stats.RecoilHandlingAbility;
        AccurateShotChance = stats.AccurateShotChance;
        DodgeAbility = stats.DodgeAbility;
        JumpDodgeDuration = stats.JumpDodgeDuration;
        AgilitySkillLevel = stats.AgilitySkillLevel;
        PanicIntensity = stats.PanicIntensity;
        MeleeSkill = stats.MeleeSkill;
        CanDeflect = stats.CanDeflect;
        DodgeOversaturate = stats.DodgeOversaturate;
        HeadHealth = stats.HeadHealth;
        BodyHealth = stats.BodyHealth;
        UnarmedSeq = [..stats.UnarmedSeq];
        SwordSeq = [.. stats.SwordSeq];
        TwoHandedSeq = [.. stats.TwoHandedSeq];
        BluntSeq = [.. stats.BluntSeq];
        TwoHandedGunSeq = [.. stats.TwoHandedGunSeq];
        OneHandedGunSeq = [.. stats.OneHandedGunSeq];
    }
}
