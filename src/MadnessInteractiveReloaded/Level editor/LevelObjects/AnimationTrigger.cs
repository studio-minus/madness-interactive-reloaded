using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Walgelijk;
using Walgelijk.Onion;
using Walgelijk.Onion.Controls;
using Walgelijk.SimpleDrawing;
using static MIR.AnimationTriggerComponent;

namespace MIR.LevelEditor.Objects;

/// <summary>
/// If a character intersects this, the character will play an animation.
/// </summary>
public class AnimationTrigger : RectangleObject
{
    public static readonly Color Color = new Color("#c95378");

    public AnimationTriggerComponent Component = new();

    private static readonly List<string> suggestions = new();

    public AnimationTrigger(LevelEditor.LevelEditorComponent editor, Rect rect) : base(editor)
    {
        Rectangle = rect;
        Component.WorldRect = Rectangle;
    }

    public override object Clone() => new AnimationTrigger(Editor, Rectangle)
    {
        Component = Component.Clone()
    };

    public override void ProcessInEditor(Scene scene, InputState input)
    {
        ProcessRectangle(input);
        Component.WorldRect = Rectangle;

        var isSelected = Editor.SelectionManager.SelectedObject == this;

        Draw.OutlineColour = isSelected || Editor.SelectionManager.IsHovering(this) ? Colors.White : Color;
        Draw.OutlineWidth = (isSelected ? 2 : 1) * Editor.PixelSize;
        Draw.Colour = Color.WithAlpha(0.2f);
        Draw.Quad(Rectangle.TopLeft, Rectangle.GetSize(), 0, 0);
        Draw.Colour = Color;
        Draw.Font = Fonts.Oxanium;
        Draw.FontSize = 16;
        Draw.Text(Component.Animation ?? "null", Rectangle.TopLeft + new Vector2(15, -15), Vector2.One);
        Draw.Text("Target: " + Component.Target.ToString(), Rectangle.TopLeft + new Vector2(15, -15 - 16), Vector2.One);

        {
            var pp = Component.TargetPosition;
            float animRot = 0;

            if (Registries.Animations.TryGet(Component.Animation ?? string.Empty, out var anim))
            {
                if (anim.BodyAnimation != null)
                {
                    var t = Game.Main.State.Time.SecondsSinceLoad;
                    t /= anim.BodyAnimation.Duration;
                    t %= 1;

                    if (anim.BodyAnimation.TranslationCurve != null)
                        pp += anim.BodyAnimation.TranslationCurve.Evaluate(t);

                    if (anim.BodyAnimation.RotationCurve != null)
                        animRot += anim.BodyAnimation.RotationCurve.Evaluate(t);
                }

                if (Component.TargetFlipped)
                {
                    animRot *= -1;
                    pp.X -= Component.TargetPosition.X;
                    pp.X *= -1;
                    pp.X += Component.TargetPosition.X;
                }
            }

            Draw.Colour = Color.WithAlpha(0.5f);
            var s = Textures.UserInterface.EditorNpcPlaceholder.Value.Size * MadnessConstants.BackgroundSizeRatio;
            if (Component.TargetFlipped)
                s.X *= -1;

            Draw.Line(Component.TargetPosition, Rectangle.ClosestPoint(Component.TargetPosition), 16);
            Draw.Texture = Textures.UserInterface.EditorNpcPlaceholder.Value;
            Draw.Colour.A = 1;

            Draw.Quad(Component.TargetPosition + new Vector2(-s.X / 2, s.Y), s);
            Draw.TransformMatrix = Matrix3x2.CreateRotation(animRot * Utilities.DegToRad) * Matrix3x2.CreateTranslation(pp);
            Draw.Colour.A = 0.5f;
            Draw.Quad(new Vector2(-s.X / 2, s.Y), s);
            Draw.ResetTransformation();
        }
    }

    public override void ProcessPropertyUi()
    {
        Ui.Label("Start event");
        Ui.Decorators.Tooltip("When should the animation trigger start responding to the player?");
        Ui.Layout.FitWidth(false).Height(32);
        if (Ui.EnumDropdown(ref Component.StartEvent))
            Editor.Dirty = true;

        Ui.Spacer(16);
        Ui.Label("Animation key");
        Ui.Layout.FitWidth(false).Height(32);
        if (Ui.StringInputBox(ref Component.Animation, TextBoxOptions.TextInput))
            FindSuggestions();

        if (!Registries.Animations.Has(Component.Animation))
        {
            Ui.Layout.FitWidth(false).Height(128).VerticalLayout();
            Ui.StartScrollView();
            {
                int i = 0;
                foreach (var item in suggestions)
                {
                    Ui.Layout.FitWidth(false).Height(32);
                    if (Ui.Button(item, i++))
                        Component.Animation = item;
                }
            }
            Ui.End();
        }
        Ui.Spacer(16);
        Ui.Label("Target");
        Ui.Layout.FitWidth(false).Height(32);
        if (Ui.EnumDropdown(ref Component.Target))
            Editor.Dirty = true;

        Ui.Spacer(16);
        Ui.Label("Target position");
        Ui.Layout.FitWidth(false).Height(32);
        Ui.StartGroup();
        {
            Ui.Layout.FitContainer(0.5f, 1, true).StickLeft().StickTop();
            Ui.FloatInputBox(ref Component.TargetPosition.X);

            Ui.Layout.FitContainer(0.5f, 1, true).StickRight().StickTop();
            Ui.FloatInputBox(ref Component.TargetPosition.Y);
        }
        Ui.End();

        Ui.Layout.FitWidth(false).Height(32);
        if (Ui.Button("Target position to trigger position"))
            Component.TargetPosition = (Rectangle.BottomLeft + Rectangle.BottomRight) * 0.5f;

        Ui.Layout.FitWidth(false).Height(32);
        Ui.Checkbox(ref Component.TargetFlipped, "Flipped");


        switch (Component.Target)
        {
            case TargetType.CreateNew:
                Ui.Spacer(16);
                Ui.Label("Faction");
                Ui.Layout.FitWidth(false).Height(32);
                Ui.StringInputBox(ref Component.CreateNewFaction, TextBoxOptions.TextInput);

                Ui.Label("Look and stats");
                Ui.Layout.FitWidth(false).Height(32);
                int si = Array.IndexOf(Editor.Looks, Component.CreateNewLook);
                if (si == -1)
                {
                    si = 0;
                    Component.CreateNewLook = Editor.Looks[si];
                }
                if (Ui.Dropdown(Editor.Looks, ref si))
                    Component.CreateNewLook = Editor.Looks[si];

                Ui.Layout.FitWidth(false).Height(32);
                si = Array.IndexOf(Editor.Stats, Component.CreateNewStats);
                if (si == -1)
                {
                    si = 0;
                    Component.CreateNewStats = Editor.Stats[si];
                }
                if (Ui.Dropdown(Editor.Stats, ref si))
                    Component.CreateNewStats = Editor.Stats[si];

                break;
        }
    }

    public override void SpawnInGameScene(Scene scene)
    {
        var e = scene.CreateEntity();
        Component.WorldRect = Rectangle;

        if (Tag.HasValue)
            scene.SetTag(e, Tag.Value);

        scene.AttachComponent(e, Component.Clone());
    }

    private void FindSuggestions()
    {
        suggestions.Clear();
        suggestions.AddRange(Registries.Animations.GetAllKeys().OrderByDescending(a => GetSimilarity(a, Component.Animation)).Take(8));
    }

    private static float GetSimilarity(string str1, string str2)
    {
        str1 = str1.ToLowerInvariant();
        str2 = str2.ToLowerInvariant();

        int len1 = str1.Length;
        int len2 = str2.Length;
        if (len1 == 0 || len2 == 0)
            return 0;

        if (len1 == len2 && str1 == str2)
            return 1;

        int matches = 0;
        for (int i = 0; i < len1; i++)
            if (str2.Contains(str1[i])) matches++;

        var ratio = (float)matches / str1.Length;
        return ratio;
    }
}
