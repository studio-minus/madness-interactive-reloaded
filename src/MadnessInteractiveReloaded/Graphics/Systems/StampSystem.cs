using System.Numerics;
using Walgelijk;
using Walgelijk.SimpleDrawing;

namespace MIR;

/// <summary>
/// Responsible for managing the background stamping texture for things like ragdolls
/// </summary>
public class StampSystem : Walgelijk.System
{
    public override void Initialise()
    {
        if (Scene.FindAnyComponent<StampCanvasComponent>(out var canvas))
            canvas.Clear(RenderQueue);
    }

    public override void Render()
    {
        if (Level.CurrentLevel != null && Scene.FindAnyComponent<StampCanvasComponent>(out var canvas))
        {
#if DEBUG
            //TODO dit mag niet
            if (Input.IsKeyReleased(Key.F7))
                canvas.Clear(RenderQueue);
#endif

            Draw.Reset();
            Draw.Order = RenderOrders.BackgroundDecals.OffsetOrder(5);

            var rect = Level.CurrentLevel.LevelBounds;
            float h = rect.Height;
            rect.MinY += h;
            rect.MaxY += h;
            Draw.Image(canvas.StampTexture, rect, Walgelijk.SimpleDrawing.ImageContainmentMode.Stretch);
            // Draw.Colour = Color.White.WithAlpha(0.1f);
            //  Draw.Image(Texture.ErrorTexture, rect, Walgelijk.SimpleDrawing.ImageContainmentMode.Stretch);
        }
    }
}
