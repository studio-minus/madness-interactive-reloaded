using Walgelijk.Onion;
using Walgelijk.SimpleDrawing;
using Walgelijk;
using Walgelijk.Localisation;
using System.Numerics;
using System.Linq;
using Walgelijk.Onion.Controls;
using Walgelijk.AssetManager;

namespace MIR;

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

