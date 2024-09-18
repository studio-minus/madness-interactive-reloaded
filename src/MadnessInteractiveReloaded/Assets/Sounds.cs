using System;
using System.Linq;
using Walgelijk;
using Walgelijk.AssetManager;

namespace MIR;

/// <summary>
/// Holds commonly used sounds, such as UI, punching, doors, impact, dryfire, etc.
/// </summary>
public static class Sounds
{
    public static readonly SpatialParams? DefaultSpatialParams = null;

    public static readonly Sound Empty = new(Assets.Load<FixedAudioData>("sounds/null.wav").Value, spatialParams: null);

    public static readonly Sound DryFire = SpatialSfx("sounds/firearms/dry_fire.wav");
    public static readonly Sound ShotgunCock = SpatialSfx("sounds/firearms/shotgun_cock.wav");
    public static readonly Sound Conjure = new(Assets.Load<FixedAudioData>("sounds/conjure.wav").Value, spatialParams: null, track: AudioTracks.SoundEffects);
    public static readonly Sound HolyShield = new(Assets.Load<FixedAudioData>("sounds/holy_shield.wav").Value, spatialParams: null, track: AudioTracks.SoundEffects);
    public static readonly Sound HigherPowers = new(Assets.Load<FixedAudioData>("sounds/higher_powers.wav").Value, spatialParams: null, track: AudioTracks.SoundEffects);
    public static readonly Sound Scenesweep = new(Assets.Load<FixedAudioData>("sounds/ui/scenesweep.wav").Value, spatialParams: null, track: AudioTracks.UserInterface);
    public static readonly Sound AccurateShotWarning = new(Assets.Load<FixedAudioData>("sounds/accurate_shot_warning_alt.wav").Value, spatialParams: null, track: AudioTracks.SoundEffects);

    public static readonly Sound DoorOpen = SpatialSfx("sounds/doors/door_open.wav");
    public static readonly Sound DoorClose = SpatialSfx("sounds/doors/door_close.wav");

    public static readonly Sound DeathSound = new(Assets.Load<FixedAudioData>("sounds/death.ogg").Value, false, spatialParams: null, track: AudioTracks.SoundEffects);
    public static readonly Sound TrainExplosion = new(Assets.Load<FixedAudioData>("sounds/train_explosion.ogg").Value, false, spatialParams: null, track: AudioTracks.SoundEffects);

    public static readonly Sound TimeFreezeStart = new(Assets.Load<FixedAudioData>("sounds/ambience/time_freeze_start.wav").Value, false, spatialParams: null, track: AudioTracks.UserInterface);
    public static readonly Sound TimeFreezeLoop = new(Assets.Load<StreamAudioData>("sounds/ambience/time_freeze.ogg").Value, true, spatialParams: null, track: AudioTracks.UserInterface);

    public static readonly Sound OutOfAmmo = SpatialSfx("sounds/out_of_ammo.wav");
    public static readonly Sound ProceedLevel = new(Assets.Load<FixedAudioData>("sounds/proceed.wav").Value, false, spatialParams: null, track: AudioTracks.SoundEffects);

    /// <summary>
    /// Hovering noise
    /// </summary>
    public static readonly Sound UiHover = new(Assets.Load<FixedAudioData>("sounds/ui/hover.wav").Value, spatialParams: null, track: AudioTracks.UserInterface);
    /// <summary>
    /// Accept / enable noise
    /// </summary>
    public static readonly Sound UiConfirm = new(Assets.Load<FixedAudioData>("sounds/ui/confirm.wav").Value, spatialParams: null, track: AudioTracks.UserInterface);
    /// <summary>
    /// Click noise
    /// </summary>
    public static readonly Sound UiPress = new(Assets.Load<FixedAudioData>("sounds/ui/press.wav").Value, spatialParams: null, track: AudioTracks.UserInterface);
    /// <summary>
    /// Bad thing happened noise
    /// </summary>
    public static readonly Sound UiBad = new(Assets.Load<FixedAudioData>("sounds/ui/bad.wav").Value, spatialParams: null, track: AudioTracks.UserInterface);

    public static readonly Sound DeathMusic = new(Assets.Load<StreamAudioData>("sounds/music/death.ogg").Value, false, spatialParams: null, track: AudioTracks.Music);

    public static readonly Sound[] NearMiss =
    {
        SpatialSfx("sounds/nearmiss/2.wav"),
        SpatialSfx("sounds/nearmiss/3.wav"),
        SpatialSfx("sounds/nearmiss/4.wav"),
        SpatialSfx("sounds/nearmiss/5.wav"),
    };

    public static readonly Sound[] MetalBulletImpact =
    {
        SpatialSfx("sounds/bullet_impact/metal_01.wav"),
        SpatialSfx("sounds/bullet_impact/metal_02.wav"),
        SpatialSfx("sounds/bullet_impact/metal_03.wav"),
        SpatialSfx("sounds/bullet_impact/metal_04.wav"),
    };

    public static readonly Sound[] BulletDeflection =
    {
        SpatialSfx("sounds/deflection/bullet_deflect_1.wav"),
        SpatialSfx("sounds/deflection/bullet_deflect_2.wav"),
        SpatialSfx("sounds/deflection/bullet_deflect_3.wav"),
        SpatialSfx("sounds/deflection/bullet_deflect_4.wav"),
        SpatialSfx("sounds/deflection/bullet_deflect_5.wav"),
        SpatialSfx("sounds/deflection/bullet_deflect_6.wav"),
    };

    public static readonly Sound[] CasingBrassCollide =
    {
        SpatialSfx("sounds/casing/brass_1.wav"),
        SpatialSfx("sounds/casing/brass_2.wav"),
        SpatialSfx("sounds/casing/brass_3.wav"),
        SpatialSfx("sounds/casing/brass_4.wav"),
    };

    public static readonly Sound[] CasingShellCollide =
    {
        SpatialSfx("sounds/casing/shell_1.wav"),
        SpatialSfx("sounds/casing/shell_2.wav"),
        SpatialSfx("sounds/casing/shell_3.wav"),
        SpatialSfx("sounds/casing/shell_4.wav"),
    };

    public static readonly Sound[] Swish =
    {
        SpatialSfx("sounds/swish/swish-1.wav"),
        SpatialSfx("sounds/swish/swish-2.wav"),
        SpatialSfx("sounds/swish/swish-3.wav"),
    };

    public static readonly Sound[] TwoHandedSwish =
    {
        SpatialSfx("sounds/swish/two-swish-1.wav"),
    };

    public static readonly Sound[] Jump =
    {
        SpatialSfx("sounds/jump/jump-1.wav"),
        SpatialSfx("sounds/jump/jump-2.wav"),
        SpatialSfx("sounds/jump/jump-3.wav"),
    };
    public static readonly Sound[] MeleeDodge =
    {
        SpatialSfx("sounds/melee_dodge/melee_dodge_1.wav"),
        SpatialSfx("sounds/melee_dodge/melee_dodge_2.wav"),
        SpatialSfx("sounds/melee_dodge/melee_dodge_3.wav"),
    };

    public static readonly Sound[] SoftBodyImpact =
    {
        SpatialSfx("sounds/physics/soft_body_impact_1.wav"),
        SpatialSfx("sounds/physics/soft_body_impact_2.wav"),
        SpatialSfx("sounds/physics/soft_body_impact_3.wav"),
        SpatialSfx("sounds/physics/soft_body_impact_4.wav"),
        SpatialSfx("sounds/physics/soft_body_impact_5.wav"),
    };

    public static readonly Sound[] GenericPunch =
    {
        SpatialSfx("sounds/punch/generic_punch-1.wav"),
        SpatialSfx("sounds/punch/generic_punch-2.wav"),
        SpatialSfx("sounds/punch/generic_punch-3.wav"),
    };

    public static readonly Sound[] LivingPunch =
    {
        GenericPunch[0],
        GenericPunch[1],
        SpatialSfx("sounds/punch/living_punch-1.wav"),
        SpatialSfx("sounds/punch/living_punch-2.wav"),
        SpatialSfx("sounds/punch/living_punch-3.wav"),
        SpatialSfx("sounds/punch/living_punch-4.wav"),
    };

    public static readonly Sound[] GenericSwordHit =
    {
        SpatialSfx("sounds/sword/blade_generic_impact_1.wav"),
        SpatialSfx("sounds/sword/blade_generic_impact_2.wav"),
        SpatialSfx("sounds/sword/blade_generic_impact_3.wav"),
        SpatialSfx("sounds/sword/blade_generic_impact_4.wav"),
        SpatialSfx("sounds/sword/blade_generic_impact_5.wav"),
        SpatialSfx("sounds/sword/blade_generic_impact_6.wav"),
        SpatialSfx("sounds/sword/blade_generic_impact_7.wav"),
        SpatialSfx("sounds/sword/blade_generic_impact_8.wav"),
        SpatialSfx("sounds/sword/blade_generic_impact_9.wav"),
        SpatialSfx("sounds/sword/blade_generic_impact_10.wav"),
    };

    public static readonly Sound[] LivingSwordHit =
    {
        GenericSwordHit[0],
        GenericSwordHit[1],
        GenericSwordHit[2],
        GenericSwordHit[3],
        GenericSwordHit[4],
        GenericSwordHit[5],
        GenericSwordHit[6],
        GenericSwordHit[7],
        GenericSwordHit[8],
        GenericSwordHit[9],
        SpatialSfx("sounds/sword/blade_living_impact_1.wav"),
        SpatialSfx("sounds/sword/blade_living_impact_2.wav"),
    };

    public static readonly AssetRef<FixedAudioData>[] FirearmCollision = [.. Assets.EnumerateFolder("sounds/weapon_drop/firearm").Select(static s => new AssetRef<FixedAudioData>(s))];
    public static readonly AssetRef<FixedAudioData>[] SwordCollision = [.. Assets.EnumerateFolder("sounds/weapon_drop/sword").Select(static s => new AssetRef<FixedAudioData>(s))];

    public static class MeleeClash
    {
        /// <summary>
        /// Returns an appropriate clash sound for the given weapon interaction
        /// </summary>
        public static Sound GetClashFor(WeaponData? a, WeaponData? b)
        {
            if (a == null && b == null)
                return Utilities.PickRandom(GenericPunch);

            var aT = a?.MeleeDamageType;
            var bT = b?.MeleeDamageType;

            if (aT is MeleeDamageType.Blade && bT is MeleeDamageType.Blade)
                return Utilities.PickRandom(Blade);

            if ((aT is MeleeDamageType.Blunt or MeleeDamageType.Axe) && (bT is MeleeDamageType.Blunt or MeleeDamageType.Axe))
                return Utilities.PickRandom(Blunt);

            return Utilities.PickRandom(Utilities.PickRandom(Blade, Blunt)); // fallback random shit
        }

        /// <summary>
        /// Returns an appropriate clash sound for the given weapon interaction
        /// </summary>
        public static Sound GetClashFor(Scene scene, ComponentRef<WeaponComponent> a, ComponentRef<WeaponComponent> b)
        {
            WeaponData? ad = null, bd = null;

            if (a.TryGet(scene, out var aC))
                ad = aC.Data;
            if (b.TryGet(scene, out var bC))
                bd = bC.Data;

            return GetClashFor(ad, bd);
        }

        public static readonly Sound Parry = SpatialSfx("sounds/melee_clash/parry.wav");

        public static readonly Sound[] Blade =
        {
            SpatialSfx("sounds/melee_clash/blade/blade_clash_1.wav"),
            SpatialSfx("sounds/melee_clash/blade/blade_clash_2.wav"),
            SpatialSfx("sounds/melee_clash/blade/blade_clash_3.wav"),
            SpatialSfx("sounds/melee_clash/blade/blade_clash_4.wav"),
            SpatialSfx("sounds/melee_clash/blade/blade_clash_5.wav"),
        };   
        
        public static readonly Sound[] Blunt =
        {
            SpatialSfx("sounds/melee_clash/blunt/blunt_clash_1.wav"),
            SpatialSfx("sounds/melee_clash/blunt/blunt_clash_2.wav"),
            SpatialSfx("sounds/melee_clash/blunt/blunt_clash_3.wav"),
            SpatialSfx("sounds/melee_clash/blunt/blunt_clash_4.wav"),
        };
    }

    private static Sound SpatialSfx(string v)
    {
        var data = Assets.Load<FixedAudioData>(v).Value;
        return new(data, track: AudioTracks.SoundEffects, spatialParams: data.ChannelCount == 2 ? null : DefaultSpatialParams);
    }
}