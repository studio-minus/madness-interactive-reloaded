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

    private static readonly MenuCharacterRenderer menuCharacterRenderer = new();

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

        Ui.Theme.FontSize(35).Once();
        Ui.Layout.Width(400).Height(40).Move(13, 13).Move(0, 310 / Onion.GlobalScale);
        Ui.TextRect("Campaigns", HorizontalTextAlign.Left, VerticalTextAlign.Top);

        Ui.Theme.Padding(10).ForegroundColor(Colors.Black.WithAlpha(0.5f)).OutlineWidth(1).Once();
        Ui.Layout.FitContainer().StickLeft().MaxWidth(400).Scale(0, -360 / Onion.GlobalScale).StickBottom().Scale(0, -50);
        Ui.StartGroup(true);
        {
            Ui.Layout.FitContainer(1, selectedCampaign == null ? 1 : 0.7f).Center().StickTop().VerticalLayout();
            Ui.Theme.Padding(10).ForegroundColor(Colors.Black.WithAlpha(0.5f)).Once();
            Ui.StartScrollView(false);
            int i = 0;
            foreach (var c in Registries.Campaigns.GetAllValues())
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
                        bool needsLookUpdate = false;

                        if (c.Look == null && character.Look != UserData.Instances.PlayerLook) // requested look is none, but look isnt playerlook, so we reset
                            needsLookUpdate = true;

                        if (c.Look != null && Registries.Looks.TryGet(c.Look, out var requested) && character.Look != requested)
                            needsLookUpdate = true;

                        if (needsLookUpdate)
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
                    Ui.Theme.Text(Colors.White.WithAlpha(0.5f)).Once();
                    Ui.TextRect($"<color=#ff0000>{sc.Name}</color>\n - {sc.Author}", HorizontalTextAlign.Left, VerticalTextAlign.Top);

                    Ui.Layout.FitWidth().PreferredSize().StickLeft().StickTop().Move(0, Onion.Tree.LastNode.GetInstance().Rects.ComputedGlobal.Height);
                    Ui.TextRect(sc.Description, HorizontalTextAlign.Left, VerticalTextAlign.Top);
                }
                Ui.End();
            }
        }
        Ui.End();

        //Ui.Layout.Size(400, 512).MaxHeight(Window.Height / 2).StickRight().StickTop().Move(-10, -10);
        //Ui.Image(PlayerDrawTarget, ImageContainmentMode.Contain);
        //PlayerDrawRect = Onion.Tree.LastNode.GetInstance().Rects.ComputedGlobal;

        var s = new Vector2(400, 512);
        s.Y = float.Min(Window.Height / 2, s.Y);
        Draw.Order = new RenderOrder(0, 1);
        PlayerDrawRect = Draw.Image(PlayerDrawTarget, new Rect(0, 0, s.X, s.Y).Translate(Window.Width - s.X, 0).Translate(-10, -10), ImageContainmentMode.Contain);

        if (Input.IsButtonPressed(MouseButton.Left) && PlayerDrawRect.ContainsPoint(Input.WindowMousePosition))
        {
            if (MadnessUtils.FindPlayer(Scene, out var player, out var character))
            {
                character.PlayAnimation(Utilities.PickRandom(Animations.Dancing));
            }
        }

        Draw.Order = new RenderOrder(0, 0);
        var pedestal = Assets.Load<Texture>("textures/pedestal.png").Value;
        var r = PlayerDrawRect;
        r.MinY = r.MaxY - pedestal.Height;
        r = r.Scale(0.7f, 1);
        r = r.Translate(0, 70);
        Draw.Reset();
        Draw.ScreenSpace = true;
        Draw.Image(pedestal, r, ImageContainmentMode.Contain);

        Ui.Layout.Size(400, 512).MaxHeight(Window.Height / 2).StickBottom().StickRight().Move(-10, -10);
        Ui.Theme.ForegroundColor(Colors.Black.WithAlpha(0.5f)).OutlineWidth(1).Once();
        Ui.StartGroup();
        {
            if (selectedCampaign != null && Registries.Campaigns.TryGet(selectedCampaign, out var sc) && sc != null)
            {
                if (CampaignProgress.TryGetCurrentStats(out var stats))
                {
                    Ui.Layout.FitContainer(1, 0.7f).StickLeft().StickTop();
                    Ui.Image(sc.Thumbnail.Value, ImageContainmentMode.Cover);

                    Ui.Layout.FitContainer(1, 0.3f).StickBottom().StickLeft();
                    Ui.StartScrollView(false);
                    {
                        Ui.Layout.FitContainer(0.5f, null).Height(90).StickLeft().StickTop().VerticalLayout();
                        Ui.StartGroup(false);
                        {
                            Ui.Layout.Height(40).FitWidth().StickLeft().StickTop();
                            Ui.Theme.FontSize(16).Once();
                            Ui.TextRect($"<color=#ff0000>Completed levels</color>\n{stats.LevelIndex} / {sc.Levels.Length}", HorizontalTextAlign.Left, VerticalTextAlign.Middle);

                            Ui.Layout.Height(40).FitWidth().StickLeft().StickTop();
                            Ui.Theme.FontSize(16).Once();
                            Ui.TextRect($"<color=#ff0000>Time spent</color>\n{stats.TotalTimeSpent:hh\\:mm\\:ss}", HorizontalTextAlign.Left, VerticalTextAlign.Middle);
                        }
                        Ui.End();

                        Ui.Layout.FitContainer(0.5f, null).Height(90).StickRight().StickTop().VerticalLayout();
                        Ui.StartGroup(false);
                        {
                            Ui.Layout.Height(40).FitWidth().StickRight().StickTop();
                            Ui.Theme.FontSize(16).Once();
                            Ui.TextRect($"<color=#ff0000>Kills</color>\n{stats.TotalKills}", HorizontalTextAlign.Right, VerticalTextAlign.Middle);

                            Ui.Layout.Height(40).FitWidth().StickRight().StickTop();
                            Ui.Theme.FontSize(16).Once();
                            Ui.TextRect($"<color=#ff0000>Deaths</color>\n{stats.TotalDeaths}", HorizontalTextAlign.Right, VerticalTextAlign.Middle);
                        }
                        Ui.End();
                    }
                    Ui.End();

                    Ui.Layout.Size(130, 40).StickLeft().StickBottom().Order(1);
                    if (Ui.Button("Level select"))
                    {
                        Game.Scene = LevelSelectionMenuScene.Load(Game);
                        MadnessUtils.Flash(Colors.Black, 0.2f);
                    }

                    Ui.Layout.Size(130, 40).StickRight().StickBottom().Order(1);
                    if (Ui.Button(stats.LevelIndex == 0 ? "Play" : "Continue"))
                    {
                        // TODO ensure we cant press this button while loading a campaign

                        Game.Scene = LevelLoadingScene.Create(Game, Registries.Levels.Get(sc.Levels[stats.LevelIndex]).Level, SceneCacheSettings.NoCache);
                        MadnessUtils.Flash(Colors.Black, 0.2f);
                    }
                }
            }
            else
            {
                Ui.Layout.FitContainer().Center();
                Ui.TextRect("Select a campaign on the left", HorizontalTextAlign.Center, VerticalTextAlign.Middle);
            }
        }
        Ui.End();

        if (MenuUiUtils.BackButton())
            Game.Scene = MainMenuScene.Load(Game);
    }
}
