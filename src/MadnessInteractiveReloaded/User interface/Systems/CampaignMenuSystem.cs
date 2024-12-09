using System.Linq;
using System.Numerics;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.Localisation;
using Walgelijk.Onion;
using Walgelijk.Onion.Controls;
using Walgelijk.SimpleDrawing;

namespace MIR;

public class CampaignMenuSystem : Walgelijk.System
{
    // TODO put in component
    public string? selectedCampaign;
    public Rect PlayerDrawRect;
    // TODO maybe use a shared rendertexure for this?
    public static RenderTexture PlayerDrawTarget = new(512, 512, flags: RenderTargetFlags.None);
    private Rect campaignBoxRect;
    private bool isLoadingCampaign = false;

    private static readonly MenuCharacterRenderer menuCharacterRenderer = new();

    public override void OnActivate()
    {
        isLoadingCampaign = false;
    }

    public override void Render()
    {
        if (!MadnessUtils.FindPlayer(Scene, out _, out var character))
            return;

        menuCharacterRenderer.HorizontalFlip = true;
        menuCharacterRenderer.Render(Window, PlayerDrawTarget, character);

    }

    private void ProcessPlayerCharacter()
    {
        if (!MadnessUtils.FindPlayer(Scene, out var player, out var character))
        {
            Level.CurrentLevel = null;
            player = Prefabs.CreatePlayer(Scene, default);
            character = Scene.GetComponentFrom<CharacterComponent>(player.Entity);
            character.AimTargetPosition = new Vector2(1000, 0);
        }

        if (Input.IsKeyReleased(Key.F5))
#if DEBUG
            character.NeedsLookUpdate = true;
#endif

        if (character.Positioning.IsFlipped)
        {
            character.Positioning.IsFlipped = false;
            character.NeedsLookUpdate = true;
        }

        character.Positioning.ShouldFeetFollowBody = false;
        character.Positioning.GlobalTarget = default;

        if (player != null)
            player.RespondToUserInput = false;

        if (!character.IsPlayingAnimation)
        {
            var next = Animations.CharacterCreationIdleAnimation;
            character.PlayAnimation(next, .5f);
        }
    }

    public override void Update()
    {
        ProcessPlayerCharacter();

        Draw.Reset();
        Draw.ScreenSpace = true;
        MenuUiUtils.DrawBackground(Scene, 1);
        MenuUiUtils.DrawLogo(Scene, 1, 0);

        // big boy in the center
        {
            var c = Window.Size * 0.5f;
            Draw.ResetMaterial();
            Draw.Colour = Colors.Red * 0.25f;
            Draw.BlendMode = BlendMode.Addition;
            Draw.TransformMatrix = Matrix3x2.CreateScale(-1, 1, c);
            Draw.Material = Materials.BlurDraw;
            Draw.Image(PlayerDrawTarget,
                new Rect(0, 0, Window.Width, Window.Height * 1.5f), ImageContainmentMode.Contain);
            Draw.ResetMaterial();
            Draw.ResetTransformation();
            Draw.BlendMode = BlendMode.AlphaBlend;
            Draw.Colour = Colors.White;
        }

        Ui.Theme.FontSize(35).Once();
        Ui.Layout.Width(400).Height(40).Move(13, 13).Move(0, 310 / Onion.GlobalScale);
        Ui.TextRect(Localisation.Get("main-menu-campaigns"), HorizontalTextAlign.Left, VerticalTextAlign.Top);

        Ui.Theme.Padding(10).ForegroundColor(Colors.Black.WithAlpha(0.5f)).OutlineWidth(1).Once();
        Ui.Layout.FitContainer().StickLeft().MaxWidth(400).Scale(0, -360 / Onion.GlobalScale).StickBottom().Scale(0, -50);
        Ui.StartGroup(true);
        {
            Ui.Layout.FitContainer(1, selectedCampaign == null ? 1 : 0.7f).Center().StickTop().VerticalLayout();
            Ui.Theme.Padding(10).ForegroundColor(Colors.Black.WithAlpha(0.5f)).Once();
            Ui.StartScrollView(false);
            int i = 0;
            foreach (var c in Registries.Campaigns.GetAllValues().OrderBy(static c => c.Order))
            {
                Ui.Layout.FitWidth().Height(40).CenterHorizontal();
                Ui.Theme.Padding(10).Once();
                if (selectedCampaign == c.Id)
                    Ui.Decorate(new CrosshairDecorator());
                Ui.Decorate(new FancyButtonDecorator());
                if (Ui.Button(c.Name, identity: i))
                {
                    if (selectedCampaign == c.Id)
                        selectedCampaign = null;
                    else
                    {
                        selectedCampaign = c.Id;
                        CampaignProgress.SetCampaign(c);
                    }

                    if (MadnessUtils.FindPlayer(Scene, out var player, out var character))
                    {
                        character.NeedsLookUpdate = true;

                        bool needsReset = false;

                        if (c.Look == null && character.Look != UserData.Instances.PlayerLook)
                            // requested look is none, but look isnt playerlook, so we reset
                            needsReset = true;

                        if (c.Look != null && Registries.Looks.TryGet(c.Look, out var requested) && character.Look != requested)
                            needsReset = true;

                        if (needsReset)
                            character.Delete(Scene);
                    }
                }
                i++;

                //Ui.ImageButton(Resources.Load<Texture>(c.Thumbnail, true), ImageContainmentMode.Cover, identity: c.Id.GetHashCode());
            }
            Ui.End();

            if (selectedCampaign != null && Registries.Campaigns.TryGet(selectedCampaign, out var sc) && sc != null)
            {
                Ui.Layout.FitContainer(1, 0.3f).Center().StickBottom();
                Ui.Theme.ForegroundColor(Colors.Black.WithAlpha(0.25f)).Once();
                Ui.StartScrollView(true);
                {
                    Ui.Layout.FitWidth().PreferredSize().StickLeft().StickTop();
                    Ui.Theme.Text(Colors.Red).FontSize(18).Once();
                    Ui.TextRect(sc.Name, HorizontalTextAlign.Left, VerticalTextAlign.Top);

                    Ui.Layout.FitWidth().PreferredSize().StickLeft().StickTop().Height(38);
                    Ui.Theme.Text(Colors.White.WithAlpha(0.5f)).FontSize(15).Once();
                    Ui.TextRect(sc.Author, HorizontalTextAlign.Left, VerticalTextAlign.Bottom);
                    Ui.Layout.FitWidth().PreferredSize().StickLeft().StickTop().Move(0, Onion.Tree.LastNode.GetInstance().Rects.ComputedGlobal.Height + 10);
                    Ui.TextRect(sc.Description, HorizontalTextAlign.Left, VerticalTextAlign.Top);
                }
                Ui.End();
            }
        }
        Ui.End();

        //Ui.Layout.Size(400, 512).MaxHeight(Window.Height / 2).StickRight().StickTop().Move(-10, -10);
        //Ui.Image(PlayerDrawTarget, ImageContainmentMode.Contain);
        //PlayerDrawRect = Onion.Tree.LastNode.GetInstance().Rects.ComputedGlobal;

        const float pedestalHeight = 70;
        var s = new Vector2(400, 512);
        s.Y = float.Min(Window.Height / 2, s.Y);
        Draw.Order = new RenderOrder(0, 1);

        PlayerDrawRect = MadnessUtils.FitImageRect(PlayerDrawTarget, campaignBoxRect.Translate(0, -campaignBoxRect.Height), ImageContainmentMode.Contain);
        PlayerDrawRect = PlayerDrawRect.Translate(0, campaignBoxRect.MinY - PlayerDrawRect.MaxY - pedestalHeight); // stick to top of box
        Draw.Image(PlayerDrawTarget, PlayerDrawRect.Translate(0, pedestalHeight * 0.5f), ImageContainmentMode.Stretch);

        if (Input.IsButtonPressed(MouseButton.Left) && PlayerDrawRect.ContainsPoint(Input.WindowMousePosition))
            if (MadnessUtils.FindPlayer(Scene, out var player, out var character))
                character.PlayAnimation(Utilities.PickRandom(Animations.Dancing));

        Draw.Order = default;
        var pedestal = Assets.Load<Texture>("textures/pedestal.png").Value;
        var r = PlayerDrawRect.Translate(0, pedestalHeight);
        r.MinY = r.MaxY - pedestalHeight;
        Draw.Reset();
        Draw.ScreenSpace = true;
        Draw.Image(pedestal, r, ImageContainmentMode.Contain);

        Ui.Layout.Size(400, 512).MaxHeight(Window.Height / 2).StickBottom().StickRight().Move(-10, -10);
        Ui.Theme.ForegroundColor(Colors.Black.WithAlpha(0.5f)).OutlineWidth(1).Once();
        Ui.StartGroup();
        {
            if (selectedCampaign != null && Registries.Campaigns.TryGet(selectedCampaign, out var campaign) && campaign != null)
            {
                if (CampaignProgress.TryGetCurrentStats(out var stats))
                {
                    Ui.Layout.FitContainer(1, 0.7f).StickLeft().StickTop();
                    Ui.Theme.BackgroundColor(Colors.Black).Once();
                    Ui.Decorate(new BackgroundDecorator());
                    Ui.Image(campaign.Thumbnail.Value, ImageContainmentMode.Contain);

                    Ui.Layout.FitContainer(1, 0.3f).StickBottom().StickLeft().Scale(0, -40);
                    Ui.StartScrollView(false);
                    {
                        Ui.Layout.FitContainer(0.5f, null).Height(90).StickLeft().StickTop().VerticalLayout();
                        Ui.StartGroup(false);
                        {
                            Ui.Layout.Height(40).FitWidth().StickLeft().StickTop();
                            Ui.Theme.FontSize(16).Once();
                            int a = int.Min(stats.LevelIndex, campaign.Levels.Length);
                            Ui.TextRect($"<color=#ff0000>{Localisation.Get("cmpgn-menu-completed-levels")}</color>\n{a} / {campaign.Levels.Length}", HorizontalTextAlign.Left, VerticalTextAlign.Middle);

                            Ui.Layout.Height(40).FitWidth().StickLeft().StickTop();
                            Ui.Theme.FontSize(16).Once();
                            Ui.TextRect($"<color=#ff0000>{Localisation.Get("cmpgn-menu-time-spent")}</color>\n{stats.TotalTimeSpent:hh\\:mm\\:ss}", HorizontalTextAlign.Left, VerticalTextAlign.Middle);
                        }
                        Ui.End();

                        Ui.Layout.FitContainer(0.5f, null).Height(90).StickRight().StickTop().VerticalLayout();
                        Ui.StartGroup(false);
                        {
                            Ui.Layout.Height(40).FitWidth().StickRight().StickTop();
                            Ui.Theme.FontSize(16).Once();
                            //Ui.TextRect($"<color=#ff0000>Kills</color>\n{stats.TotalKills}",
                            Ui.TextRect(string.Format(Localisation.Get("frmt-kills"), stats.TotalKills),
                                HorizontalTextAlign.Right, VerticalTextAlign.Middle);

                            Ui.Layout.Height(40).FitWidth().StickRight().StickTop();
                            Ui.Theme.FontSize(16).Once();
                            //Ui.TextRect($"<color=#ff0000>Deaths</color>\n{stats.TotalDeaths}", 
                            Ui.TextRect(string.Format(Localisation.Get("frmt-deaths"), stats.TotalDeaths),
                                HorizontalTextAlign.Right, VerticalTextAlign.Middle);
                        }
                        Ui.End();
                    }
                    Ui.End();

                    Ui.Layout.Size(130, 40).StickLeft().StickBottom().Order(1);
                    if (Ui.Button(Localisation.Get("cmpgn-menu-lvl-select")))
                    {
                        Game.Scene = LevelSelectionMenuScene.Load(Game);
                        MadnessUtils.Flash(Colors.Black, 0.2f);
                    }

                    var btnKey = stats.LevelIndex == 0 ?
                        "cmpgn-menu-play" :
                        (stats.LevelIndex >= campaign.Levels.Length ? "cmpgn-menu-restart" : "cmpgn-menu-continue");
                    Ui.Layout.Size(130, 40).StickRight().StickBottom().Order(1);
                    if (Ui.Button(Localisation.Get(btnKey)) && !isLoadingCampaign)
                    {
                        isLoadingCampaign = true; // prevent double click
                        var i = stats.LevelIndex;

                        if (i >= campaign.Levels.Length) // we completed the campaign, restart
                            i = 0;

                        Game.Scene = LevelLoadingScene.Create(Game, Registries.Levels.Get(campaign.Levels[i]).Level, SceneCacheSettings.NoCache);
                        MadnessUtils.Flash(Colors.Black, 0.2f);
                    }
                }
            }
            else
            {
                Ui.Layout.FitContainer().Center();
                Ui.TextRect(Localisation.Get("cmpgn-menu-select-campaign"),
                    HorizontalTextAlign.Center, VerticalTextAlign.Middle);
            }
        }
        Ui.End();
        campaignBoxRect = Onion.Tree.LastNode.GetInstance().Rects.ComputedGlobal;

        if (MenuUiUtils.BackButton())
            Game.Scene = MainMenuScene.Load(Game);
    }
}
