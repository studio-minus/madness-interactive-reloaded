using System.Numerics;
using Walgelijk;
using Walgelijk.SimpleDrawing;

namespace MIR;

/// <summary>
/// Ui for loading levels.
/// </summary>
public class LevelLoadingSystem : Walgelijk.System
{
    public override void Render()
    {
        Draw.Reset();
        Draw.ScreenSpace = true;
        Draw.Material = Materials.LoadingSpinningHead;
        Draw.Colour = Colors.White;
        Draw.Order = RenderOrders.UserInterfaceTop;
        Draw.Texture = Textures.UserInterface.LoadingFlipbook.Value;
        Materials.LoadingSpinningHead.SetUniform("progress", (Time.SecondsSinceLoad * 4) % 1f);
        var size = new Vector2(128);
        Draw.Quad(Window.Size - size - new Vector2(8), size);
    }
}
