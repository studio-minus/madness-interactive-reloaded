﻿using System;
using System.Linq;
using System.Numerics;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.Localisation;
using Walgelijk.Onion;
using Walgelijk.Onion.Controls;
using Walgelijk.Onion.Layout;
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

        DrawMainPanel(data, character);

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

    private void DrawMainPanel(CharacterCreationComponent data, CharacterComponent character)
    {
        Ui.Theme.Padding(15).Push();

        Ui.Layout.FitContainer(null, 1).AspectRatio(0.6f, AspectRatioBehaviour.Grow).StickLeft().StickTop();
        Ui.Theme.ForegroundColor(Colors.Black).Once();
        Ui.StartGroup(true);
        {
            const int topBarHeight = 70;
            float cursor = topBarHeight;

            var borderTex = Assets.Load<Texture>("textures/border-top-bottom.png").Value;
            Ui.Theme.Padding(0).OutlineWidth(0).Foreground(new(new Appearance(borderTex, ImageMode.Slice))).Image(new(Colors.White.WithAlpha(0.8f), Colors.White, Colors.White.Brightness(0.7f))).Push();
            Ui.Layout.FitWidth(false).Height(topBarHeight).HorizontalLayout();
            // upper tab bar
            Ui.StartGroup(false);
            {
                if (data.CurrentTab == CharacterCreationTab.Head)
                    Ui.Decorate(new CrosshairDecorator());
                Ui.Layout.FitContainer(1 / 4f, 1, false);
                if (Ui.ImageButton(Assets.Load<Texture>("textures/ui/category_icons/skin_icon.png").Value, ImageContainmentMode.Contain))
                    data.CurrentTab = CharacterCreationTab.Head;

                if (data.CurrentTab == CharacterCreationTab.Body)
                    Ui.Decorate(new CrosshairDecorator());
                Ui.Layout.FitContainer(1 / 4f, 1, false);
                if (Ui.ImageButton(Assets.Load<Texture>("textures/ui/category_icons/body_icon.png").Value, ImageContainmentMode.Contain))
                    data.CurrentTab = CharacterCreationTab.Body;

                if (data.CurrentTab == CharacterCreationTab.Hands)
                    Ui.Decorate(new CrosshairDecorator());
                Ui.Layout.FitContainer(1 / 4f, 1, false);
                if (Ui.ImageButton(Assets.Load<Texture>("textures/ui/category_icons/hand_icon.png").Value, ImageContainmentMode.Contain))
                    data.CurrentTab = CharacterCreationTab.Hands;

                if (data.CurrentTab == CharacterCreationTab.Blood)
                    Ui.Decorate(new CrosshairDecorator());
                Ui.Layout.FitContainer(1 / 4f, 1, false);
                if (Ui.ImageButton(Assets.Load<Texture>("textures/ui/category_icons/blood_icon.png").Value, ImageContainmentMode.Contain))
                    data.CurrentTab = CharacterCreationTab.Blood;
            }
            Ui.End();
            Ui.Theme.Pop();

            // layer selector
            if (data.CurrentTab is CharacterCreationTab.Head or CharacterCreationTab.Body)
            {
                const int layerBarHeight = topBarHeight + 20;
                cursor += layerBarHeight + 20;
                Ui.Layout.FitWidth(false).StickTop().Move(0, topBarHeight).Scale(-100, 0).CenterHorizontal().Height(layerBarHeight).EnqueueLayout(new DistributeChildrenLayout());
                Ui.StartGroup(false);
                {
                    int c = data.CurrentTab == CharacterCreationTab.Head ? CharacterLook.HeadLayerCount : CharacterLook.BodyLayerCount;
                    int layer = data.CurrentTab == CharacterCreationTab.Head ? data.SelectedHeadLayer : data.SelectedBodyLayer;
                    for (int i = 0; i < c; i++)
                    {
                        if (i == layer)
                            Ui.Theme.OutlineWidth(2).Once();

                        Texture? tex = null;

                        if (data.CurrentTab == CharacterCreationTab.Head)
                        {
                            var p = data.GetHeadPieceRef(character, i);
                            if (p != null)
                                tex = p.Left.Value;
                        }
                        else
                        {
                            var p = data.GetBodyPieceRef(character, i);
                            if (p != null)
                                tex = p.Left.Value;
                        }

                        Ui.Layout.FitHeight(false).MinWidth(10);
                        Ui.Theme.FontSize(18).Once();
                        if (ThumbnailButton.Click(i == 0 ? Localisation.Get("base-layer") : string.Format(Localisation.Get("frmt-layer"), i), tex, i))
                        {
                            switch (data.CurrentTab)
                            {
                                case CharacterCreationTab.Head:
                                    data.SelectedHeadLayer = i;
                                    break;
                                case CharacterCreationTab.Body:
                                    data.SelectedBodyLayer = i;
                                    break;
                            }
                        }
                    }
                }
                Ui.End();
            }

            // piece grid
            Ui.Layout.FitContainer(1, 1, false).Center().Scale(0, -cursor - 5).StickBottom(false);
            Ui.StartGroup();
            switch (data.CurrentTab)
            {
                case CharacterCreationTab.Head:
                    HeadMenu(data, character);
                    break;
                case CharacterCreationTab.Body:
                    BodyMenu(data, character);
                    break;
                case CharacterCreationTab.Hands:
                    HandMenu(data, character);
                    break;
                case CharacterCreationTab.Blood:
                    BloodMenu(data, character);
                    break;
            }
            Ui.End();
        }
        Ui.End();

        Ui.Theme.Pop();
    }

    private void BloodMenu(CharacterCreationComponent data, CharacterComponent character)
    {
        var look = character.Look;

        Ui.Theme.FontSize(23).Once();
        Ui.Layout.FitWidth().Height(30).CenterHorizontal();
        Ui.TextRect(Localisation.Get("blood-colour"), HorizontalTextAlign.Center, VerticalTextAlign.Middle);

        Ui.Layout.FitWidth().Height(40).CenterHorizontal().Move(0, 40).EnqueueLayout(new DistributeChildrenLayout());
        Ui.StartGroup();
        {
            var rect = Onion.Tree.CurrentNode!.GetInstance().Rects.ComputedGlobal;
            var colors = data.Swatches.Concat([Colors.Yellow, Colors.Red]).Reverse().Take((int)(rect.Width / (rect.Height + 1))); // TODO this could be faster. replace the list with an array and be more careful where you put stuff and what you replace
            int i = 0;
            foreach (var c in colors)
            {
                Ui.Layout.FitHeight(false);
                Ui.Decorate(new FancyButtonDecorator());
                Ui.Theme.Image(c).OutlineColour(Colors.Gray).OutlineWidth(1).Once();
                if (Ui.ImageButton(Texture.White, ImageContainmentMode.Stretch, identity: i++))
                    look.BloodColour = c;
            }
        }
        Ui.End();

        Ui.Layout.FitWidth().Scale(-Ui.Theme.GetChanges().Padding, 0).CenterHorizontal().AspectRatio(2).MinHeight(200).Move(0, 90);
        Ui.Theme.Padding(5).OutlineColour(Colors.Gray).OutlineWidth(1).Push();
        Ui.ColourPicker(ref look.BloodColour);
        Ui.Theme.Pop();

        if (Onion.Input.MousePrimaryRelease && Onion.Tree.GetLastInstance().Rects.Rendered.ContainsPoint(Onion.Input.MousePosition))
        {
            data.Swatches.Add(look.BloodColour);
            if (data.Swatches.Count > 8)
                data.Swatches.RemoveAt(0);
        }
    }

    private void HandMenu(CharacterCreationComponent data, CharacterComponent character)
    {
        var look = character.Look;

        if (PieceGrid(character, Registries.Armour.HandArmour, ref look.Hands, Registries.Armour.HandArmour.Get("default")))
            if (Utilities.RandomFloat() > 0.9f)
                character.PlayAnimation(Utilities.PickRandom(Animations.CharacterCreationHandsAnimations));
    }

    private void HeadMenu(CharacterCreationComponent data, CharacterComponent character)
    {
        var look = character.Look;
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
    }

    private void BodyMenu(CharacterCreationComponent data, CharacterComponent character)
    {
        var look = character.Look;
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
    }

    private void DrawBackground(CharacterCreationComponent data)
    {
        Draw.Reset();
        Draw.Order = RenderOrders.BackgroundBehind;
        Draw.ScreenSpace = true;
        Draw.TransformMatrix = Matrix3x2.CreateScale(1.2f, new Vector2(0, Window.Height * 0.88f));

        var rect = playerDrawRect = Draw.Image(data.Background.Value, new Rect(0, 0, Window.Width, Window.Height), ImageContainmentMode.Cover);
        calculatedPlayerRect = Draw.Image(data.PlayerDrawTarget, rect.Scale(0.4f).Translate(0, 45), ImageContainmentMode.Cover);

        Draw.ClearMask();
        Draw.WriteMask();
        Draw.ResetTexture();
        Draw.Material = Materials.AlphaClipDraw;
        Draw.Image(Assets.Load<Texture>("textures/backgrounds/character_creation_mirror.qoi").Value, new Rect(0, 0, Window.Width, Window.Height), ImageContainmentMode.Cover);

        Draw.ResetMaterial();
        Draw.TransformMatrix = Matrix3x2.CreateScale(-1, 1, rect.GetCenter()) * Draw.TransformMatrix;
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
        const int preferredColumns = 3;

        Ui.Theme.Padding(5).Push();

        float padding = Onion.Tree.CurrentNode!.GetInstance().Theme.Padding;

        bool returnValue = false;
        Ui.Layout.FitContainer(1, 1, false).StickLeft(false).StickBottom(false).VerticalLayout().Overflow(false, true);
        Ui.Theme.ScrollbarWidth(24).ForegroundColor(Colors.Black).Once();
        Ui.StartScrollView(false);
        {
            float w = Onion.Tree.CurrentNode!.GetInstance().Rects.GetInnerContentRect().Width - padding;
            int i = 0;
            float x = 0;
            float rowHeight = (w) / preferredColumns;
            foreach (var piece in registry.GetAllValues().OrderBy(static p => p.Order))
            {
                if (piece.Hidden)
                    continue;

                if (x > w || i == 0)
                {
                    x = rowHeight;

                    // end the previous row if this isnt the first row ever made!!
                    if (i != 0)
                        Ui.End();

                    // start a row
                    Ui.Layout.FitWidth(true).StickLeft(true).StickTop(false).Height(rowHeight).HorizontalLayout();
                    Ui.StartGroup(false, identity: i + 428);
                }

                Ui.Layout.FitHeight(false).AspectRatio(1, AspectRatioBehaviour.Grow).StickLeft(false).StickTop(false);
                Ui.Theme.Padding(0).OutlineWidth(0).FontSize(20).Once();
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

                x += rowHeight;
            }

            Ui.End(); // end the last row
        }
        Ui.End();

        Ui.Theme.Pop();

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

public struct DistributeChildrenLayout : ILayout
{
    public void Apply(in ControlParams p, int index, int childId)
    {
        float padding = p.Theme.Padding;
        var child = p.Tree.EnsureInstance(childId); //  - p.Theme.Padding
        var w = (p.Instance.Rects.Rendered.Width + padding) / p.Node.Children.Count(Onion.Tree.IsAlive);

        child.Rects.Intermediate.Width = (w - padding);
        child.Rects.Intermediate = child.Rects.Intermediate.Translate(w * index, 0);
    }
}