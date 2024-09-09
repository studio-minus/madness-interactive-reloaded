using System;
using System.Numerics;
using Walgelijk;
using Walgelijk.SimpleDrawing;

namespace MIR;

/// <summary>
/// Component for submitting things for the <see cref="StampSystem"/>
/// to stamp.
/// </summary>
public class StampCanvasComponent : Component, IDisposable
{
    /// <summary>
    /// Width for the stamping area.
    /// </summary>
    public readonly int Width;

    /// <summary>
    /// Height for the stamping area.
    /// </summary>
    public readonly int Height;

    /// <summary>
    /// Offset the stamping area.
    /// </summary>
    public readonly Vector2 Offset;

    /// <summary>
    /// The texture the stamps get rendered to.
    /// </summary>
    public readonly RenderTexture StampTexture;

    public StampCanvasComponent(int width, int height, Vector2 offset)
    {
        Width = width;
        Height = height;
        Offset = offset;

        const int downsample = 2;

        StampTexture = new RenderTexture(width / downsample, height / downsample, flags: RenderTargetFlags.None);
        StampTexture.ViewMatrix = Matrix4x4.CreateTranslation(-Offset.X, -Offset.Y, 0);
        StampTexture.ProjectionMatrix = Matrix4x4.CreateOrthographic(Width, Height, 0, 1);
    }

    ///// <summary>
    ///// Submit things to be stamped.
    ///// </summary>
    ///// <param name="graphics"></param> 
    //public void PrepareForDrawing(IGraphics graphics)
    //{
    //    // TODO this is actually completely broken!!
    //    // we should never forcefully reset the renderqueue, but instead
    //    // submit a new render task to the queue that does everything we want to do

    //    Game.Main.RenderQueue.RenderAndReset(graphics);
    //    Draw.Reset();
    //    Draw.ScreenSpace = true;
    //    graphics.CurrentTarget = StampTexture;
    //}

    ///// <summary>
    ///// End the block of stamps.
    ///// </summary>
    ///// <param name="graphics"></param>
    //public void EndDrawing(IGraphics graphics)
    //{
    //    Game.Main.RenderQueue.RenderAndReset(graphics);
    //    graphics.CurrentTarget = Game.Main.Window.RenderTarget;
    //}

    //public void Clear(IGraphics graphics)
    //{
    //    PrepareForDrawing(graphics);
    //    graphics.Clear(Colors.Transparent);
    //    EndDrawing(graphics);
    //}

    //public void DrawTask(IGraphics graphics, IRenderTask task)
    //{
    //    PrepareForDrawing(graphics);
    //    task.Execute(graphics);
    //    EndDrawing(graphics);
    //}

    //public void DrawShape(IGraphics graphics, ShapeComponent shape)
    //{
    //    PrepareForDrawing(graphics);
    //    shape.RenderTask.Execute(graphics);
    //    EndDrawing(graphics);
    //}

    public void Dispose()
    {
        StampTexture.Dispose();
        Logger.Debug("Stamp texture disposed");
    }
}
