using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using System.Numerics;
using Walgelijk;
using Walgelijk.Onion;
using Walgelijk.Onion.Controls;
using Walgelijk.SimpleDrawing;

namespace MIR.LevelEditor.Objects;

/// <summary>
/// A C# script that will be executed in the level
public class LevelScript : LevelObject, ITagged
{
    public const float Radius = 48;

    public Vector2 Position;
    public string Name = "untitled";
    public string Code;

    public Tag? Tag { get; set; }

    public LevelScript(LevelEditorComponent editor, Vector2 pos) : base(editor)
    {
        Code =
@"/*
this.Game
this.Scene
this.Input
this.Time
this.Audio
this.Window
this.Graphics
this.DebugDraw
*/

void Start()
{
	
}

void Update()
{
	
}

void FixedUpdate()
{
	
}

void Render()
{
	
}

void End()
{
	
}

";
        Position = pos;
    }

    public override object Clone()
    {
        return new LevelScript(Editor, Position) { Code = Code, Name = Name };
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

        Draw.Colour = (isSelected || isHover ? Colors.Red : Colors.White);

        Draw.Texture = Textures.UserInterface.EditorScriptInstance.Value;
        Draw.Quad(Position + new Vector2(Radius / -2, Radius / 2), new Vector2(Radius));

        Draw.Colour = isSelected || isHover ? Colors.Red : Colors.White;
        Draw.FontSize = 18;
        Draw.Font = Fonts.Inter;
        Draw.Text(Name, Position + new Vector2(0, -Radius), Vector2.One, HorizontalTextAlign.Center, VerticalTextAlign.Bottom);
    }

    public override void ProcessPropertyUi()
    {
        Ui.Label("Name");
        Ui.Layout.FitWidth(false).Height(32);
        Ui.StringInputBox(ref Name, TextBoxOptions.TextInput);
        Ui.Spacer(8);
        Ui.Layout.FitWidth(false).Height(32);
        Ui.StartGroup(false);
        {
            Ui.Layout.FitContainer(1 / 3f, 1, true).StickLeft(false);
            if (Ui.Button("Copy"))
                TextCopy.ClipboardService.SetText(Code);
            Ui.Layout.FitContainer(1 / 3f, 1, true).StickRight(false);
            if (Ui.Button("Paste"))
            {
                var previous = Code;
                Code = (TextCopy.ClipboardService.GetText() ?? string.Empty).Normalize().Trim();

                // all this code is doing is checking for syntax errors
                var tree = CSharpSyntaxTree.ParseText(Code, CSharpParseOptions.Default.WithKind(SourceCodeKind.Script));
                var diagnostics = tree.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error);
                if (diagnostics.Any())
                    Game.Main.Scene.AttachComponent(Game.Main.Scene.CreateEntity(), new ConfirmationDialogComponent(
                        "Script syntax error",
                        string.Join('\n', diagnostics) + "\n\nRevert changes?", () => { Code = previous; }));
            }
            Ui.Layout.FitContainer(1 / 3f, 1, true).CenterHorizontal();
            if (Ui.Button("Clear"))
                Code = string.Empty;
        }
        Ui.End();
        Ui.Layout.FitWidth(false).PreferredSize();
        Ui.Theme.Font(Fonts.CascadiaMono).FontSize(12).Once();
        Ui.TextRect(Code, HorizontalTextAlign.Left, VerticalTextAlign.Top);
    }

    public override void SpawnInGameScene(Scene scene)
    {
        var ent = scene.CreateEntity();
        if (Tag.HasValue)
            scene.SetTag(ent, Tag.Value);
        scene.AttachComponent(ent, new LevelScriptComponent
        {
            Code = Code,
            Name = Name,
            Enabled = true,
        });
    }
}
