using System.Numerics;
using Walgelijk;
using Walgelijk.Onion;
using Walgelijk.Physics;
using Walgelijk.SimpleDrawing;

namespace MIR.LevelEditor.Objects;

/// <summary>
/// A wall represented by a <see cref="Rect"/>.
/// </summary>
public class RectWall : RectangleObject
{
    public BlockerType BlockerType;

    public RectWall(LevelEditor.LevelEditorComponent editor, Rect rect) : base(editor)
    {
        Rectangle = rect;
    }

    public override object Clone()
    {
        return new RectWall(Editor, Rectangle) { BlockerType = BlockerType };
    }

    public override void ProcessInEditor(Scene scene, InputState input)
    {
        if (ProcessRectangle(input))
            Editor.UpdateFloorLine();

        var isSelected = Editor.SelectionManager.SelectedObject == this;

        Draw.OutlineColour = isSelected || Editor.SelectionManager.IsHovering(this) ? Colors.White : BlockerType.GetColour();
        Draw.OutlineWidth = (isSelected ? 2 : 1) * Editor.PixelSize;
        Draw.Colour = BlockerType.GetColour().WithAlpha(0.2f);
        Draw.Quad(Rectangle.TopLeft, Rectangle.GetSize(), 0, 0);
    }

    public override void ProcessPropertyUi()
    {
        Ui.Layout.FitWidth().Height(32);
        if (Ui.EnumDropdown(ref BlockerType))
            Editor.Dirty = true;
    }

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
        phy.FilterBits = BlockerType.GetFilterMask();
        phy.Collider = new RectangleCollider(transform, Vector2.One);

        if (Tag.HasValue)
            scene.SetTag(wallEntity, Tag.Value);
    }

    public override float? GetFloorPointAt(float x)
    {
        if (x < Rectangle.MinX || x > Rectangle.MaxX)
            return null;

        return Rectangle.MaxY;
    }

    public override bool IsFloor() => BlockerType is BlockerType.All or BlockerType.Characters;
}
