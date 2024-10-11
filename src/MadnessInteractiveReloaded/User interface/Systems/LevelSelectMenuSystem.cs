using Walgelijk.Onion;
using Walgelijk.SimpleDrawing;
using Walgelijk;
using Walgelijk.Localisation;
using System.Numerics;
using System.Linq;
using Walgelijk.Onion.Controls;
using Walgelijk.AssetManager;
using System.Runtime.CompilerServices;
using Walgelijk.Onion.Layout;
using System.Reflection.Emit;

namespace MIR;
//🥝 🎈
public class ImprobabilityDiskSelectMenuSystem : Walgelijk.System
{
    private ImprobabilityDisk? hoverModifier = null;

    public override void Update()
    {
        MenuUiUtils.StartFullMenuPanel(Scene);
        {
            ModifierSelectGrid();
        }
        Ui.End();

        if (MenuUiUtils.BackButton())
            Game.Scene = MainMenuScene.Load(Game);
    }

    private void ModifierSelectGrid()
    {
        Ui.Layout.FitContainer(0.8f, 1, false).Scale(-10, 0).Move(10, 0);
        Ui.Theme.Foreground(new Appearance(Colors.White.WithAlpha(1), Assets.Load<Texture>("textures/right-border.png").Value, ImageMode.Slice)).Once();
        Ui.StartGroup(true); // modifier grid container
        {
            Ui.Layout.FitWidth().StickLeft().StickTop().Height(60);
            Ui.StartGroup(false);
            {
                Ui.Layout.FitContainer(0.5f, 1, false);
                Ui.Theme.FontSize(40).Once();
                Ui.TextRect("Improbability Disks", HorizontalTextAlign.Left, VerticalTextAlign.Middle);
            }
            Ui.End();

            var levelButtonSize = new Vector2(180);

            Ui.Layout.FitContainer().Scale(-8, -60).StickBottom().StickLeft();
            Ui.StartScrollView(); // modifier grid
            {
                hoverModifier = null;

                const int gap = 5;
                float w = Onion.Tree.CurrentNode!.GetInstance().Rects.ComputedGlobal.Width - 20;

                int columnCount = (int)(w / levelButtonSize.X);

                float scaling = w / (columnCount * levelButtonSize.X);

                int bw = (int)(levelButtonSize.X * scaling);
                int bh = (int)(levelButtonSize.Y * scaling);

                int x = 0;
                int y = 0;
                int i = 0;
                foreach (var p in ImprobabilityDisks.All)
                {
                    bool incompatible = ImprobabilityDisks.IsIncompatibleWithEnabled(p.Key);
                    bool unlocked = ImprobabilityDisks.IsUnlocked(p.Key);

                    bool enabled = p.Value.Enabled;
                    if (!incompatible)
                        Ui.Decorate(new FancyButtonDecorator());
                    Ui.Animation.SetDuration(0);
                    Ui.Theme.OutlineWidth(enabled ? 4 : 0).FontSize(18).Once();
                    Ui.Layout.Size(bw - gap, bh - gap).Move(x * (bw + gap / 2), y * (bh + gap / 2));
                    if (ModifierButton.Start(p.Key, identity: i))
                    {
                        if (incompatible || !unlocked)
                            Audio.PlayOnce(Sounds.UiBad);
                        else
                        {
                            p.Value.Enabled = !p.Value.Enabled;
                            if (p.Value.Enabled)
                            {
                                var a = Utilities.PickRandom(Assets.EnumerateFolder("sounds/ui/modifiers/insert"));
                                Audio.PlayOnce(SoundCache.Instance.LoadUISoundEffect(Assets.Load<FixedAudioData>(a)));
                            }
                            else
                                Audio.PlayOnce(SoundCache.Instance.LoadUISoundEffect(Assets.Load<FixedAudioData>("sounds/ui/modifiers/eject.wav")));
                        }
                    }

                    if (unlocked && Onion.Tree.LastNode.GetInstance().IsHover)
                        hoverModifier = p.Value;

                    x++;
                    if (x == columnCount)
                    {
                        x = 0;
                        y++;
                    }

                    i++;
                }
            }
            Ui.End();
        }
        Ui.End();

        Ui.Layout.FitContainer(0.2f, 1, false).StickRight(false).VerticalLayout().Scale(0, -60);
        Ui.StartGroup(false); // level stat panel
        {
            if (hoverModifier != null)
            {
                const int h = 30;
                Ui.Theme.Font(Fonts.CascadiaMono).FontSize(16).Text(Colors.White).Push();
                // TODO cache strings
                // TODO make this look not shit

                Ui.Layout.FitWidth().CenterHorizontal().Height(h);
                Ui.Theme.FontSize(18).Text(Colors.Red).Once();
                Ui.TextRect($"{hoverModifier.DisplayName}.dll", HorizontalTextAlign.Left, VerticalTextAlign.Middle);

                Ui.Layout.FitWidth().PreferredSize().CenterHorizontal();
                Ui.TextRect(Localisation.Get(hoverModifier.Description), HorizontalTextAlign.Left, VerticalTextAlign.Middle);

                if (hoverModifier.AbilityDescriptors.Length == 0)
                {
                    Ui.Spacer(10);
                    Ui.Theme.Text(Colors.White.WithAlpha(0.5f));
                    Ui.Layout.FitWidth().Scale(-10, 0).PreferredSize().CenterHorizontal();
                    Ui.TextRect(Localisation.Get("improbability-no-ability"), HorizontalTextAlign.Left, VerticalTextAlign.Top);
                }
                else
                {
                    int i = 0;
                    foreach (var item in hoverModifier.AbilityDescriptors)
                    {
                        Ui.Spacer(10, identity: i++);
                        Ui.Layout.FitWidth().Scale(-10, 0).CenterHorizontal().PreferredSize();
                        AbilityViewControl.Start(item, identity: i++);
                    }
                }

                Ui.Theme.Pop();
            }
        }
        Ui.End();
    }
}

public readonly struct AbilityViewControl(AbilityDescriptor AbilityDescriptor) : IControl
{
    public static void Start(AbilityDescriptor desc, int identity = 0, [CallerLineNumber] int site = 0)
    {
        var (instance, node) = Onion.Tree.Start(IdGen.Create(nameof(AbilityViewControl).GetHashCode(), identity, site), new AbilityViewControl(desc));
        instance.RenderFocusBox = false;
        Onion.Tree.End();
    }

    public void OnAdd(in ControlParams p)
    {
    }

    public void OnStart(in ControlParams p)
    {
        p.Instance.Rects.Local = new Rect(0, 0, 1, 1);
        p.Instance.CaptureFlags = CaptureFlags.None;
        p.Instance.Rects.Raycast = null;
        p.Instance.Rects.DrawBounds = null;
    }

    public void OnProcess(in ControlParams p)
    {
        p.Instance.Rects.Rendered = p.Instance.Rects.ComputedGlobal;
        p.Instance.CaptureFlags = CaptureFlags.Hover;
        p.Instance.Rects.Raycast = p.Instance.Rects.Rendered;
        p.Instance.Rects.DrawBounds = p.Instance.Rects.Rendered;
    }

    public void OnRender(in ControlParams p)
    {
        (ControlTree tree, LayoutQueue layout, Input input, GameState state, Node node, ControlInstance instance) = p;

        instance.Rects.Rendered = instance.Rects.ComputedGlobal;
        var t = node.GetAnimationTime();

        if (t <= float.Epsilon)
            return;

        var anim = instance.Animations;

        var fg = p.Theme.Foreground[ControlState.None];
        Draw.Colour = Colors.Transparent;
        Draw.OutlineColour = Colors.White.WithAlpha(0.5f);
        Draw.OutlineWidth = 4;

        anim.AnimateRect(ref instance.Rects.Rendered, t);
        anim.AnimateColour(ref Draw.Colour, t);
        anim.AnimateColour(ref Draw.OutlineColour, t);

        ref var r = ref instance.Rects.Rendered;

        Draw.Quad(r, 0, 10);

        Draw.Colour = Color.White;
        Draw.FontSize = 16;
        Draw.Text(AbilityDescriptor.Name, new Vector2(r.MinX + 10, r.MinY + 12), Vector2.One, textBoxWidth: r.Width - 20);

        Draw.Colour = Color.White.WithAlpha(0.8f);
        Draw.FontSize = 14;
        var h = Draw.CalculateTextHeight(AbilityDescriptor.Description, r.Width - 15) + 5;
        Draw.Text(AbilityDescriptor.Description, new Vector2(r.MinX + 10, r.MinY + 5 + 32), Vector2.One, textBoxWidth: r.Width - 20);

        instance.PreferredHeight = h + 32 + 10;
    }

    public void OnEnd(in ControlParams p) { }

    public void OnRemove(in ControlParams p) { }
}

public class LevelSelectMenuSystem : Walgelijk.System
{
    private enum Screen
    {
        LevelSelect,
        Modifiers
    }

    // TODO put in component lol
    private string? selectedLevel;
    private Screen currentScreen = Screen.LevelSelect;

    public override void OnActivate()
    {
        currentScreen = Screen.LevelSelect;
    }

    public override void Update()
    {
        if (CampaignProgress.CurrentCampaign == null)
            CampaignProgress.SetCampaign(Registries.Campaigns["employee_of_the_month"]);

        var campaign = CampaignProgress.CurrentCampaign;
        if (campaign == null)
            return;

        if (selectedLevel != null && !campaign.Levels.Contains(selectedLevel)) // TODO maybe this should be cached or something
            selectedLevel = null;

        MenuUiUtils.StartFullMenuPanel(Scene);
        {
            LevelSelectGrid(campaign);
        }
        Ui.End();

        if (MenuUiUtils.BackButton())
            Game.Scene = CampaignMenuScene.Load(Game);
    }

    private void LevelSelectGrid(Campaign campaign)
    {
        Ui.Animation.DefaultDurationSeconds = 0.1f;
        Ui.Layout.FitContainer(0.8f, 1, false).Move(10, 0).Scale(-15, 0);
        Ui.Theme.Foreground(new Appearance(Colors.White.WithAlpha(0.9f), Assets.Load<Texture>("textures/right-border.png").Value, ImageMode.Slice)).Once();
        Ui.StartGroup(true); // level grid container
        {
            Ui.Layout.FitWidth().StickLeft().StickTop().Height(60);
            Ui.StartGroup(false);
            {
                Ui.Layout.FitContainer(0.2f, 1, false);
                Ui.Theme.FontSize(40).Once();
                Ui.TextRect("Levels", HorizontalTextAlign.Left, VerticalTextAlign.Middle);
                Ui.Layout.FitContainer(0.8f, 1, false).StickRight(false);
                Ui.Theme.Text(Colors.Red).FontSize(24).Once();
                Ui.TextRect(campaign.Name, HorizontalTextAlign.Left, VerticalTextAlign.Middle);
            }
            Ui.End();

            var levelButtonSize = new Vector2(260, 200) * 0.8f;

            Ui.Layout.FitContainer().Scale(-5, -60).StickBottom().StickLeft().Overflow(false, true);
            Ui.StartScrollView(); // level grid
            {
                const int gap = 10;
                float w = Onion.Tree.CurrentNode!.GetInstance().Rects.GetInnerContentRect().Width - 20;

                int columnCount = (int)(w / levelButtonSize.X);

                float scaling = w / (columnCount * levelButtonSize.X);

                int bw = (int)(levelButtonSize.X * scaling);
                int bh = (int)(levelButtonSize.Y * scaling);

                int x = 0;
                int y = 0;

                int i = 0;
                foreach (var key in campaign.Levels)
                {
                    if (!Registries.Levels.TryGet(key, out var l))
                        continue;

                    bool selected = key == selectedLevel;
                    var locked = false;
                    if (CampaignProgress.TryGetCurrentStats(out var stats))
                        locked = stats.IsLevelLocked(key);

                    if (selected)
                        Ui.Decorate(new CrosshairDecorator());
                    if (!locked)
                        Ui.Decorate(new FancyButtonDecorator());
                    Ui.Animation.SetDuration(0);
                    Ui.Theme.OutlineWidth(selected ? 4 : 2).FontSize(18).Once();
                    Ui.Layout.Size(bw - gap, bh - gap).Move(x * (bw + gap / 2), y * (bh + gap / 2));
                    if (LevelButton.Start(key, i, identity: i))
                    {
                        if (locked)
                            Audio.PlayOnce(Sounds.UiBad);
                        else
                            selectedLevel = key;
                    }

                    x++;
                    if (x == columnCount)
                    {
                        x = 0;
                        y++;
                    }

                    i++;
                }
            }
            Ui.End();
        }
        Ui.End();

        if (selectedLevel != null)
        {
            LevelStats? lvlStats = null;
            bool s = CampaignProgress.TryGetCurrentStats(out var ss) && ss.ByLevel.TryGetValue(selectedLevel, out lvlStats);

            Ui.Layout.FitContainer(0.2f, 1, false).StickRight(false).VerticalLayout().Scale(0, -100);
            Ui.StartGroup(false); // level stat panel
            {
                const int h = 24;
                Ui.Theme.Font(Fonts.CascadiaMono).FontSize(16).Text(Colors.White).Push();
                // TODO cache strings
                // TODO make this look not shit

                var t = $"<color=#ff0000>{selectedLevel}";

                Draw.Font = Fonts.CascadiaMono;
                Draw.FontSize = 24;
                float titleHeight = Draw.CalculateTextHeight(t, Onion.Tree.CurrentNode!.GetInstance().Rects.ComputedGlobal.Width);

                Ui.Layout.FitWidth().CenterHorizontal().Height(titleHeight + h);
                Ui.Theme.FontSize(24).Once();
                Ui.TextRect(t, HorizontalTextAlign.Center, VerticalTextAlign.Middle);

                if (lvlStats != null)
                {
                    Ui.Layout.FitWidth().CenterHorizontal().Height(h);
                    Ui.TextRect(string.Format(Localisation.Get("frmt-attempts"), lvlStats.Attempts), HorizontalTextAlign.Left, VerticalTextAlign.Middle);

                    Ui.Layout.FitWidth().CenterHorizontal().Height(h);
                    Ui.TextRect(string.Format(Localisation.Get("frmt-kills"), lvlStats.Kills), HorizontalTextAlign.Left, VerticalTextAlign.Middle);

                    Ui.Layout.FitWidth().CenterHorizontal().Height(h);
                    Ui.TextRect(string.Format(Localisation.Get("frmt-deaths"), lvlStats.Deaths), HorizontalTextAlign.Left, VerticalTextAlign.Middle);

                    Ui.Layout.FitWidth().CenterHorizontal().Height(h);
                    Ui.TextRect(string.Format(Localisation.Get("frmt-spent-time"), lvlStats.TotalTimeSpent.ToString("hh\\hmm\\mss\\s")), HorizontalTextAlign.Left, VerticalTextAlign.Middle);
                }

                Ui.Theme.Pop();
            }
            Ui.End();

            Ui.Theme.FontSize(24).OutlineWidth(2).Padding(12).Once();
            Ui.Layout.FitContainer(0.2f, null).Scale(-12, 0).Height(60).StickBottom().StickRight();
            if (Ui.Button("Proceed"))
            {
                // TODO ensure we cant press this button while loading a campaign

                Game.Scene = LevelLoadingScene.Create(Game, Registries.Levels.Get(selectedLevel).Level, SceneCacheSettings.NoCache);
                MadnessUtils.Flash(Colors.Black, 0.2f);
            }
        }
    }
}

