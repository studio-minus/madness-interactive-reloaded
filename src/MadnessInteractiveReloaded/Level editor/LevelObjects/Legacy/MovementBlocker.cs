using System;
using Walgelijk;
using Walgelijk.Physics;
using Walgelijk.SimpleDrawing;

namespace MIR.LevelEditor.Objects;

[Obsolete]
public class MovementBlocker : RectangleObject
{
    public MovementBlocker(LevelEditor.LevelEditorComponent editor, Rect rectangle) : base(editor)
    {
        Rectangle = rectangle;
    }

    public override object Clone()
    {
        return new MovementBlocker(Editor, Rectangle);
    }

    public override void ProcessInEditor(Scene scene, InputState input)
    {
        if (ProcessRectangle(input))
            Editor.UpdateFloorLine();

        var isSelected = Editor.SelectionManager.SelectedObject == this;

        Draw.OutlineColour = isSelected ? Colors.White : Colors.Cyan;
        Draw.OutlineWidth = 4 * Editor.PixelSize;
        Draw.Colour = (Editor.SelectionManager.HoveringObject == this || isSelected) ? Colors.White.WithAlpha(0.4f) : Colors.Cyan.WithAlpha(0.2f);
        Draw.Quad(Rectangle.TopLeft, Rectangle.GetSize(), 0, 0);
    }

    public override bool IsFloor() => true;

    public override void SpawnInGameScene(Scene scene)
    {
        var wallEntity = scene.CreateEntity();
        var transform = scene.AttachComponent(wallEntity, new TransformComponent
        {
            Position = Rectangle.GetCenter()
        });
        var phy = scene.AttachComponent(wallEntity, new PhysicsBodyComponent());
        phy.BodyType = BodyType.Static;
        phy.FilterBits = CollisionLayers.BlockMovement;
        phy.Collider = new RectangleCollider(transform, Rectangle.GetSize());


        if (Tag.HasValue)
            scene.SetTag(wallEntity, Tag.Value);
    }

    public override void ProcessPropertyUi() { }
}
