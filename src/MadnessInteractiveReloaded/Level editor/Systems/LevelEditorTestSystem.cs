using Walgelijk;
using Walgelijk.Onion;

namespace MIR.LevelEditor;

public class LevelEditorTestSystem : Walgelijk.System
{
    public override void Update()
    {
        Ui.Layout.Size(120, 32).StickTop().CenterHorizontal();
        if (Ui.Button("Back to editor") || Input.IsKeyReleased(Key.F5))
            Game.Main.Scene = LevelEditorScene.Load(Scene.Game);
    }
}
