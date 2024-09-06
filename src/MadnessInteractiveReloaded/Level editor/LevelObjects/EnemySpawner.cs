using System.Numerics;
using Walgelijk;
using Walgelijk.SimpleDrawing;

namespace MIR.LevelEditor.Objects;

/// <summary>
/// For spawning enemies in your level.
/// </summary>
public class EnemySpawner : LevelObject
{
    /// <summary>
    /// Where this object is in the level.
    /// </summary>
    public Vector2 Position;

    public EnemySpawner(LevelEditor.LevelEditorComponent editor) : base(editor)
    {
    }

    public override bool ContainsPoint(Vector2 worldPoint)
    {
        return Vector2.Distance(Position, worldPoint) < 64;
    }

    public override void SpawnInGameScene(Scene scene)
    {
        //No need because objects of this type are read and added to the level enemy list
    }

    public override object Clone()
    {
        return new EnemySpawner(Editor) { Position = Position };
    }

    public override void ProcessInEditor(Scene scene, InputState input)
    {
        ProcessDraggable(input);
        var isSelected = Editor.SelectionManager.SelectedObject == this;
        var isHover = Editor.SelectionManager.HoveringObject == this;

        Draw.Colour = (isSelected || isHover ? Colors.White : Colors.Red).WithAlpha(0.2f);

        Draw.ResetTexture();
        Draw.Circle(Position, new Vector2(32));

        Draw.Colour = isSelected || isHover ? Colors.Red : Colors.White;
        Draw.FontSize = 18;
        Draw.Font = Fonts.Inter;
        Draw.Text("Enemy\nSpawner", Position, Vector2.One, HorizontalTextAlign.Center, VerticalTextAlign.Middle);
    }

    public override void ProcessPropertyUi() { }

    public override Vector2 GetPosition() => Position;
    public override void SetPosition(Vector2 pos) => Position = pos;
}

