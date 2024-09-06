using Walgelijk;
using Walgelijk.SimpleDrawing;

namespace MIR.LevelEditor.Objects;

/// <summary>
/// Make the train look like it's moving really fast.
/// </summary>
public class TrainMovingBackground : RectangleObject
{
    /// <summary>
    /// How fast does the background scroll?
    /// </summary>
    public float Speed;

    public TrainMovingBackground(LevelEditor.LevelEditorComponent editor, Rect rect) : base(editor)
    {
        Rectangle = rect;
    }

    public override object Clone()
    {
        return new TrainMovingBackground(Editor, Rectangle) { Speed = Speed };
    }

    public override void ProcessInEditor(Scene scene, InputState input)
    {
        ProcessRectangle(input);
        var isSelected = Editor.SelectionManager.SelectedObject == this;
        var order = Draw.Order;

        Draw.Colour = Colors.White;
        Draw.Material = Materials.TrainMovingBackground;
        Draw.Texture = Textures.MovingView.Value;
        Draw.Order = RenderOrders.BackgroundBehind.WithOrder(-1);
        Draw.Quad(Rectangle.TopLeft, Rectangle.GetSize(), 0, 0);

        Draw.OutlineColour = isSelected ? Colors.White : Colors.Transparent;
        Draw.OutlineWidth = isSelected || Editor.SelectionManager.HoveringObject == this ? 2 * Editor.PixelSize : 0;
        Draw.Colour = Colors.Transparent;
        Draw.Order = order;
        Draw.ResetMaterial();
        Draw.ResetTexture();
        Draw.Quad(Rectangle.TopLeft, Rectangle.GetSize(), 0, 0);
    }

    public override void SpawnInGameScene(Scene scene)
    {
        var ent = scene.CreateEntity();
        scene.AttachComponent(ent, new TransformComponent
        {
            Position =
            Rectangle.GetCenter(),
            Scale = Rectangle.GetSize()
        });
        scene.AttachComponent(ent, new QuadShapeComponent(true)
        {
            Material = Materials.TrainMovingBackground,
            RenderOrder = RenderOrders.BackgroundBehind.WithOrder(-10)
        });
        //RenderOrder = RenderOrders.UserInterface });

        if (Tag.HasValue)
            scene.SetTag(ent, Tag.Value);
    }

    public override void ProcessPropertyUi() { }
}