using System;
using System.Numerics;
using Walgelijk;
using Walgelijk.Physics;
using Walgelijk.SimpleDrawing;

namespace MIR.LevelEditor.Objects;

[Obsolete]
public class AllBlocker : RectangleObject
{
    public AllBlocker(LevelEditor.LevelEditorComponent editor, Rect rectangle) : base(editor)
    {
        Rectangle = rectangle;
    }

    public override object Clone()
    {
        return new AllBlocker(Editor, Rectangle);
    }

    public override void ProcessInEditor(Scene scene, InputState input)
    {
        if (ProcessRectangle(input))
            Editor.UpdateFloorLine();

        var isSelected = Editor.SelectionManager.SelectedObject == this;

        Draw.OutlineColour = isSelected ? Colors.White : Colors.Purple;
        Draw.OutlineWidth = 4 * Editor.PixelSize;
        Draw.Colour = (Editor.SelectionManager.HoveringObject == this || isSelected) ? Colors.White.WithAlpha(0.4f) : Colors.Purple.WithAlpha(0.2f);
        Draw.Quad(Rectangle.TopLeft, Rectangle.GetSize(), 0, 0);
    }

    public override void ProcessPropertyUi() { }

    public override float? GetFloorPointAt(float x)
    {
        if (x < Rectangle.MinX || x > Rectangle.MaxX)
            return null;

        return Rectangle.MaxY;
    }

    public override bool IsFloor() => true;

    public override void SpawnInGameScene(Scene scene)
    {
        var wallEntity = scene.CreateEntity();
        var transform = scene.AttachComponent(wallEntity, new TransformComponent
        {
            Position = Rectangle.GetCenter(),
            Scale = Rectangle.GetSize()
        });
        var phy = scene.AttachComponent(wallEntity, new PhysicsBodyComponent());
        phy.BodyType = BodyType.Static;
        phy.FilterBits = CollisionLayers.BlockAll;
        phy.Collider = new RectangleCollider(transform, Vector2.One);

        if (Tag.HasValue)
            scene.SetTag(wallEntity, Tag.Value);
    }
}
