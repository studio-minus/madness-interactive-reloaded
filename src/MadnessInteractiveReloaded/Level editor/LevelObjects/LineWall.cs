using System.Numerics;
using Walgelijk;
using Walgelijk.Onion;
using Walgelijk.Onion.Controls;
using Walgelijk.Physics;
using Walgelijk.SimpleDrawing;

namespace MIR.LevelEditor.Objects;

/// <summary>
/// A wall made from a single line.
/// See: <see cref="LineWall"/>.
/// </summary>
public class LineWall : LineObject
{
    /// <summary>
    /// What this wall will block.
    /// </summary>
    public BlockerType BlockerType;

    public LineWall(LevelEditor.LevelEditorComponent editor, Vector2 a, Vector2 b, float radius) : base(editor, a, b, radius)
    {
    }

    public override object Clone()
    {
        return new LineWall(Editor, A, B, Radius);
    }

    public override void ProcessInEditor(Scene scene, InputState input)
    {
        base.ProcessInEditor(scene, input);

        var isSelected = Editor.SelectionManager.SelectedObject == this;

        Draw.OutlineColour = (isSelected || Editor.SelectionManager.IsHovering(this)) ? Colors.White : BlockerType.GetColour();
        Draw.OutlineWidth = (isSelected ? 2 : 1) * Editor.PixelSize;
        Draw.Colour = BlockerType.GetColour().WithAlpha(0.2f);
        Draw.Line(A, B, Radius * 2, Radius);

        Draw.OutlineWidth = 0;
        Draw.Colour = Colors.White;
        Draw.Circle(A, new Vector2(8));
        Draw.Circle(B, new Vector2(8));
        Draw.Colour = Colors.White.WithAlpha(0.5f);
        Draw.Line(A, B, 4, 4);
    }

    public override void ProcessPropertyUi()
    {
        Ui.Layout.Height(32).FitWidth(false);
        if (Ui.EnumDropdown(ref BlockerType))
            Editor.Dirty = true;
        Ui.Spacer(8);
        Ui.Label("Radius");
        Ui.Layout.Height(32).FitWidth(false);
        if (Ui.FloatSlider(ref Radius, Direction.Horizontal, (16,256), 1, "{0:#}px"))
            Editor.Dirty = true;
    }

    public override void SpawnInGameScene(Scene scene)
    {
        var wallEntity = scene.CreateEntity();
        var transform = scene.AttachComponent(wallEntity, new TransformComponent()); // (zooi): identity transform because the line points are what actually matter
        var phy = scene.AttachComponent(wallEntity, new PhysicsBodyComponent());
        phy.BodyType = BodyType.Static;
        phy.FilterBits = BlockerType.GetFilterMask();
        phy.Collider = new LineCollider(transform, A, B, Radius * 2);

        if (Tag.HasValue)
            scene.SetTag(wallEntity, Tag.Value);
    }

    public override bool IsFloor() => BlockerType is BlockerType.All or BlockerType.Characters;
}
