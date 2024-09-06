using System.Numerics;
using Walgelijk;
using Walgelijk.SimpleDrawing;

namespace MIR.LevelEditor.Objects;

/// <summary>
/// If player overlaps, go to the next level.
/// </summary>
public class LevelProgressTrigger : RectangleObject
{
    public static readonly Color Color = new Color("#8ceb6c");

    public LevelProgressTrigger(LevelEditor.LevelEditorComponent editor, Rect rectangle) : base(editor)
    {
        Rectangle = rectangle;
    }

    public override object Clone() => new LevelProgressTrigger(Editor, Rectangle);

    public override void ProcessInEditor(Scene scene, InputState input)
    {
        ProcessRectangle(input);

        var isSelected = Editor.SelectionManager.SelectedObject == this;

        Draw.OutlineColour = isSelected || Editor.SelectionManager.IsHovering(this) ? Colors.White : Color;
        Draw.OutlineWidth = (isSelected ? 2 : 1) * Editor.PixelSize;
        Draw.Colour = Color.WithAlpha(0.2f);
        Draw.Quad(Rectangle.TopLeft, Rectangle.GetSize(), 0, 0);
        Draw.Colour = Color;
        Draw.Font = Fonts.Oxanium;
        Draw.FontSize = 16;
        Draw.Text(nameof(LevelProgressTrigger), Rectangle.GetCenter(), Vector2.One, HorizontalTextAlign.Center, VerticalTextAlign.Middle);
    }

    public override void ProcessPropertyUi()
    {
    }

    public override void SpawnInGameScene(Scene scene)
    {
        var e = scene.CreateEntity();

        if (Tag.HasValue)
            scene.SetTag(e, Tag.Value);

        scene.AttachComponent(e, new LevelProgressTriggerComponent { WorldRect = Rectangle });
    }
}