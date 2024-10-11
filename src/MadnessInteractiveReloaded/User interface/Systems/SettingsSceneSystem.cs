using System;
using System.Linq;
using Walgelijk;
using Walgelijk.Onion;
using Walgelijk.Onion.Controls;
using Walgelijk.Localisation;
using System.Runtime.CompilerServices;
using Walgelijk.SimpleDrawing;
using System.Collections.Generic;

using AspectRatioBehaviour = Walgelijk.Onion.Layout.AspectRatio.Behaviour;
using Walgelijk.AssetManager;

namespace MIR;

/// <summary>
/// Scene for manging the settings menu.
/// </summary>
public class SettingsSceneSystem : Walgelijk.System
{
    // TODO this stuff should really be in a component

    private static int selectedLanguageIndex = 0;
    private static int sliderInterval;
    private readonly static string[] languages;
    private readonly static Dictionary<SettingTab, (string, Action)[]> settingsScreens;

    private SettingTab tab;
    private static Settings settings => UserData.Instances.Settings;

    private readonly FixedIntervalDistributor fixedInterval = new()
    {
        Rate = 20
    };

    static SettingsSceneSystem()
    {
        var s = Registries.Languages.GetAllValues().ToArray();
        languages = s.Select(static b => b.Culture.Name).ToArray();

        settingsScreens = new()
        {
            { SettingTab.General, [
                ("settings-language", () => {
                    Ui.Layout.FitContainer(1,1,false);
                    int selectedLanguageIndex = Array.IndexOf(languages, settings.General.Language);
                    if (Ui.Dropdown(s, ref selectedLanguageIndex))
                    {
                        settings.General.Language = languages[selectedLanguageIndex];
                        settings.Apply();
                    }
                }),
                ("settings-screenshake", () => {
                    Ui.Layout.FitContainer(1,1,false);
                     if (Ui.FloatSlider(ref settings.General.Screenshake, Direction.Horizontal, (0,2), 0.1f, "{0:P0}"))
                        settings.Apply();
                })
            ]},

            { SettingTab.Video, [
                ("settings-fps-limit", () => {
                    const int maxFps = 301;
                    bool isUncapped = settings.Video.FramerateCap == 0;
                    int v = isUncapped ? maxFps : settings.Video.FramerateCap;
                    Ui.Layout.FitContainer(1,1,false);
                    if (Ui.IntSlider(ref v, Direction.Horizontal, new MinMax<int>(24, maxFps), label: isUncapped ? "Uncapped" : "{0} FPS"))
                    {
                        settings.Video.FramerateCap = (int)(v == maxFps ? 0 : v);
                        settings.Apply();
                    }
                }),
                ("settings-window-display-mode", () => {
                    Ui.Layout.FitContainer(1,1,false);
                    if (Ui.EnumDropdown(ref settings.Video.WindowType))
                        settings.Apply();
                }),   
                ("settings-vsync", () => {
                    Ui.Layout.FitContainer(1,1,false).AspectRatio(1, AspectRatioBehaviour.Shrink);
                    if (Controls.Toggle.Start(ref settings.Video.Vsync))
                        settings.Apply();
                })  ,
                ("settings-stamp-ragdolls", () => {
                    Ui.Layout.FitContainer(1, 1, false).AspectRatio(1, AspectRatioBehaviour.Shrink);
                    if (Controls.Toggle.Start(ref settings.Video.StampRagdolls))
                        settings.Apply();
                })
            ]},

            { SettingTab.Audio, [
                ("settings-master-volume", () => {
                    Ui.Layout.FitContainer(1,1,false);
                    if (Ui.FloatSlider(ref settings.Audio.MasterVolume, Direction.Horizontal, (0,1), 0.01f, "{0:P0}"))
                        settings.Apply();
                }),
                ("settings-music-volume", () => {
                    Ui.Layout.FitContainer(1,1,false);
                    if (Ui.FloatSlider(ref settings.Audio.MusicVolume, Direction.Horizontal, (0,1), 0.01f,  "{0:P0}"))
                        settings.Apply();
                }),
                ("settings-sfx-volume", () => {
                    Ui.Layout.FitContainer(1,1,false);
                    if (Ui.FloatSlider(ref settings.Audio.SfxVolume, Direction.Horizontal, (0,1), 0.01f,  "{0:P0}"))
                    {
                        settings.Apply();
                        for (int i = 0; i < sliderInterval; i++)
                            Game.Main.AudioRenderer.PlayOnce(Utilities.PickRandom(Sounds.NearMiss), track: AudioTracks.SoundEffects);
                    }
                }),
                ("settings-ui-volume", () => {
                    Ui.Layout.FitContainer(1,1,false);
                    if (Ui.FloatSlider(ref settings.Audio.UiVolume, Direction.Horizontal, (0,1), 0.01f,  "{0:P0}"))
                    {
                        settings.Apply();
                        for (int i = 0; i < sliderInterval; i++)
                            Game.Main.AudioRenderer.PlayOnce(Sounds.UiPress, track: AudioTracks.UserInterface);
                    }
                }),
            ]},

            { SettingTab.Input, [
                ("settings-reset-controls", () => {
                    Ui.Layout.FitContainer(1,1,false);
                    if (Ui.Button(Localisation.Get("reset")))
                        ResetControls();
                }),
            ]},
        };
    }

    private static void ResetControls()
    {
        var s = Game.Main.Scene;
        s.AttachComponent(s.CreateEntity(), new ConfirmationDialogComponent(
            Localisation.Get("settings-reset-controls"),
            Localisation.Get("are-you-sure"),
            () =>
            {
                settings.Controls = new();
                settings.Apply();
            }));
    }

    public override void Update()
    {
        fixedInterval.Rate = 8;
        sliderInterval = fixedInterval.CalculateCycleCount(Time.DeltaTimeUnscaled);

        Draw.Reset();
        Draw.ScreenSpace = true;
        MenuUiUtils.DrawBackground(Scene, 1);
        MenuUiUtils.DrawLogo(Scene, 1, 0);
        Ui.Theme.Padding(10).Foreground(new Appearance(Colors.White.WithAlpha(0.9f),
            Assets.Load<Texture>("textures/border-top-bottom.png").Value, ImageMode.Slice)).Once();
        Ui.Layout.FitContainer(0.4f, 1).MinWidth(500).StickLeft().Scale(0, -300 / Onion.GlobalScale).StickBottom();
        Ui.StartGroup(true);
        {
            Ui.Layout.FitContainer(1, 1, true).StickLeft().StickTop().Scale(0, -60);
            Ui.StartGroup(false);
            {
                DrawSettingsUi(Game, ref tab);
            }
            Ui.End();

            // back button
            if (MenuUiUtils.BackButton())
                Game.Scene = MainMenuScene.Load(Game);
        }
        Ui.End();
    }

    /// <summary>
    /// Renders the settings UI.<br></br> 
    /// Useful for rendering it outside of the <see cref="SettingsSceneSystem"/> like in the <see cref="PauseSystem"/>
    /// </summary>
    public static void DrawSettingsUi(Game game, ref SettingTab tab, [CallerLineNumber] int identity = 0)
    {
        const int tabWidth = 90;

        var container = Onion.Tree.CurrentNode!.GetInstance().Rects.ComputedGlobal;

        var scene = game.Scene;

        Ui.Layout.Width(tabWidth).FitHeight(false).VerticalLayout()/*.Overflow(false, false)*/;
        Ui.Theme.Padding(10);
        Ui.StartGroup(false);
        {
            Onion.Tree.CurrentNode!.GetInstance().Rects.DrawBounds = container; // TODO why is this necessary? engine bug?

            // TODO i genuinely don't know if this invokes GC. doesn't seem to. should cache it though.
            foreach (var s in Enum.GetValues<SettingTab>())
            {
                if (tab == s)
                    Ui.Decorate(new CrosshairDecorator());
                Ui.Decorate(new FancyButtonDecorator());
                Ui.Layout.FitWidth().AspectRatio(1).CenterHorizontal();
                if (Ui.ImageButton(GetTextureForTab(s), ImageContainmentMode.Stretch, identity: (int)s))
                    tab = s;
            }
        }
        Ui.End();
        Ui.Theme.Pop();

        Ui.Layout.FitContainer(1, 1, false).Scale(-tabWidth, 0).StickRight(false);
        Ui.StartGroup(false);
        {
            Ui.Layout.FitContainer(1, 1, false).Scale(-10, -10).Center().VerticalLayout().Overflow(false, true);
            Ui.StartScrollView(false);
            {
                Ui.Layout.FitWidth().Height(50);
                Ui.Theme.FontSize(24).Once();
                Ui.TextRect(GetTitleForTab(tab), HorizontalTextAlign.Left, VerticalTextAlign.Middle);

                int i = 0;
                foreach (var setting in settingsScreens[tab])
                {
                    Ui.Layout.FitWidth(false).CenterHorizontal().Height(40);
                    Ui.StartGroup(false, identity: i++);
                    {
                        Ui.Layout.FitContainer(0.5f, 1, false);
                        Ui.TextRect(Localisation.Get(setting.Item1), HorizontalTextAlign.Left, VerticalTextAlign.Middle);

                        Ui.Layout.FitContainer(0.5f, 1, false).StickRight(false);
                        Ui.StartGroup(false);
                        {
                            setting.Item2();
                        }
                        Ui.End();
                    }
                    Ui.End();
                }

                if (tab is SettingTab.Input)
                {
                    Ui.Spacer(16);

                    i = 0;
                    foreach (var action in settings.Controls.InputMap.Keys)
                    {
                        Ui.Layout.FitWidth(false).CenterHorizontal().Height(40);
                        Ui.StartGroup(false, identity: i++);
                        {
                            Ui.Layout.FitContainer(0.5f, 1, false);
                            Ui.TextRect(action.GetDisplayName(), HorizontalTextAlign.Left, VerticalTextAlign.Middle);

                            var input = settings.Controls.InputMap[action];
                            Ui.Layout.FitContainer(0.5f, 1, false).StickRight(false);
                            if (settings.Controls.InputMap.Any(a => a.Key != action && a.Value.Equals(input)))
                                if (game.State.Time % 1 > 0.5f)
                                    Ui.Theme.Text(Colors.Red).Once();

                            if (Controls.InputSwitchButton.Start(ref input))
                            {
                                settings.Controls.InputMap[action] = input;
                                settings.Apply();
                            }
                        }
                        Ui.End();
                    }
                }
            }
            Ui.End();
        }
        Ui.End();
    }

    private static Texture GetTextureForTab(SettingTab s)
    {
        return s switch
        {
            SettingTab.Video => Assets.Load<Texture>("textures/ui/settings/video_icon.png"),
            SettingTab.Audio => Assets.Load<Texture>("textures/ui/settings/sound_icon.png"),
            SettingTab.Input => Assets.Load<Texture>("textures/ui/settings/controls_icon.png"),
            _ => Assets.Load<Texture>("textures/ui/settings/gameplay_icon.png"),
        };
    }

    private static string GetTitleForTab(SettingTab s)
    {
        return s switch
        {
            SettingTab.Video => Localisation.Get("settings-video-header"),
            SettingTab.Audio => Localisation.Get("settings-audio-header"),
            SettingTab.Input => Localisation.Get("settings-input-header"),
            _ => Localisation.Get("settings-general-header"),
        };
    }
}

public enum SettingTab
{
    General,
    Video,
    Audio,
    Input
}