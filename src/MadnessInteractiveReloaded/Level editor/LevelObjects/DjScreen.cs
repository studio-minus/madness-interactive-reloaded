using Walgelijk;
using Walgelijk.SimpleDrawing;

namespace MIR.LevelEditor.Objects;

/// <summary>
/// The screen for the music visualiser.
/// </summary>
public class DjScreen : RectangleObject
{
    private static readonly Texture Placeholder = TexGen.Checkerboard(64, 64, 4, Colors.White, Colors.Black);

    public DjScreen(LevelEditor.LevelEditorComponent editor, Rect rectangle) : base(editor)
    {
        Rectangle = rectangle;
    }

    public override void ProcessInEditor(Scene scene, InputState input)
    {
        ProcessRectangle(input);
        var isSelected = Editor.SelectionManager.SelectedObject == this;

        Draw.Texture = Placeholder;
        Draw.OutlineColour = Colors.Red;
        Draw.OutlineWidth = isSelected ? 4 * Editor.PixelSize : 0;
        Draw.Colour = (Editor.SelectionManager.HoveringObject == this || isSelected) ? Colors.White.WithAlpha(.4f) : Colors.White.WithAlpha(0.2f);
        Draw.Quad(Rectangle, 0, 0);
    }

    public override void ProcessPropertyUi() { }

    public override void SpawnInGameScene(Scene scene)
    {
        var ent = scene.CreateEntity();
        scene.AttachComponent(ent, new DjScreenComponent(Rectangle));

        if (Tag.HasValue)
            scene.SetTag(ent, Tag.Value);
    }

    public override object Clone()
    {
        return new DjScreen(Editor, Rectangle);
    }
}
