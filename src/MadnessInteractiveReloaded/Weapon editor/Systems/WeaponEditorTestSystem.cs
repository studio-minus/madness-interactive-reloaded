using Walgelijk;
using Walgelijk.Onion;

namespace MIR;

/// <summary>
/// For the scene where we test the weapon we're editing.
/// </summary>
public class WeaponEditorTestSystem : Walgelijk.System
{
    public override void Update()
    {
        Ui.Layout.Size(120, 32).StickTop().StickLeft();
        if (Ui.ClickButton("Back to editor") || Input.IsKeyReleased(Key.F5))
            Game.Main.Scene = WeaponEditorScene.Load(Scene.Game);
    }
}
