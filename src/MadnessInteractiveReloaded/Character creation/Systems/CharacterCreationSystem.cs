using MIR.Controls;
using System;
using System.Data;
using System.Linq;
using System.Numerics;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.Localisation;
using Walgelijk.Onion;
using Walgelijk.Onion.Controls;
using Walgelijk.SimpleDrawing;

using AspectRatioBehaviour = Walgelijk.Onion.Layout.AspectRatio.Behaviour;

namespace MIR;

/// <summary>
/// The system for the character creation menu.
/// </summary>
public class CharacterCreationSystem : Walgelijk.System
{
    // TODO put in component

    private static Rect playerDrawRect;
    private static Rect calculatedPlayerRect;
    private static readonly MenuCharacterRenderer menuCharacterRenderer = new();

    public override void Render()
    {
        if (!Scene.FindAnyComponent<CharacterCreationComponent>(out var data))
            return;

        if (!MadnessUtils.FindPlayer(Scene, out _, out var character))
            return;

        menuCharacterRenderer.Render(Window, data.PlayerDrawTarget, character);
    }

    public override void Update()
    {
        if (!Scene.FindAnyComponent<CharacterCreationComponent>(out var data))
            return;

        if (!MadnessUtils.FindPlayer(Scene, out var player, out var character))
            return;

        if (!MadnessUtils.FindCamera(Scene, out var camera, out var cameraTransform) || camera == null || cameraTransform == null)
            return;

        cameraTransform.Position = new Vector2(0, -1000);
        camera.OrthographicSize = 0.8f / (Window.Height / 1080f);

        DrawBackground(data);


        var s = calculatedPlayerRect.Scale(0.9f);
        s.Width *= 0.5f;
        s = s.Translate(s.Width / 2, s.Height * 0.05f);
        var bodyBounds = s;
        var headBounds = s;

        headBounds.MaxY = headBounds.MinY + headBounds.Height * 0.5f;
        bodyBounds.MinY = headBounds.MaxY;

        UserInterface(headBounds, bodyBounds);
        ConfigurePlayer(player, character);
    }

    private void DrawBackground(CharacterCreationComponent data)
    {
        Draw.Reset();
        Draw.Order = RenderOrders.BackgroundBehind;
        Draw.ScreenSpace = true;

        var rect = playerDrawRect = Draw.Image(data.Background.Value, new Rect(0, 0, Window.Width, Window.Height), ImageContainmentMode.Cover);

        calculatedPlayerRect = Draw.Image(data.PlayerDrawTarget, rect.Scale(0.4f).Translate(0, 45), ImageContainmentMode.Cover);

        Draw.WriteMask();
        Draw.ResetTexture();
        Draw.Material = Materials.AlphaClipDraw;
        Draw.Image(Assets.Load<Texture>("textures/backgrounds/character_creation_mirror.qoi").Value, new Rect(0, 0, Window.Width, Window.Height), ImageContainmentMode.Cover);

        Draw.ResetMaterial();
        Draw.TransformMatrix = Matrix3x2.CreateScale(-1, 1, rect.GetCenter());
        Draw.Colour = Colors.Gray.WithAlpha(0.2f);

        Draw.InsideMask();
        Draw.Image(data.PlayerDrawTarget, rect.Scale(0.4f).Translate(-900f * (Window.Height / 1920f), 45), ImageContainmentMode.Cover);
        Draw.DisableMask();

        Draw.ResetTransformation();
        Draw.ScreenSpace = false;
    }

    private void UserInterface(Rect headBounds, Rect bodyBounds)
    {
        if (!Scene.FindAnyComponent<CharacterCreationComponent>(out var data))
            return;

        if (!MadnessUtils.FindPlayer(Scene, out var player, out var character))
            return;

        var look = UserData.Instances.PlayerLook;

        UnhighlightParts(character);

        Ui.Layout.Size(200, 80).StickRight().StickBottom();
        Ui.Theme.FontSize(32).OutlineWidth(4).Once();
        if (Ui.Button(Localisation.Get("done")))
        {
            Game.Scene = MainMenuScene.Load(Game);
            MadnessUtils.Flash(Colors.Black, 0.2f);
            ThumbnailRenderer.ResetAllPosters();
        }

        // HEAD MENU
        Ui.Layout.FitContainer(null, 0.33f).AspectRatio(1.5f, AspectRatioBehaviour.Grow).StickLeft().StickTop();
        Ui.StartGroup(false);
        {
            bool a = false;

            switch (data.SelectedHeadLayer)
            {
                case 0:
                    a = PieceGrid(character, Registries.Armour.Head, ref look.Head, Registries.Armour.Head.Get("default_head"));
                    break;

                case 1:
                    a = PieceGrid(character, Registries.Armour.HeadAccessory, ref look.HeadLayer1, null);
                    break;

                case 2:
                    a = PieceGrid(character, Registries.Armour.HeadAccessory, ref look.HeadLayer2, null);
                    break;

                case 3:
                    a = PieceGrid(character, Registries.Armour.HeadAccessory, ref look.HeadLayer3, null);
                    break;
            }

            if (a && Utilities.RandomFloat() > 0.9f)
                character.PlayAnimation(Utilities.PickRandom(
                    data.SelectedHeadLayer == 0 ? Animations.CharacterCreationHeadAnimations : Animations.CharacterCreationFaceAnimations
                    ));

            Ui.Layout.FitHeight().Width(58).StickRight(false).CenterVertical().VerticalLayout();
            Ui.StartGroup(false);
            {
                for (int i = 0; i <= 3; i++)
                {
                    Ui.Layout.FitWidth().AspectRatio(1, AspectRatioBehaviour.Grow).StickLeft();
                    if (data.SelectedHeadLayer == i)
                        Ui.Decorate(new CrosshairDecorator());
                    Ui.Decorate(new FancyButtonDecorator());

                    Texture? tex = null;

                    var p = data.GetHeadPieceRef(character, i);
                    if (p != null)
                        tex = p.Left.Value;

                    Ui.Decorators.Tooltip("Layer " + i);
                    if (ItemButton.Start(tex, identity: i))
                        data.SelectedHeadLayer = i;
                    if (Onion.Tree.LastNode.GetInstance().IsHover)
                        HighlightPart(character, i == 0 ? ArmourPieceType.Head : ArmourPieceType.HeadAccessory, i - 1);
                }
            }
            Ui.End();
        }
        Ui.End();

        // BODY MENU
        Ui.Layout.FitContainer(null, 0.33f).AspectRatio(1.5f, AspectRatioBehaviour.Grow).StickLeft().CenterVertical();
        Ui.StartGroup(false);
        {
            bool a = false;
            switch (data.SelectedBodyLayer)
            {
                case 0:
                    a = PieceGrid(character, Registries.Armour.Body, ref look.Body, Registries.Armour.Body.Get("default_body"));
                    break;

                case 1:
                    a = PieceGrid(character, Registries.Armour.BodyAccessory, ref look.BodyLayer1, null);
                    break;

                case 2:
                    a = PieceGrid(character, Registries.Armour.BodyAccessory, ref look.BodyLayer2, null);
                    break;
            }

            if (a && Utilities.RandomFloat() > 0.9f)
                character.PlayAnimation(Utilities.PickRandom(Animations.CharacterCreationBodyAnimations));

            Ui.Layout.FitHeight().Width(58).StickRight(false).CenterVertical().VerticalLayout();
            Ui.StartGroup(false);
            {
                for (int i = 0; i <= 2; i++)
                {
                    Ui.Layout.FitWidth().AspectRatio(1, AspectRatioBehaviour.Grow).StickLeft();
                    if (data.SelectedBodyLayer == i)
                        Ui.Decorate(new CrosshairDecorator());
                    Ui.Decorate(new FancyButtonDecorator());

                    Texture? tex = null;

                    var p = data.GetBodyPieceRef(character, i);
                    if (p != null)
                        tex = p.Left.Value;

                    Ui.Decorators.Tooltip("Layer " + i);
                    if (ItemButton.Start(tex, identity: i))
                        data.SelectedBodyLayer = i;
                    if (Onion.Tree.LastNode.GetInstance().IsHover)
                        HighlightPart(character, i == 0 ? ArmourPieceType.Body : ArmourPieceType.BodyAccessory, i - 1);
                }
            }
            Ui.End();
        }
        Ui.End();

        // HAND MENU
        Ui.Layout.FitContainer(null, 0.33f).AspectRatio(1.5f, AspectRatioBehaviour.Grow).StickLeft().StickBottom();
        Ui.StartGroup(false);
        {
            if (PieceGrid(character, Registries.Armour.HandArmour, ref look.Hands, Registries.Armour.HandArmour.Get("default")))
                if (Utilities.RandomFloat() > 0.9f)
                    character.PlayAnimation(Utilities.PickRandom(Animations.CharacterCreationHandsAnimations));
        }
        Ui.End();

        // BLOOD MENU
        Ui.Layout.Size(300, 300).StickRight().StickTop();
        Ui.StartGroup(false);
        {
            Ui.Layout.FitContainer(1, 1, false);
            Ui.Theme.Foreground(default).Once();
            Ui.ColourPicker(ref look.BloodColour);
        }
        Ui.End();

        Ui.Layout.Width(120).Height(40).CenterHorizontal().StickBottom().Move(0, -15);
        Ui.Theme.Foreground(default).Image(new(Colors.White, Colors.Red)).OutlineWidth(0).Once();
        if (Ui.ImageButton(Assets.Load<Texture>("textures/ui/icon/flip.png").Value, ImageContainmentMode.Center))
        {
            character.Positioning.IsFlipped = !character.Positioning.IsFlipped;
            character.NeedsLookUpdate = true;
        }
    }

    private void UnhighlightParts(CharacterComponent character)
    {
        reset(character.Positioning.Body.Entity);
        reset(character.Positioning.Head.Entity);
        reset(character.Positioning.Hands.First.Entity);
        reset(character.Positioning.Hands.Second.Entity);

        foreach (var e in character.Positioning.BodyDecorations)
            reset(e);

        foreach (var e in character.Positioning.HeadDecorations)
            reset(e);

        void reset(Entity ent)
        {
            if (Scene.TryGetComponentFrom<QuadShapeComponent>(ent, out var quad))
            {
                if (quad.Material.TryGetUniform("tint", out Vector4 c))
                    quad.Material.SetUniform("tint", Utilities.SmoothApproach(c, Colors.White, 5, Time.DeltaTime));
                else
                    quad.Material.SetUniform("tint", Colors.White);
            }
        }
    }

    private void HighlightPart(CharacterComponent character, ArmourPieceType s, int index)
    {
        var targetColour = Utilities.Lerp(Colors.White, Colors.Red, (float.Sin(Time.SecondsSinceSceneChange * 8) * 0.5f + 0.5f) * 0.8f);

        if (s is ArmourPieceType.BodyAccessory or ArmourPieceType.HeadAccessory)
        {
            var ents = s switch
            {
                ArmourPieceType.Body or ArmourPieceType.BodyAccessory => character.Positioning.BodyDecorations,
                ArmourPieceType.Head or ArmourPieceType.HeadAccessory => character.Positioning.HeadDecorations,
                _ => null
            };

            if (ents == null)
                return;

            if (index < 0 || index >= ents.Length)
                return;

            var a = ents[index];
            set(a);
        }
        else if (s is ArmourPieceType.Hand)
        {
            if (index < 0 || index >= 2)
                return;

            var a = character.Positioning.Hands[index];
            set(a.Entity);
        }
        else
        {
            var a = s switch
            {
                ArmourPieceType.Body or ArmourPieceType.BodyAccessory => character.Positioning.Body.Entity,
                ArmourPieceType.Head or ArmourPieceType.HeadAccessory => character.Positioning.Head.Entity,
                _ => default
            };
            set(a);
        }

        void set(Entity ent)
        {
            if (Scene.TryGetComponentFrom<QuadShapeComponent>(ent, out var quad))
            {
                if (quad.Material.TryGetUniform("tint", out Vector4 c))
                    quad.Material.SetUniform("tint", Utilities.SmoothApproach(c, targetColour, 5, Time.DeltaTime));
                else
                    quad.Material.SetUniform("tint", Colors.White);
            }
        }
    }

    private bool PieceGrid<T>(CharacterComponent character, Registry<T> registry, ref T? target, T? @default) where T : class, ICharacterCustomisationItem
    {
        bool returnValue = false;
        Ui.Layout.FitContainer().Scale(-60, 0).StickLeft().StickBottom().VerticalLayout().Overflow(false, true);
        Ui.Theme.ScrollbarWidth(16).ForegroundColor(Colors.Black).Once();
        Ui.StartScrollView(true);
        {
            float w = Onion.Tree.CurrentNode!.GetInstance().Rects.ComputedGlobal.Width;
            int i = 0;
            float x = 0;
            const int preferredColumns = 3;
            float rowHeight = (w - Onion.Theme.Base.Padding * (2 * preferredColumns)) / preferredColumns;
            foreach (var piece in registry.GetAllValues().OrderBy(static p => p.Order))
            {
                if (piece.Hidden)
                    continue;

                if (x > w || i == 0)
                {
                    x = rowHeight + Ui.Theme.Base.Padding * 2;

                    // end the previous row if this isnt the first row ever made!!
                    if (i != 0)
                        Ui.End();

                    // start a row
                    Ui.Layout.FitWidth().StickLeft().StickTop().Height(rowHeight).HorizontalLayout();
                    Ui.StartGroup(false, identity: i + 428);
                }

                Ui.Layout.FitHeight().AspectRatio(0.9f, AspectRatioBehaviour.Grow).StickLeft().StickTop();
                Ui.Theme.Padding(0).OutlineWidth(0).Once();
                if (target == piece)
                    Ui.Decorate(new CrosshairDecorator());
                if (ThumbnailButton.Click(piece.DisplayName, piece.Texture, identity: i++))
                {
                    if (target == piece)
                        target = @default;
                    else
                        target = piece;
                    character.NeedsLookUpdate = true;
                    returnValue = true;
                }

                x += rowHeight + Ui.Theme.Base.Padding * 2;
            }

            Ui.End(); // end the last row
        }
        Ui.End();
        return returnValue;
    }

    private void ConfigurePlayer(PlayerComponent player, CharacterComponent character)
    {
        player.RespondToUserInput = false;
        character.Positioning.ShouldFeetFollowBody = false;

        if (character.Look != UserData.Instances.PlayerLook)
            character.Look = UserData.Instances.PlayerLook;

        if (!character.IsPlayingAnimation || character.MainAnimation!.IsAlmostOver(0.7f))
            character.PlayAnimation(Animations.CharacterCreationIdleAnimation, 0.5f);

        var v3 = UserData.Instances.PlayerLook.BloodColour.RGB;

        if (Scene.TryGetComponentFrom<QuadShapeComponent>(character.Positioning.Head.Entity, out var headShape))
        {
            var c = headShape.Material;
            c.SetUniform("outerBloodColour", v3);
            c.SetUniform("innerBloodColour", v3 * 0.6522f);
        }

        if (Scene.TryGetComponentFrom<QuadShapeComponent>(character.Positioning.Body.Entity, out var bodyShape))
        {
            var c = bodyShape.Material;
            c.SetUniform("outerBloodColour", v3);
            c.SetUniform("innerBloodColour", v3 * 0.6522f);
        }
    }
}
