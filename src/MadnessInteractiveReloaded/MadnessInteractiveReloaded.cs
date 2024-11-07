using MIR.Cutscenes;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.AssetManager.Deserialisers;
using Walgelijk.CommonAssetDeserialisers;
using Walgelijk.CommonAssetDeserialisers.Audio;
using Walgelijk.CommonAssetDeserialisers.Audio.Qoa;
using Walgelijk.Localisation;
using Walgelijk.Onion;
using Walgelijk.Onion.Animations;
using Walgelijk.OpenTK;
using Walgelijk.OpenTK.MotionTK;
using Walgelijk.PortAudio;
using Walgelijk.SimpleDrawing;

namespace MIR;

public class MadnessInteractiveReloaded
{
    public static Game? Game { get; private set; }

    public static void PrepareResourceInitialise()
    {
        // common stuff
        AssetDeserialisers.Register(new OggFixedAudioDeserialiser());
        AssetDeserialisers.Register(new OggStreamAudioDeserialiser());
        AssetDeserialisers.Register(new QoaFixedAudioDeserialiser());
        AssetDeserialisers.Register(new WaveFixedAudioDeserialiser());
        AssetDeserialisers.Register(new FontDeserialiser());

        // game stuff
        AssetDeserialisers.Register(new LevelDeserialiser.AssetDeserialiser());
        AssetDeserialisers.Register(new AnimationDeserialiser.AssetDeserialiser());
        AssetDeserialisers.Register(new CharacterLookDeserialiser.AssetDeserialiser());
        AssetDeserialisers.Register(new CharacterStatsDeserialiser.AssetDeserialiser());
        AssetDeserialisers.Register(new ArenaModeWave.AssetDeserialiser());
        AssetDeserialisers.Register(new Faction.FactionDeserialiser());
        AssetDeserialisers.Register(new ConVarsDeserialiser());
        AssetDeserialisers.Register(new ByteArrayDeserialiser());
        AssetDeserialisers.Register(new CutsceneDeserialiser());
        AssetDeserialisers.Register(new ArmourDeserialiser.AssetDeserialiser());
        AssetDeserialisers.Register(new HandArmourDeserialiser.AssetDeserialiser());
        AssetDeserialisers.Register(new MeleeSequenceDeserialiser.AssetDeserialiser());
        AssetDeserialisers.Register(new CharacterPresetDeserialiser.AssetDeserialiser());
        AssetDeserialisers.Register(new Campaign.AssetDeserialiser());
        AssetDeserialisers.Register(new WeaponDeserialiser.AssetDeserialiser());

        // "temporary" migration bridge
        AssetDeserialisers.Register(new DelegateDeserialiserBridge<Language>(Language.Load, "json"));
        AssetDeserialisers.Register(new DelegateDeserialiserBridge<Video>(p => new Video(p), "mp4"));

        foreach (var a in Directory.EnumerateFiles("resources", "*.waa"))
            Assets.RegisterPackage(a);

        Assets.Load<FixedAudioData>("sounds/null.wav");

        // set fallbacks

        AssetDeserialisers.SetFallbackForType(typeof(Texture), Textures.Error);
        AssetDeserialisers.SetFallbackForType(typeof(FixedAudioData), Assets.LoadNoCache<FixedAudioData>("sounds/error.wav"));
        AssetDeserialisers.SetFallbackForType(typeof(StreamAudioData), Assets.LoadNoCache<StreamAudioData>("sounds/error.ogg"));

        // all of the following should be obsolete at some point:

        Resources.BasePath = "resources";
        Resources.SetBasePathForType<IReadableTexture>("textures");
        Resources.SetBasePathForType<Texture>("textures");
        Resources.SetBasePathForType<AudioData>("sounds");
        Resources.SetBasePathForType<FixedAudioData>("sounds");
        Resources.SetBasePathForType<StreamAudioData>("sounds");
        Resources.SetBasePathForType<Font>("fonts");
        //Resources.SetBasePathForType<CharacterAnimation>("data/animations");
        //Resources.SetBasePathForType<CharacterStats>("data/stats");
        //Resources.SetBasePathForType<CharacterLook>("data/looks");
        //Resources.SetBasePathForType<Level>("data/levels");
        //Resources.SetBasePathForType<Language>("locale");
        //Resources.SetBasePathForType<MeleeSequence>("data/melee_sequences");
        //Resources.SetBasePathForType<Cutscene>("data/cutscenes");
        //Resources.SetBasePathForType<Video>("video");
        //Resources.SetBasePathForType<Campaign>("data/campaigns");

        Resources.RegisterType(typeof(Language), Language.Load);
        Resources.RegisterType(typeof(CharacterAnimation), static s =>
        {
            using var f = new FileStream(s, FileMode.Open, FileAccess.Read);
            return AnimationDeserialiser.Load(f, Path.GetFileNameWithoutExtension(s), Path.GetDirectoryName(s) ?? string.Empty);
        });
        Resources.RegisterType(typeof(CharacterStats), CharacterStatsDeserialiser.Load);
        Resources.RegisterType(typeof(ArmourPiece), ArmourDeserialiser.LoadFromPath);
        Resources.RegisterType(typeof(CharacterLook), CharacterLookDeserialiser.Load);
        Resources.RegisterType(typeof(Level), static s =>
        {
            using var f = new FileStream(s, FileMode.Open, FileAccess.Read);
            return LevelDeserialiser.Load(f, Path.GetFileNameWithoutExtension(s));
        });
        Resources.RegisterType(typeof(Video), p => new Video(p));
        Resources.RegisterType(typeof(ExperimentCharacterPreset), CharacterPresetDeserialiser.Load);
        Resources.RegisterType(typeof(Campaign), Campaign.LoadFromFile);
    }

    public MadnessInteractiveReloaded()
    {
        BuiltInShaders.TexturedFragment = @"#version 330 core

in vec2 uv;
in vec4 vertexColor;

out vec4 color;

uniform sampler2D mainTex;
uniform vec4 tint = vec4(1,1,1,1);

void main()
{
    color = vertexColor * texture(mainTex, uv) * tint;
}";

        try
        {
            var p = Process.GetCurrentProcess();
            p.PriorityBoostEnabled = true;
            p.PriorityClass = ProcessPriorityClass.High;
        }
        catch (global::System.Exception e)
        {
            Logger.Warn($"Failed to set process priority: {e}");
        }

        Game = new Game(
             new OpenTKWindow("Madness Interactive Reloaded", -Vector2.One, new Vector2(1920, 1080) * 0.8f),
             new OpenALAudioRenderer()
             );

        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        Game.Window.SetIcon(TextureLoader.FromFile("resources/textures/icon.png"));
        Game.UpdateRate = 144;
        Game.FixedUpdateRate = 60;
        Game.Window.VSync = false;
        Game.AudioRenderer.Volume = 0.5f;

        PrepareResourceInitialise();

        Game.Console.UI.BackgroundColour = new(25, 0, 0, 250);
        Game.Console.UI.InputBoxColour = new(15, 0, 0);
        Game.Console.UI.FilterBoxColour = new(35, 0, 0);
        Game.Console.UI.BackgroundIntensity = 3;

        Onion.Configuration.RenderLayer = RenderOrders.UserInterface.Layer;
        Onion.Configuration.ScrollSensitivity = 60;
        Onion.Configuration.AudioTrack = AudioTracks.UserInterface;
        Onion.Animation.DefaultDurationSeconds = 0.05f;
        Onion.Animation.Default.Clear();
        //Onion.Animation.Default.Add(new FlickerAnimation());
        Onion.Animation.Default.Add(new FadeAnimation());

        ref var uiTheme = ref Onion.Theme.Base;
        uiTheme.Background = new(Colors.Transparent);
        uiTheme.Foreground = new(Colors.Black.WithAlpha(.8f));
        uiTheme.Font = Fonts.Oxanium;
        uiTheme.FontSize = 16;
        uiTheme.Padding = 3;
        uiTheme.Text = new(Colors.White, Colors.Red, Colors.Red);
        uiTheme.Accent = new(Colors.Red, Colors.White, Colors.White);
        uiTheme.OutlineColour = new(Colors.Red, Colors.Red, Colors.Red * 0.5f);
        uiTheme.OutlineWidth = new(0, 2, 2);
        uiTheme.Rounding = 0;
        uiTheme.FocusBoxColour = Colors.Red;
        uiTheme.ScrollbarWidth = 16;
        uiTheme.ScrollbarTracker = new(Colors.Red.Brightness(0.8f), Colors.White, Colors.Red.Brightness(0.5f));
        uiTheme.ScrollbarBackground = Colors.Red.Brightness(0.2f);

        TextureLoader.Settings.FilterMode = FilterMode.Linear;
        TextureLoader.Settings.WrapMode = WrapMode.Clamp;

        //TODO hoe kan dit beter
        Directory.CreateDirectory(UserData.Paths.BaseDir);
        Directory.CreateDirectory(UserData.Paths.ExperimentDir);
        Directory.CreateDirectory(UserData.Paths.ExperimentCharacterPresets);
        Directory.CreateDirectory(UserData.Paths.CampaignStatsDir);
        MigrateSaveData();

        ModLoader.AddSource(new LocalModCollectionSource(new DirectoryInfo("./mods")));

#if DEBUG
        Game.Profiling.DrawQuickProfiler = false;
        Game.Console.DrawConsoleNotification = true;
        Game.DevelopmentMode = false;
#else
        Game.Profiling.DrawQuickProfiler = false;
        Game.Console.DrawConsoleNotification = false;
        Game.DevelopmentMode = false;
#endif
        //var loadingScene = Game.Scene = TextTestingScene.Create(Game);
        Game.Scene = GameOpeningScene.Create(Game);

        // (duston): try to load an autoexec first, if no autoexec
        // use cmd line args.
        GameLoadingScene.OnFinishedLoading += () =>
        {
            AutoExec.Run();
        };

        Draw.TextMeshGenerator.KerningMultiplier = 0.9f;
        Draw.TextMeshGenerator.LineHeightMultiplier = 0.9f;
        Draw.CacheTextMeshes = -1;

        Game.Compositor.Flags = RenderTargetFlags.HDR;
        Game.Compositor.Enabled = false;

        Game.Start();

        // TODO handle error
        // TODO invoke mod unload thing i dont remember

        Settings.WriteToFile(UserData.Paths.Settings, UserData.Instances.Settings);
        CharacterLookDeserialiser.Save(UserData.Instances.PlayerLook, UserData.Paths.PlayerLookFile);
        ArenaModeSaves.Save(UserData.Instances.ArenaMode);
        ImprobabilityDisks.SaveUnlocked(UserData.Instances.UnlockedImprobabilityDisks);

        foreach (var item in Registries.CampaignStats.GetAllValues())
            try
            {
                if (Registries.Campaigns.Has(item.CampaignId))
                    item.Save();
            }
            catch (System.Exception e)
            {
                Logger.Error($"Failed to save campaign stats & progress for {item.CampaignId}: {e}");
            }

        Game.Scene.Dispose();
        Resources.UnloadAll();
        Assets.ClearRegistry();
    }

    private void MigrateSaveData()
    {
        var oldFolder = new DirectoryInfo("userdata");

        if (oldFolder.Exists)
        {
            Logger.Log($"Old user data folder found at \"{oldFolder.FullName}\"! Migrating to \"{UserData.Paths.BaseDir}\"");
            MadnessUtils.CopyDirectory(oldFolder.FullName, UserData.Paths.BaseDir, true);
            oldFolder.Delete(true);
        }
    }
}
