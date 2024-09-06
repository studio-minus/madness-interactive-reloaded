using System.Numerics;
using Walgelijk;
using Walgelijk.Physics;
using Walgelijk.SimpleDrawing;

namespace MIR.LevelEditor.Objects;

// Pizza calzone
/// <summary>
///  Define a zone where decals can be placed.
/// </summary>
public class DecalZone : RectangleObject
{
    public DecalZone(LevelEditor.LevelEditorComponent editor, Rect rectangle) : base(editor)
    {
        Rectangle = rectangle;
    }

    public override object Clone() => new DecalZone(Editor, Rectangle);

    public override void ProcessInEditor(Scene scene, InputState input)
    {
        ProcessRectangle(input);
        var isSelected = Editor.SelectionManager.SelectedObject == this;

        Draw.OutlineColour = isSelected ? Colors.White : Colors.Yellow;
        Draw.OutlineWidth = 4 * Editor.PixelSize;
        Draw.Colour = (Editor.SelectionManager.HoveringObject == this || isSelected) ? Colors.White.WithAlpha(0.4f) : Colors.Yellow.WithAlpha(0.2f);
        Draw.Quad(Rectangle, 0, 0);
    }

    public override void ProcessPropertyUi() { }

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
        phy.FilterBits = CollisionLayers.DecalZone;
        phy.Collider = new RectangleCollider(transform, Vector2.One);


        if (Tag.HasValue)
            scene.SetTag(wallEntity, Tag.Value);
    }
}
