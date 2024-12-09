using Walgelijk;
using Walgelijk.AssetManager;

namespace MIR;

/// <summary>
/// Holds commonly used textures.
/// </summary>
public static class Textures
{
    static Textures()
    {
        MovingView.Value.WrapMode = WrapMode.Repeat;
    }

    /// <summary>
    /// A 1x1 pixel fully transparent texture.
    /// </summary>
    public static readonly Texture Transparent = new(1, 1, new Color[1] { Colors.Transparent }, false);
    /// <summary>
    /// A 1x1 pixel fully black texture.
    /// </summary>
    public static readonly Texture Black = new(1, 1, new Color[1] { Colors.Black }, false);

    public static readonly AssetRef<Texture> MovingView = Assets.Load<Texture>("textures/moving_view.png");

    /// <summary>
    /// The door texture.
    /// </summary>
    public static readonly AssetRef<Texture> Door = Assets.Load<Texture>("textures/doors/door_1.png");
    public static readonly AssetRef<Texture> TrainLevelExplosion = Assets.Load<Texture>("textures/explosion_1x15.png");

    public static readonly AssetRef<Texture>[] Muzzleflashes = {
            Assets.Load<Texture>("textures/muzzle_flashes/muzzle_flash_1.png"),
            Assets.Load<Texture>("textures/muzzle_flashes/muzzle_flash_2.png"),
            Assets.Load<Texture>("textures/muzzle_flashes/muzzle_flash_3.png"),
            Assets.Load<Texture>("textures/muzzle_flashes/muzzle_flash_4.png"),
        };

    public static readonly AssetRef<Texture>[] EngineSparks =
    {
       Assets.Load<Texture>("textures/engine_spark/spark_1.png"),
       Assets.Load<Texture>("textures/engine_spark/spark_2.png"),
       Assets.Load<Texture>("textures/engine_spark/spark_3.png"),
       Assets.Load<Texture>("textures/engine_spark/spark_4.png"),
       Assets.Load<Texture>("textures/engine_spark/spark_5.png"),
    };

    public static readonly AssetRef<Texture>[] PlayerPosterOverlays =
    {
        Assets.Load<Texture>("textures/props/player_poster_1.png"),
        Assets.Load<Texture>("textures/props/player_poster_2.png"),
        Assets.Load<Texture>("textures/props/player_poster_3.png"),
    };

    public static readonly BloodSpurtTexture[] BloodSpurts = {
            (Assets.Load<Texture>("textures/blood_impact/blood_spurt_1x12.png"), 1, 12, 1, true),
            (Assets.Load<Texture>("textures/blood_impact/blood_spurt_1x14.png"), 1, 14, 1, true),
            (Assets.Load<Texture>("textures/blood_impact/blood_spurt_1x15.png"), 1, 15, 1, true),
            (Assets.Load<Texture>("textures/blood_impact/blood_spurt_1x18.png"), 1, 18, 1, true),
            (Assets.Load<Texture>("textures/blood_impact/blood_spurt_1x20.png"), 1, 20, 1, true),
            (Assets.Load<Texture>("textures/blood_impact/blood_spurt_1x21.png"), 1, 21, 1, true),
            (Assets.Load<Texture>("textures/blood_impact/blood_spurt_1x21.png"), 1, 21, 1, true),
            (Assets.Load<Texture>("textures/blood_impact/blood_spurt_6_1x7.png"), 1, 7, 2f, true),

            (Assets.Load<Texture>("textures/blood_impact/blood_spurt_1_1x15.png"), 1, 15, 3f, false),
            (Assets.Load<Texture>("textures/blood_impact/blood_spurt_3_1x13.png"), 1, 13, 3f, false),
            (Assets.Load<Texture>("textures/blood_impact/blood_spurt_4_1x11.png"), 1, 11, 2f, false),
            (Assets.Load<Texture>("textures/blood_impact/blood_spurt_5_1x8.png"), 1, 8, 2f, false),
            (Assets.Load<Texture>("textures/blood_impact/blood_spurt_2_1x16.png"), 1, 16, 3f, false)
        };

    public readonly static (AssetRef<Texture> texture, int rows, int columns)[] Vuurtjes =
    {
        (Assets.Load<Texture>("textures/vuurtje1_1x31.png"), 1, 31),
        (Assets.Load<Texture>("textures/vuurtje2_1x31.png"), 1, 31),
    };

    public readonly static (AssetRef<Texture> texture, int rows, int columns)[] BulletImpacts =
    {
        (Assets.Load<Texture>("textures/bullet_impact/bulletimpact_1_1x7.png"), 1, 7),
        (Assets.Load<Texture>("textures/bullet_impact/bulletimpact_2_1x10.png"), 1, 10),
        (Assets.Load<Texture>("textures/bullet_impact/bulletimpact_3_1x10.png"), 1, 10),
        (Assets.Load<Texture>("textures/bullet_impact/bulletimpact_4_1x9.png"), 1, 9),
        (Assets.Load<Texture>("textures/bullet_impact/bulletimpact_5_1x12.png"), 1, 12),
    };

    public readonly static (AssetRef<Texture> texture, int rows, int columns)[] DeflectionSpark =
    {
        (Assets.Load<Texture>("textures/bullet_deflect/bulletdeflect_1_1x13.png"), 1, 13),
        (Assets.Load<Texture>("textures/bullet_deflect/bulletdeflect_2_1x10.png"), 1, 10),
        (Assets.Load<Texture>("textures/bullet_deflect/bulletdeflect_3_1x8.png"), 1, 8),
        (Assets.Load<Texture>("textures/bullet_deflect/bulletdeflect_4_1x12.png"), 1, 12),
        (Assets.Load<Texture>("textures/bullet_deflect/bulletdeflect_5_1x8.png"), 1, 8),
    };

    public static AssetRef<Texture> SteamEngineOverlay => Assets.Load<Texture>("textures/engine_overlay.png");

    /// <summary>
    /// The error texture. Like error.vmdl and that pink/black checkerboard texture from Source engine games.
    /// If you see this, someone messed up.
    /// </summary>
    public static Texture Error => Texture.ErrorTexture;

    public static class UserInterface
    {
        public static readonly AssetRef<Texture> ApparelButtonBackground = Assets.Load<Texture>("textures/ui/item_background.png");
        public static readonly AssetRef<Texture> ApparelSelectedButtonBackground = Assets.Load<Texture>("textures/ui/item_background_selected.png");
        public static readonly AssetRef<Texture> Logo = Assets.Load<Texture>("textures/ui/logo.png");

        public static readonly AssetRef<Texture> Locked = Assets.Load<Texture>("textures/ui/locked.png");
        public static readonly AssetRef<Texture> Unlocked = Assets.Load<Texture>("textures/ui/unlocked.png");
        public static readonly AssetRef<Texture> EyeOn = Assets.Load<Texture>("textures/ui/eye_on.png");
        public static readonly AssetRef<Texture> EyeOff = Assets.Load<Texture>("textures/ui/eye_off.png");

        public static readonly AssetRef<Texture> BloodPool = Assets.Load<Texture>("textures/ui/blood_pool.png");

        public static readonly AssetRef<Texture> LoadingFlipbook = Assets.Load<Texture>("textures/ui/loading_8x1.png");

        public static readonly AssetRef<Texture> SmallArrowRight = Assets.Load<Texture>("textures/ui/small_arrow_right.png");
        public static readonly AssetRef<Texture> SmallArrowLeft = Assets.Load<Texture>("textures/ui/small_arrow_left.png");
        public static readonly AssetRef<Texture> SmallExitClose = Assets.Load<Texture>("textures/ui/exit.png");

        public static readonly AssetRef<Texture> MenuBack = Assets.Load<Texture>("textures/ui/back.png");

        public static readonly AssetRef<Texture> BodyCategoryIcon = Assets.Load<Texture>("textures/ui/category_icons/body_icon.png");
        public static readonly AssetRef<Texture> EyesCategoryIcon = Assets.Load<Texture>("textures/ui/category_icons/eyes_icon.png");
        public static readonly AssetRef<Texture> HatCategoryIcon = Assets.Load<Texture>("textures/ui/category_icons/hat_icon.png");
        public static readonly AssetRef<Texture> JacketCategoryIcon = Assets.Load<Texture>("textures/ui/category_icons/jacket_icon.png");
        public static readonly AssetRef<Texture> MaskCategoryIcon = Assets.Load<Texture>("textures/ui/category_icons/mask_icon.png");
        public static readonly AssetRef<Texture> SkinCategoryIcon = Assets.Load<Texture>("textures/ui/category_icons/skin_icon.png");

        public static readonly AssetRef<Texture> EditorNpcPlaceholder = Assets.Load<Texture>("textures/ui/editor_npc_placeholder.png");
        public static readonly AssetRef<Texture> EditorSequenceInstance = Assets.Load<Texture>("textures/ui/editor_sequence_instance.png");
        public static readonly AssetRef<Texture> EditorScriptInstance = Assets.Load<Texture>("textures/ui/editor_script_instance.png");

        public static class ExperimentMode
        {
            public static readonly AssetRef<Texture> CharactersIcon = Assets.Load<Texture>("textures/ui/experimentmode/characters_icon.png");
            public static readonly AssetRef<Texture> GameRulesIcon = Assets.Load<Texture>("textures/ui/experimentmode/gamerules_icon.png");
            public static readonly AssetRef<Texture> MusicIcon = Assets.Load<Texture>("textures/ui/experimentmode/music_icon.png");
            public static readonly AssetRef<Texture> SettingsIcon = Assets.Load<Texture>("textures/ui/experimentmode/settings_icon.png");
            public static readonly AssetRef<Texture> WeaponsIcon = Assets.Load<Texture>("textures/ui/experimentmode/weapons_icon.png");

            public static class MusicPlayer
            {
                public static readonly AssetRef<Texture> PlayButton = Assets.Load<Texture>("textures/ui/experimentmode/play_icon.png");
                public static readonly AssetRef<Texture> StopButton = Assets.Load<Texture>("textures/ui/experimentmode/stop_icon.png");
                public static readonly AssetRef<Texture> RepeatAllButton = Assets.Load<Texture>("textures/ui/experimentmode/repeatall_icon.png");
                public static readonly AssetRef<Texture> RepeatOneButton = Assets.Load<Texture>("textures/ui/experimentmode/repeat1_icon.png");
                public static readonly AssetRef<Texture> RepeatNoneButton = Assets.Load<Texture>("textures/ui/experimentmode/repeatoff_icon.png");
            }
        }
    }

    public static class Decals
    {
        public static readonly AssetRef<Texture> BulletHole = Assets.Load<Texture>("textures/decals/bullet_hole.qoi");
        public static readonly AssetRef<Texture> BloodSplat = Assets.Load<Texture>("textures/decals/blood_splat.qoi");
    }

    public static class Character
    {
        public static AssetRef<Texture> DefaultHeadRight => Assets.Load<Texture>("textures/bodies/default/head_right.png");
        public static AssetRef<Texture> DefaultHeadLeft => Assets.Load<Texture>("textures/bodies/default/head_left.png");

        public static AssetRef<Texture> DefaultBodyRight => Assets.Load<Texture>("textures/bodies/default/body_right.png");
        public static AssetRef<Texture> DefaultBodyLeft => Assets.Load<Texture>("textures/bodies/default/body_left.png");

        public static AssetRef<Texture> DefaultFist => Assets.Load<Texture>("textures/bodies/default/hand_fist.png");
        public static AssetRef<Texture> DefaultFistBack => Assets.Load<Texture>("textures/bodies/default/hand_fist_back.png");

        public static AssetRef<Texture> DefaultRifleGrip => Assets.Load<Texture>("textures/bodies/default/hand_rifle.png");
        public static AssetRef<Texture> DefaultRifleGripBack => Assets.Load<Texture>("textures/bodies/default/hand_rifle_back.png");

        public static AssetRef<Texture> DefaultHandOpen => Assets.Load<Texture>("textures/bodies/default/hand_open.png");
        public static AssetRef<Texture> DefaultHandOpenBack => Assets.Load<Texture>("textures/bodies/default/hand_open_back.png");

        public static AssetRef<Texture> DefaultHandPistol => Assets.Load<Texture>("textures/bodies/default/hand_pistol.png");
        public static AssetRef<Texture> DefaultHandPistolBack => Assets.Load<Texture>("textures/bodies/default/hand_pistol_back.png");

        public static AssetRef<Texture> DefaultHandPoint => Assets.Load<Texture>("textures/bodies/default/hand_point.png");
        public static AssetRef<Texture> DefaultHandPointBack => Assets.Load<Texture>("textures/bodies/default/hand_point_back.png");

        public static AssetRef<Texture> DefaultHandUnderside => Assets.Load<Texture>("textures/bodies/default/hand_underside.png");
        public static AssetRef<Texture> DefaultHandUndersideBack => Assets.Load<Texture>("textures/bodies/default/hand_underside_back.png");

        public static AssetRef<Texture> DefaultHandHoldStock => Assets.Load<Texture>("textures/bodies/default/hand_stock.png");
        public static AssetRef<Texture> DefaultHandHoldStockBack => Assets.Load<Texture>("textures/bodies/default/hand_stock_back.png");

        public static AssetRef<Texture> DefaultFoot => Assets.Load<Texture>("textures/bodies/default/foot.png");

        public static AssetRef<Texture> FleshHead => Assets.Load<Texture>("textures/bodies/gore/flesh_head.png");
        public static AssetRef<Texture> FleshBody => Assets.Load<Texture>("textures/bodies/gore/flesh_body.png");    
        
        public static AssetRef<Texture> GoreHead => Assets.Load<Texture>("textures/bodies/gore/gore_head.png");
        public static AssetRef<Texture> GoreBody => Assets.Load<Texture>("textures/bodies/gore/gore_body.png");

        public static Material GetMaterialForHandLook(HandArmourPiece? gloves, HandLook look, bool back, WeaponType weaponType)
            => SpriteMaterialCreator.Instance.Load(GetTextureForHandLook(gloves, look, back, weaponType));

        public static Texture GetTextureForHandLook(HandArmourPiece? gloves, HandLook look, bool back, WeaponType weaponType)
        {
            //TODO is dit fucked?
            if (look == HandLook.HoldPistol && weaponType == WeaponType.Melee)
                look = HandLook.Fist;

            if (gloves != null)
                return gloves.GetByLook(look).Select(back).Value;

            //TODO misschien moet dit weg? 
            //fallback :) ?
            switch (look)
            {
                case HandLook.Open:
                    return (back ? DefaultHandOpenBack : DefaultHandOpen).Value;
                case HandLook.HoldPistol:
                    return (back ? DefaultHandPistolBack : DefaultHandPistol).Value;
                case HandLook.HoldRifle:
                    return (back ? DefaultRifleGripBack : DefaultRifleGrip).Value;
                case HandLook.HoldUnderside:
                    return (back ? DefaultHandUndersideBack : DefaultHandUnderside).Value;
                case HandLook.Point:
                    return (back ? DefaultHandPointBack : DefaultHandPoint).Value;
                case HandLook.HoldStock:
                    return (back ? DefaultHandHoldStockBack : DefaultHandHoldStock).Value;
                case HandLook.Fist:
                default:
                    return (back ? DefaultFistBack : DefaultFist).Value;
            }
        }
    }
}
