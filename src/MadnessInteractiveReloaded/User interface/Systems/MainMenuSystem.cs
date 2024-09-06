using MIR.Controls;
using System;
using System.Linq;
using System.Numerics;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.Localisation;
using Walgelijk.Onion;
using Walgelijk.Onion.Layout;
using Walgelijk.SimpleDrawing;

namespace MIR;

/// <summary>
/// Draws the main menu.
/// </summary>
public class MainMenuSystem : Walgelijk.System
{
    // this whole responsive thing should probaby fuck off honestly
    public const int WindowHeightResponsiveThreshold = 700;

    private static readonly string versionString = "Madness Interactive Reloaded " + GameVersion.Version;
    private static readonly MenuCharacterRenderer menuCharacterRenderer = new();

    public override void Initialise()
    {
        Scene.OnActive += () =>
        {
            if (PersistentSoundHandles.LevelMusic != null)
                Audio.Pause(PersistentSoundHandles.LevelMusic);
            if (PersistentSoundHandles.PauseMusic != null)
                Audio.Pause(PersistentSoundHandles.PauseMusic);
        };
    }

    public override void Update()
    {
        if (!Scene.FindAnyComponent<MainMenuComponent>(out var mm))
        {
            Scene.AttachComponent(Scene.CreateEntity(), new MainMenuComponent());
            return;
        }

        ProcessPlayerCharacter(mm);

        Draw.Reset();
        Draw.ScreenSpace = true;
        mm.AnimationFlicker = (mm.Time > 0.5f && mm.Time < 0.52f) || mm.Time > 0.55f;

        if (mm.AnimationFlicker)
            ProcessUi(mm);

        PositionPlayerDrawRect(mm);

        MenuUiUtils.DrawBackground(Scene, mm.AnimationProgress);
        MenuUiUtils.DrawBorderGradients(Scene, mm.AnimationProgress);
        if (mm.AnimationFlicker)
            MenuUiUtils.DrawLogo(Scene, mm.AnimationProgress, WindowHeightResponsiveThreshold);
        if (Window.Height > WindowHeightResponsiveThreshold)
            DrawPlayer(mm);

        Draw.Colour = Colors.White.WithAlpha(0.5f);
        Draw.Text(versionString, Window.Size - new Vector2(10), Vector2.One, HorizontalTextAlign.Right, VerticalTextAlign.Bottom);

        mm.Time += Time.DeltaTimeUnscaled * 2;
        mm.AnimationProgress = Easings.Cubic.Out(float.Clamp(mm.Time, 0, 1));

#if DEBUG
        if (Input.IsKeyReleased(Key.F5))
            mm.Time = 0;
#endif

        PersistentSoundHandles.MainMenuMusic ??=
            SoundCache.Instance.LoadMusic(Assets.Load<StreamAudioData>("sounds/music/Lothyde/unusual_tranquillity.ogg"));

        //var snd = SoundCache.Instance.LoadMusic(Assets.Load<StreamAudioData>("sounds/music/Lothyde/unusual_tranquillity.ogg"));

        if (!Audio.IsPlaying(PersistentSoundHandles.MainMenuMusic))
            Audio.Play(PersistentSoundHandles.MainMenuMusic);

        Ui.Theme.FontSize(24).ForegroundColor(new Color(0x5865F2)).Rounding(10).OutlineColour(Colors.White).OutlineWidth(new(0,5)).Once();
        Ui.Layout.Size(80, 80).StickRight().StickTop().Move(-10, 10);
        if (Ui.ImageButton(Assets.Load<Texture>("textures/ui/discord.png").Value, ImageContainmentMode.Contain))
        {
            global::System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://discord.gg/invite/eZYgeGdsD6")
            {
                UseShellExecute  = true
            });
        }
    }

    private void ProcessPlayerCharacter(MainMenuComponent mm)
    {
        if (!MadnessUtils.FindPlayer(Scene, out var player, out var character))
        {
            Level.CurrentLevel = null;
            player = Prefabs.CreatePlayer(Scene, default);

            character = Scene.GetComponentFrom<CharacterComponent>(player.Entity);
            character.AimTargetPosition = new Vector2(1000, 0);

            if (!Registries.Weapons.TryGet("walther_ppk", out var instr))
                instr = Registries.Weapons.GetRandomValue();
            var wpn = Prefabs.CreateWeapon(Scene, default, instr!);
            character.EquipWeapon(Scene, wpn);
        }

#if DEBUG
        if (Input.IsKeyReleased(Key.F5))
            character.NeedsLookUpdate = true;
#endif

        if (character.Positioning.IsFlipped)
        {
            character.Positioning.IsFlipped = false;
            character.NeedsLookUpdate = true;
        }

        player.RespondToUserInput = false;
        character.Positioning.GlobalTarget = default;

        if (!character.IsPlayingAnimation)
        {
            var next = mm.LastCharAnimation = GetNextAnimation(mm.LastCharAnimation);
            character.PlayAnimation(next);
        }
    }

    private static CharacterAnimation GetNextAnimation(CharacterAnimation? previous)
    {
        if (previous == null)
            return Utilities.PickRandom(
                Animations.MainMenuLoopDown,
                Animations.MainMenuLoopUp,
                Animations.MainMenuTransitionUp,
                Animations.MainMenuTransitionDown
                );

        if (previous == Animations.MainMenuLoopDown)
            return Utilities.RandomFloat() > 0.2f ? Animations.MainMenuLoopDown : Animations.MainMenuTransitionUp;

        if (previous == Animations.MainMenuLoopUp)
            return Utilities.RandomFloat() > 0.2f ? Animations.MainMenuLoopUp : Animations.MainMenuTransitionDown;

        if (previous == Animations.MainMenuTransitionDown)
            return Animations.MainMenuLoopDown;

        if (previous == Animations.MainMenuTransitionUp)
            return Animations.MainMenuLoopUp;

        return Animations.MainMenuTransitionUp; // fallback into valid state lol
    }

    private void PositionPlayerDrawRect(MainMenuComponent mm)
    {
        var playerDrawRectSizeRatio = Window.Height / 500f;
        var s = 500 * playerDrawRectSizeRatio;
        var r = new Rect(0, 0, s, s);

        var x = Utilities.Lerp(
            Window.Width - r.Width,
            (600 + (Window.Width - r.Width)) / 2,
            Easings.Cubic.Out(float.Clamp(mm.Time * 0.5f, 0, 1)));

        r = r.Translate(x, Window.Height * 1.15f - r.Height);
        mm.PlayerDrawRect = r;
    }

    public override void Render()
    {
        if (!Scene.FindAnyComponent<MainMenuComponent>(out var mm))
            return;

        if (!MadnessUtils.FindPlayer(Scene, out _, out var character))
            return;

        menuCharacterRenderer.Render(Window, mm.PlayerDrawTarget, character);
    }

    private void DrawPlayer(MainMenuComponent mm)
    {
        Draw.Colour = Colors.White.Brightness(Easings.Quad.InOut(Utilities.Clamp(mm.Time * 0.5f - 0.05f)));
        Draw.Texture = mm.PlayerDrawTarget;
        Draw.Quad(mm.PlayerDrawRect);
    }

    private void ProcessUi(MainMenuComponent mm)
    {
        Ui.Layout.Size(330, 355).VerticalLayout();
        if (Window.Height > WindowHeightResponsiveThreshold)
            Ui.Layout.StickBottom(false).StickLeft(false).MoveAbs(70, -70);
        else
            Ui.Layout.MoveAbs(30, 0).CenterVertical();

        Ui.Theme.Padding(0).ForegroundColor(Colors.White).ForegroundTexture(Assets.Load<Texture>("textures/border-top-bottom.png").Value, ImageMode.Slice).Once();
        Ui.StartGroup(true);
        Ui.Layout.DefaultConstraints.Add(new FitContainer(1, null));
        Ui.Layout.DefaultConstraints.Add(new CenterInParent(true, false));
        Ui.Layout.DefaultConstraints.Add(new HeightConstraint(35));
        Ui.Layout.DefaultConstraints.Add(new MoveConstraint(0, 18));
        {
            Ui.Theme.Padding(10).Text(new(Colors.Red, Colors.White, Colors.Red)).Font(Fonts.Oxanium)
                .FontSize(24).Foreground((Appearance)Colors.Transparent).OutlineWidth(0);

            Ui.Decorate(new MenuButtonDecorator());
            if (LeftAlignedButton.Start(Localisation.Get("main-menu-campaigns")))
            {
                Game.Scene = CampaignMenuScene.Load(Game.Main);
                MadnessUtils.Flash(Colors.Black, 0.2f);
            }

            Ui.Decorate(new MenuButtonDecorator());
            if (LeftAlignedButton.Start(Localisation.Get("main-menu-improbability-disks")))
            {
                Game.Scene = ImprobabilityDiskSelectionMenuScene.Load(Game.Main);
                MadnessUtils.Flash(Colors.Black, 0.2f);
            }  
            
            //Ui.Decorate(new MenuButtonDecorator());
            //if (Ui.ClickButton(Localisation.Get("main-menu-arena-mode")))
            //{
            //    Game.Scene = ArenaMenuScene.Load(Game.Main);
            //    MadnessUtils.Flash(Colors.Black, 0.2f);
            //}

            Ui.Decorate(new MenuButtonDecorator());
            if (LeftAlignedButton.Start(Localisation.Get("main-menu-experiment")))
            {
                MadnessUtils.TransitionScene(static game =>
                {
                    var lvl = Registries.Levels["exp_1"]?.Level.Value ?? throw new Exception("The current level is null");
                    return ExperimentScene.Create(game, lvl, new SceneCacheSettings(lvl.Id));
                });
            }

            Ui.Decorate(new MenuButtonDecorator());
            if (LeftAlignedButton.Start(Localisation.Get("main-menu-char-customisation")))
            {
                Game.Scene = CharacterCreationScene.Load(Game.Main);
                MadnessUtils.Flash(Colors.Black, 0.2f);
            }

            Ui.Decorate(new MenuButtonDecorator());
            if (LeftAlignedButton.Start(Localisation.Get("main-menu-mod-menu")))
            {
                Game.Scene = ModMenuScene.Load(Game.Main);
                MadnessUtils.Flash(Colors.Black, 0.2f);
            }

            Ui.Decorate(new MenuButtonDecorator());
            if (LeftAlignedButton.Start(Localisation.Get("main-menu-settings")))
            {
                Game.Scene = SettingsScene.Load(Game.Main);
                MadnessUtils.Flash(Colors.Black, 0.2f);
            }

            Ui.Decorate(new MenuButtonDecorator());
            if (LeftAlignedButton.Start(Localisation.Get("main-menu-credits-info")))
            {
                Game.Scene = InformationScene.Load(Game.Main);
                MadnessUtils.Flash(Colors.Black, 0.2f);
            }

            Ui.Spacer(30);

            Ui.Decorate(new MenuButtonDecorator());
            if (LeftAlignedButton.Start(Localisation.Get("main-menu-quit")))
            {
                Game.Window.IsVisible = false;
                Game.Stop();
            }

            Ui.Theme.Reset();
        }
        Ui.Layout.DefaultConstraints.Clear();
        Ui.End();
    }
}
