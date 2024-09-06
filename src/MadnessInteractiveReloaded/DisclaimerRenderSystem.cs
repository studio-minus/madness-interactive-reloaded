using System.Numerics;
using Walgelijk;
using Walgelijk.SimpleDrawing;

namespace MIR;

internal class DisclaimerRenderSystem : Walgelijk.System
{
    public override void Update()
    {
        Draw.Reset();
        Draw.Font = Font.Default;
        Draw.Order = RenderOrders.UserInterfaceTop;
        Draw.ScreenSpace = true;
        Draw.Colour = Colors.White.WithAlpha(0.2f);

        Draw.Text("BETA BUILD - YOU WILL ENCOUNTER BUGS AND UNFINISHED CONTENT\nPlease contribute to the repository :)", 
            new Vector2(Window.Width * 0.5f, 10), Vector2.One, HorizontalTextAlign.Center, VerticalTextAlign.Top);
    }
}