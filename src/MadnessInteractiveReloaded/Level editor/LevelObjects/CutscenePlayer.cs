using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Walgelijk;
using Walgelijk.Onion;
using Walgelijk.Onion.Controls;
using Walgelijk.SimpleDrawing;

namespace MIR.LevelEditor.Objects;

/// <summary>
/// A level object for playing cutscenes.
/// </summary>
public class CutscenePlayer : LevelObject, ITagged
{
    public const float Radius = 48;

    /// <summary>
    /// The cutscene registry key of the cutscene to play.
    /// </summary>
    public string CutsceneKey = string.Empty;

    /// <summary>
    /// The position of this object.
    /// </summary>
    public Vector2 Position;

    /// <summary>
    /// If the game should progress to the next level when this cutscene ends.
    /// </summary>
    public bool ProgressLevelOnEnd;

    private static readonly List<string> suggestions = new();

    public Tag? Tag { get; set; }

    public CutscenePlayer(LevelEditor.LevelEditorComponent editor) : base(editor)
    {
    }

    public override object Clone() => new CutscenePlayer(Editor) { CutsceneKey = CutsceneKey, Position = Position };

    public override bool ContainsPoint(Vector2 worldPoint) => Vector2.DistanceSquared(Position, worldPoint) < Radius * Radius;

    public override Vector2 GetPosition() => Position;

    public override void ProcessInEditor(Scene scene, InputState input)
    {
        ProcessDraggable(input);
        var isSelected = Editor.SelectionManager.SelectedObject == this;
        var isHover = Editor.SelectionManager.HoveringObject == this;

        Draw.Colour = (isSelected || isHover ? Colors.Red : Colors.White);

        Draw.Texture = Textures.UserInterface.EditorSequenceInstance.Value;
        Draw.Quad(Position + new Vector2(Radius / -2, Radius / 2), new Vector2(Radius));

        Draw.Colour = isSelected || isHover ? Colors.Red : Colors.White;
        Draw.FontSize = 18;
        Draw.Font = Fonts.Inter;
        Draw.Text(string.IsNullOrWhiteSpace(CutsceneKey) ? "invalid cutscene" : CutsceneKey, Position + new Vector2(0, -Radius), Vector2.One, HorizontalTextAlign.Center, VerticalTextAlign.Bottom);
    }

    public override void ProcessPropertyUi()
    {
        Ui.Label("Cutscene key");
        Ui.Layout.FitWidth(false).Height(32);
        if (Ui.StringInputBox(ref CutsceneKey, TextBoxOptions.TextInput))
            FindSuggestions();

        if (!Registries.Cutscenes.Has(CutsceneKey))
        {
            Ui.Spacer(8);
            //Gui.StartVerticalLayout(default, new Vector2(128), ArrayLayoutMode.Start, ArrayScaleMode.Stretch, optionalId: id);
            Ui.Layout.Height(128).FitWidth(false).VerticalLayout();
            Ui.StartScrollView(false);
            {
                int i = 0;
                foreach (var item in suggestions)
                {
                    Ui.Layout.FitWidth().Height(32).StickLeft();
                    if (Ui.Button(item, i++))
                        CutsceneKey = item;
                }
            }
            Ui.End();
        }

        Ui.Layout.FitWidth().Height(32);
        Ui.Checkbox(ref ProgressLevelOnEnd, "End on level complete");
    }

    private void FindSuggestions()
    {
        suggestions.Clear();
        suggestions.AddRange(Registries.Cutscenes.GetAllKeys().OrderByDescending(a => GetSimilarity(a, CutsceneKey)).Take(8));
    }

    private static float GetSimilarity(string str1, string str2)
    {
        str1 = str1.ToLower();
        str2 = str2.ToLower();
        int len1 = str1.Length;
        int len2 = str2.Length;
        if (len1 == 0 || len2 == 0) return 0;
        int matches = 0;
        for (int i = 0; i < len1; i++)
            if (str2.Contains(str1[i])) matches++;
        var ratio = (float)matches / str1.Length;
        if (len1 == len2 && str1 == str2) ratio = 1.0f;
        return ratio;
    }

    public override void SetPosition(Vector2 pos) => Position = pos;

    public override void SpawnInGameScene(Scene scene)
    {
        scene.AttachComponent(scene.CreateEntity(), new CutscenePlayerComponent(Registries.Cutscenes.Get(CutsceneKey), Position) { ProgressLevelOnEnd = ProgressLevelOnEnd });
    }
}
