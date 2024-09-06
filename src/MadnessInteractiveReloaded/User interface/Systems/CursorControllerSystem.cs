using System.Numerics;
using Walgelijk;
using Walgelijk.SimpleDrawing;

namespace MIR;

public class CursorControllerSystem : Walgelijk.System
{
    public override void Update()
    {
        // TODO

        //return;
        //if (!Window.HasFocus || (!Scene.FindAnyComponent<GameModeComponent>(out var gm) || gm.Mode == GameMode.Unknown) || MadnessUtils.IsPaused(Scene) || MadnessUtils.EditingInExperimentMode(Scene) || Gui.Context.IsUiBeingUsed())
        //    Window.IsCursorLocked = false;
        //else
        //{
        //    Draw.Reset();
        //    Draw.ScreenSpace = true;
        //    Draw.Order = RenderOrders.Imgui;
        //    Draw.Colour = Colors.Red;
        //    Draw.Circle(Input.WindowMousePosition, new Vector2(8));
        //    Window.IsCursorLocked = true;
        //}
    }
}