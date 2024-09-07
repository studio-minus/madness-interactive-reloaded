using System.Numerics;
using Walgelijk;
using Walgelijk.SimpleDrawing;

namespace MIR;

internal class DisclaimerRenderSystem : Walgelijk.System
{
    public override void Update()
    {
        Draw.Reset();
        Draw.Font = Fonts.Oxanium;
        Draw.Order = RenderOrders.UserInterfaceTop;
        Draw.ScreenSpace = true;
        Draw.FontSize = 18;
        Draw.Colour = ((Color)Utilities.Lerp(Colors.Red, Colors.White, Easings.Expo.Out((Time.SecondsSinceLoadUnscaled * 0.1f) % 1))).WithAlpha(0.5f);

        Draw.Text("BETA BUILD - YOU WILL ENCOUNTER BUGS AND UNFINISHED CONTENT\nPlease contribute to the repository :)", 
            new Vector2(Window.Width * 0.5f, 10), Vector2.One, HorizontalTextAlign.Center, VerticalTextAlign.Top);
    }
}