using MIR.LevelEditor;
using System.Numerics;
using Walgelijk;
using Walgelijk.SimpleDrawing;

namespace MIR;

internal class DisclaimerRenderSystem : Walgelijk.System
{
    public override void Update()
    {
        return;
        Draw.Reset();
        Draw.Font = Fonts.Oxanium;
        Draw.Order = RenderOrders.UserInterfaceTop;
        Draw.ScreenSpace = true;
        Draw.FontSize = 18;
        Draw.Colour = ((Color)Utilities.Lerp(Colors.Red, Colors.White, Easings.Expo.Out((Time.SecondsSinceLoadUnscaled * 0.1f) % 1))).WithAlpha(0.9f);

        var c = new Vector2(Window.Width * 0.5f, 10);

        if (Scene.HasSystem<LevelEditorSystem>())
            c.Y += 32;

        Draw.Text("BETA BUILD - YOU WILL ENCOUNTER BUGS, UNFINISHED CONTENT, LAG, AND CRASHING\nPlease contribute to the repository :)", 
            c, Vector2.One, HorizontalTextAlign.Center, VerticalTextAlign.Top);
    }
}