using System;
using System.Numerics;
using Walgelijk;
using Walgelijk.SimpleDrawing;

namespace MIR.LevelEditor.Objects;

/// <summary>
/// Define a place for the camera to focus on.
/// </summary>
public class CameraSection : RectangleObject
{
    public CameraSection(LevelEditor.LevelEditorComponent editor, Rect rectangle) : base(editor)
    {
        Rectangle = rectangle;
    }

    public override object Clone() => new CameraSection(Editor, Rectangle);

    public override Rect? GetBounds() => Rectangle;

    public override void ProcessInEditor(Scene scene, InputState input)
    {
        ProcessRectangle(input);
        var isSelected = Editor.SelectionManager.SelectedObject == this;

        Draw.OutlineColour = isSelected ? Colors.White : Colors.Aqua;
        Draw.OutlineWidth = 4 * Editor.PixelSize;
        Draw.Colour = (Editor.SelectionManager.HoveringObject == this || isSelected) ? Colors.Aqua.WithAlpha(0.1f) : Colors.Transparent;
        Draw.Quad(Rectangle.TopLeft, Rectangle.GetSize(), 0, 0);

        var c = Rectangle.GetCenter();
        float crossSize = MathF.Min(50, MathF.Min(Rectangle.Width / 2, Rectangle.Height / 2) - 30);
        if (crossSize < 5)
            return;
        Draw.Colour = Draw.OutlineColour;
        Draw.OutlineWidth = 0;
        Draw.Line(new Vector2(c.X - crossSize, c.Y), new Vector2(c.X + crossSize, c.Y), 2 * Editor.PixelSize, 0);
        Draw.Line(new Vector2(c.X, c.Y - crossSize), new Vector2(c.X, c.Y + crossSize), 2 * Editor.PixelSize, 0);
    }

    public override void ProcessPropertyUi() { }

    public override void SpawnInGameScene(Scene scene)
    {
        var entity = scene.CreateEntity();
        scene.AttachComponent(entity, new CameraSectionComponent(Rectangle));
    }
}
