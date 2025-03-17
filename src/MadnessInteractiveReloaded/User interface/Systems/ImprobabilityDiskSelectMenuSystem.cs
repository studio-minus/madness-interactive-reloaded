using Walgelijk.Onion;
using Walgelijk.SimpleDrawing;
using Walgelijk;
using Walgelijk.Localisation;
using System.Numerics;
using Walgelijk.Onion.Controls;
using Walgelijk.AssetManager;

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
                Ui.TextRect(Localisation.Get("main-menu-improbability-disks"), HorizontalTextAlign.Left, VerticalTextAlign.Middle);
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
                                Audio.PlayOnce(SoundCache.Instance.LoadUISoundEffect(
                                    Assets.Load<FixedAudioData>(a)));
                            }
                            else
                                Audio.PlayOnce(SoundCache.Instance.LoadUISoundEffect(
                                    Assets.Load<FixedAudioData>("sounds/ui/modifiers/eject.wav")));
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

                Ui.Layout.FitWidth().CenterHorizontal().Height(300);
                Ui.Image(hoverModifier.Texture, ImageContainmentMode.Contain);

                Ui.Layout.FitWidth().CenterHorizontal().Height(h);
                Ui.Theme.FontSize(18).Text(Colors.Red).Once();
                Ui.TextRect($"{hoverModifier.DisplayName}.dll", HorizontalTextAlign.Left, VerticalTextAlign.Middle);

                Ui.Layout.FitWidth().PreferredSize().CenterHorizontal();
                Ui.TextRect(Localisation.Get(hoverModifier.Description), HorizontalTextAlign.Left, VerticalTextAlign.Middle);

                if (hoverModifier.AbilityDescriptors.Length == 0)
                {
                    //Ui.Spacer(10);
                    //Ui.Theme.Text(Colors.White.WithAlpha(0.5f));
                    //Ui.Layout.FitWidth().Scale(-10, 0).PreferredSize().CenterHorizontal();
                    //Ui.TextRect(Localisation.Get("improbability-no-ability"), HorizontalTextAlign.Left, VerticalTextAlign.Top);
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

