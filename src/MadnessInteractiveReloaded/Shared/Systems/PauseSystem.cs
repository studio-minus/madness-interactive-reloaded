using System.Numerics;
using Walgelijk;
using Walgelijk.Onion;
using Walgelijk.SimpleDrawing;
using Walgelijk.Localisation;
using Walgelijk.Onion.Animations;
using Walgelijk.AssetManager;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.ComponentModel;
using Walgelijk.Onion.Controls;

namespace MIR;

/// <summary>
/// System for pausing the game.<br></br>
/// See: <see cref="PauseComponent"/>.
/// </summary>
public class PauseSystem : Walgelijk.System
{
    // todo oh no stateful system waaaaaaa
    private bool pausedLvlMusic = false;
    private Sound? pauseMusic => PersistentSoundHandles.PauseMusic;

    public PauseSystem()
    {
        EnsurePauseMusic();
    }

    [MemberNotNull(nameof(pauseMusic))]
    private void EnsurePauseMusic()
    {
        PersistentSoundHandles.PauseMusic ??= SoundCache.Instance.LoadMusic(Assets.Load<StreamAudioData>("sounds/music/Lothyde/pause_menu.ogg"));
    }

    public override void Initialise()
    {
        Game.OnSceneChange.AddListener(_ =>
        {
            EnsurePauseMusic();
            Audio.Pause(pauseMusic);
        });
    }

    public override void Update()
    {
        if (!Scene.FindAnyComponent<PauseComponent>(out var pauseComponent))
            return;

        if (MadnessUtils.IsCutscenePlaying(Scene))
            return;

        EnsurePauseMusic();

        if (pauseComponent.Paused)
        {
            if (!pausedLvlMusic)
            {
                pausedLvlMusic = true;
                pauseMusic.Volume = 0.01f;
                pauseMusic.ForceUpdate();
                Audio.Play(pauseMusic, pauseMusic.Volume);

                if (PersistentSoundHandles.LevelMusic != null && Audio.IsPlaying(PersistentSoundHandles.LevelMusic))
                    Audio.Pause(PersistentSoundHandles.LevelMusic);
            }
        }
        else
        {
            if (pausedLvlMusic)
            {
                pausedLvlMusic = false;
                Audio.Pause(pauseMusic);

                if (PersistentSoundHandles.LevelMusic != null && !Audio.IsPlaying(PersistentSoundHandles.LevelMusic))
                    Audio.Play(PersistentSoundHandles.LevelMusic);
            }
        }

        pauseMusic.Volume = Utilities.SmoothApproach(pauseMusic.Volume, 1, 0.1f, Time.DeltaTime);
        pauseMusic.ForceUpdate();

        pauseComponent.TimeSinceChange += (pauseComponent.Paused ? Time.DeltaTimeUnscaled : -Time.DeltaTimeUnscaled) * 5f;
        var t = pauseComponent.TimeSinceChange = float.Clamp(pauseComponent.TimeSinceChange, 0, 1);

        if (Input.IsKeyPressed(Key.Escape) && !MadnessUtils.EditingInExperimentMode(Scene) && !Game.Console.IsActive)
        {
            pauseComponent.Paused = !pauseComponent.Paused;
            if (!pauseComponent.Paused)
            {
                pauseComponent.SettingsMenuOpen = false;
                pauseComponent.PauseMenuSize = default;
            }
        }

        float progress = pauseComponent.AnimationProgress = Easings.Quad.Out(t);

        if (progress > float.Epsilon)
        {
            var exp = Scene.FindAnyComponent<ExperimentModeComponent>(out _);

            Draw.Reset();
            Draw.ScreenSpace = true;
            Draw.Order = RenderOrders.UserInterface;

            Draw.Colour = Vector4.Lerp(Colors.Red, Colors.White, Utilities.MapRange(0, 1, 1, 0.5f, t));
            Draw.BlendMode = BlendMode.Multiply;
            Draw.Quad(new Rect(0, 0, Window.Width, Window.Height));

            Draw.BlendMode = BlendMode.AlphaBlend;
            float extend = 128 * progress;
            Draw.Colour = Colors.Black;
            Draw.Quad(new Rect(0, 0, Window.Width, extend));
            Draw.Quad(new Rect(0, Window.Height - extend, Window.Width, Window.Height));
            Draw.Quad(new Rect(0, 0, extend / 2, Window.Height));

            if (ImprobabilityDisks.All.Values.Any(static d => d.Enabled))
            {
                float e = 128;
                Draw.Quad(new Rect(Window.Width - extend, 0, Window.Width, Window.Height));
                int i = 0;
                foreach (var item in ImprobabilityDisks.All.Values)
                {
                    if (item.Enabled)
                    {
                        float w = e - 20;
                        var r = new Rect(new Vector2(

                            Window.Width - e * progress / 2,
                            e * 1.5f - 10 + w * i),

                            new Vector2(w));

                        Draw.Colour = Colors.White;
                        Draw.Texture = item.Texture;
                        Draw.Quad(r);
                        i++;
                    }
                }
                Draw.ResetTexture();
            }
            else
                Draw.Quad(new Rect(Window.Width - extend / 2, 0, Window.Width, Window.Height));

            if (!exp && CampaignProgress.TryGetCurrentStats(out var stats))
            {
                Draw.Font = Fonts.Impact;
                Draw.Colour = Colors.White;
                Draw.FontSize = 48;

                // TODO cache these on pause menu open

                Draw.Text($"{stats.TotalKills} KILLS", new Vector2(Window.Width / 2 - 140, Window.Height - extend / 1.5f), Vector2.One, HorizontalTextAlign.Right, VerticalTextAlign.Top);

                Draw.Text($"{stats.TotalDeaths} DEATHS", new Vector2(Window.Width / 2 + 140, Window.Height - extend / 1.5f), Vector2.One, HorizontalTextAlign.Left, VerticalTextAlign.Top);

                Draw.Text($"{stats.TotalTimeSpent:h\\:mm\\:ss}", new Vector2(Window.Width / 2, extend / 1.5f), Vector2.One, HorizontalTextAlign.Center, VerticalTextAlign.Bottom);
            }

            var buttonHeight = 40;

            const float animSpeed = 8;
            if (pauseComponent.SettingsMenuOpen)
                pauseComponent.PauseMenuSizeAnimationTime += animSpeed * Time.DeltaTime;
            else
                pauseComponent.PauseMenuSizeAnimationTime -= animSpeed * Time.DeltaTime;
            pauseComponent.PauseMenuSizeAnimationTime = float.Clamp(pauseComponent.PauseMenuSizeAnimationTime, 0, 1);
            float sizeTime = Easings.Quad.InOut(pauseComponent.PauseMenuSizeAnimationTime);

            // Menu
            if (pauseComponent.Paused)
            {
                var h = pauseComponent.PauseMenuSize.Y = float.Lerp(230, 400, sizeTime);
                var w = pauseComponent.PauseMenuSize.X = float.Lerp(350, 600, sizeTime);

                Ui.Animation.SetDuration(1 / 5f);
                Ui.Animate(new ShrinkAnimation());
                Ui.Animate(new FadeAnimation());
                Ui.Layout.Size(w, h).Center();
                Ui.Theme.Padding(5).ForegroundColor(Colors.White).ForegroundTexture(
                    Assets.Load<Texture>("textures/border-top-bottom.png").Value, ImageMode.Slice).Once();
                Ui.StartGroup();
                {
                    if (pauseComponent.SettingsMenuOpen)
                    {
                        Ui.Layout.FitContainer(1, 1, true).StickLeft().StickTop().Scale(0, -60);
                        Ui.Animation.SetDuration(0);
                        Ui.StartGroup(false);
                        {
                            SettingsSceneSystem.DrawSettingsUi(Game, ref pauseComponent.SettingTab, 0);
                        }
                        Ui.End();
                        
                        Ui.Layout.Size(120, buttonHeight).StickLeft().StickBottom().Move(2, -2);
                        Ui.Animation.SetDuration(0);
                        if (Ui.Button(Localisation.Get("back")))
                            pauseComponent.SettingsMenuOpen = false;
                    }
                    else
                    {
                        Ui.Theme.Text(Colors.Red).FontSize(40).Font(Fonts.Impact).Once();
                        Ui.Layout.PreferredSize().MinHeight(40).FitWidth(false).CenterHorizontal().Move(0, 10);
                        Ui.TextRect(Localisation.Get("pause-menu-paused"), HorizontalTextAlign.Center, VerticalTextAlign.Middle);

                        Ui.Theme.Text(new(Colors.Red, Colors.White, Colors.Red)).Font(Fonts.Oxanium).FontSize(24).Foreground((Appearance)Colors.Transparent).OutlineWidth(0);
                        Ui.Theme.Foreground(default);

                        Onion.Layout.FitWidth().CenterHorizontal().Height(buttonHeight).Move(0, 60);
                        if (Ui.ClickButton(Localisation.Get("pause-menu-resume")))
                            pauseComponent.Paused = false;

                        Onion.Layout.FitWidth().CenterHorizontal().Height(buttonHeight).Move(0, 60 + buttonHeight);
                        if (Ui.ClickButton(Localisation.Get("main-menu-settings")))
                            pauseComponent.SettingsMenuOpen = true;

                        //if (!exp)
                        //{
                        //    Onion.Layout.FitWidth().CenterHorizontal().Height(buttonHeight).Move(0, 60 + buttonHeight * 2);
                        //    if (Ui.ClickButton(Localisation.Get("pause-menu-retry")))
                        //    {
                        //        // TODO
                        //    }
                        //}

                        Onion.Layout.FitWidth().CenterHorizontal().Height(buttonHeight).StickBottom().Move(0, -10);
                        if (Ui.ClickButton(Localisation.Get("main-menu-quit")))
                        {
                            Audio.StopAll();
                            MadnessUtils.TransitionScene(static game => MainMenuScene.Load(game));
                        }
                    }

                    Ui.Theme.Reset();
                }
                Ui.End();
            }
        }
    }
}
