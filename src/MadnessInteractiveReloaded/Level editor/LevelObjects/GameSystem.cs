using System;
using System.Numerics;
using Walgelijk;
using Walgelijk.Onion;
using Walgelijk.SimpleDrawing;

namespace MIR.LevelEditor.Objects;

/// <summary>
/// Place this in a level with an associated
/// <see cref="Walgelijk.System"/> derived System type (<see cref="SystemTypeName"/>)
/// and that system will automatically be instantiated into the <see cref="Scene"/>
/// when the level loads via reflection (<see cref="Activator"/>).
/// </summary>
public class GameSystem : LevelObject
{
    /// <summary>
    /// Where this object is in the level.
    /// 
    /// </summary>
    public Vector2 Position;

    /// <summary>
    /// The type name of the system to spawn.
    /// <br></br>
    /// For example:
    /// "EnemySpawningSystem"
    /// </summary>
    public string? SystemTypeName;

    public GameSystem(LevelEditorComponent editor) : base(editor)
    {
    }

    public override object Clone()
    {
        return new GameSystem(Editor) { SystemTypeName = SystemTypeName, Position = Position };
    }

    public override bool ContainsPoint(Vector2 worldPoint)
    {
        return Vector2.Distance(Position, worldPoint) < 64;
    }

    public override Vector2 GetPosition() => Position;
    public override void SetPosition(Vector2 pos) => Position = pos;

    public override void ProcessInEditor(Scene scene, InputState input)
    {
        ProcessDraggable(input);
        var isSelected = Editor.SelectionManager.SelectedObject == this;
        var isHover = Editor.SelectionManager.HoveringObject == this;

        Draw.Colour = (isSelected || isHover ? Colors.White : Colors.Yellow).WithAlpha(0.2f);

        Draw.ResetTexture();
        Draw.Circle(Position, new Vector2(64));

        Draw.Colour = isSelected || isHover ? Colors.Green : Colors.White;
        Draw.FontSize = 18;
        Draw.Font = Fonts.Inter;
        Draw.Text(string.IsNullOrWhiteSpace(SystemTypeName) ? "GAME SYSTEM" : SystemTypeName, Position, Vector2.One, HorizontalTextAlign.Center, VerticalTextAlign.Middle);
    }

    public override void ProcessPropertyUi()
    {
        int i = Array.IndexOf(Editor.GameSystemOptions, SystemTypeName);
        if (i < 0 || i >= Editor.GameSystemOptions.Length)
            i = 0;

        Ui.Layout.FitWidth(false).Height(32);
        if (Ui.Dropdown(Editor.GameSystemOptionNames, ref i))
            SystemTypeName = Editor.GameSystemOptions[i];
    }

    public override void SpawnInGameScene(Scene scene)
    {
        if (string.IsNullOrWhiteSpace(SystemTypeName))
            return;

        var type = Type.GetType(SystemTypeName, false, false);
        if (type == null)
        {
            Logger.Error("The game system that the level requested to be created could not be found");
            return;
        }

        try
        {
            var s = Activator.CreateInstance(type);
            if (s is Walgelijk.System system)
                scene.AddSystem(system);
            else throw new Exception();
        }
        catch (Exception)
        {
            throw new Exception("The game system that the level requested to be created can not exist");
        }
    }
}