using Newtonsoft.Json;
using System.Collections.Generic;
using Walgelijk;
using Walgelijk.AssetManager;

namespace MIR;

/// <summary>
/// Information about weapons.<br></br>
/// Can describe damage, recoil, if swords can deflect this weapons bullets, accuracy, range, etc.
/// </summary>
public class WeaponData
{
    /// <summary>
    /// What is this weapon called?
    /// </summary>
    // Applicable for all weapons
    public string Name = "Missing";

    /// <summary>
    /// How much damage it does.
    /// </summary>
    public float Damage;

    /// <summary>
    /// If the trigger pull does not instantly result in a shot being fired, how long that delay is.
    /// </summary>
    public float UseDelay = 0;
    
    /// <summary>
    /// How far away this weapon is held from the body.
    /// </summary>
    public float MaxHandRangeMultiplier = 1;

    /// <summary>
    /// Other components (by name) that will get added to this weapon.<br></br>
    /// Example: <see cref="AuditorSwordComponent"/>.
    /// </summary>
    public string[]? AdditionalComponents;

    /// <summary>
    /// Systems (by name) that need to be added to the scene for this weapon.<br></br>
    /// Example: <see cref="AuditorSwordSystem"/>.
    /// </summary>
    public string[]? EnsureSystems;

    /// <summary>
    /// How much damage to do when this weapon is thrown at someone.
    /// </summary>
    public float ThrowableDamage = 15;

    /// <summary>
    /// If the weapon is heavy when thrown.
    /// </summary>
    public bool ThrowableHeavy = false;

    /// <summary>
    /// The sharp boxes to use when the weapon is thrown. Like when you throw a knife, the parts that will stab people.
    /// </summary>
    public List<Rect> ThrowableSharpBoxes = new();

    /// <summary>
    /// What weapon type is this?
    /// </summary>
    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public WeaponType WeaponType = WeaponType.Firearm;

    // Only applicable for firearms

    ///
    /// <summary>
    /// How many bullets per shot? Like how shotguns shoot more than 1.
    /// </summary>
    public int BulletsPerShot = 1;

    /// <summary>
    /// How many rounds each mag for this gun starts with.
    /// </summary>
    public int RoundsPerMagazine = 12;

    /// <summary>
    /// How many bullets are shot per burst in burst mode.
    /// </summary>
    public int BurstFireCount = 0;

    /// <summary>
    /// How accurate this weapon is.<br></br>
    /// Calculated like:<br></br>
    /// <c>           
    /// var dir = barrel.direction;<br></br>
    /// dir += Utilities.RandomPointInCircle()* (1 - data.Accuracy);
    /// </c>
    /// </summary>
    public float Accuracy;

    /// <summary>
    /// How much climb the gun has when shooting it.
    /// </summary>
    public float Recoil;

    /// <summary>
    /// How intense the rotation from recoil is.
    /// </summary>
    public float RotationalRecoilIntensity = 1;
    public float RecoilHandling;

    /// <summary>
    /// If the gun shuts as fast as it can while the trigger is held.
    /// </summary>
    public bool Automatic = false;
    
    /// <summary>
    /// Sounds to make for the gunshots.
    /// </summary>
    public List<AssetRef<FixedAudioData>>? ShootSounds;

    /// <summary>
    /// Can a sword deflect these bullets?
    /// </summary>
    public bool CanBulletsBeDeflected = true;
    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public EjectionParticle EjectionParticle;
    public float CasingSize = 1;
    public bool IsPumpAction = false;

    // Only applicable for melee weapons

    /// <summary>
    /// How far a melee weapon can hit people.
    /// </summary>
    public float Range = 500;

    /// <summary>
    /// Can this melee weapon deflect bullets?
    /// </summary>
    public bool CanDeflectBullets = false;

    /// <summary>
    /// Flag that determines whether this melee weapon has "special" abilities. This gives it a unique advantage in combat.
    /// </summary>
    public bool SpecialMelee;

    /// <summary>
    /// Optional set of sounds to play when this weapon is used to hit an enemy.
    /// </summary>
    //public List<AssetRef<FixedAudioData>>? HitSounds; // not yet implemented

    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public MeleeDamageType MeleeDamageType = MeleeDamageType.Firearm;

    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public MeleeSize MeleeSize = MeleeSize.Medium;
}
