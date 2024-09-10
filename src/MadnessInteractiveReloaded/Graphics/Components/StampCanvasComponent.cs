using SkiaSharp;
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

    public void Clear(RenderQueue queue)
    {
        queue.Add(CreateStampTask(g => g.Clear(Colors.Transparent)));
    }

    public IRenderTask CreateStampTask(Action<IGraphics> action)
    {
        // TODO please avoid delegates
        return new ActionRenderTask(g =>
        {
            var o = g.CurrentTarget;
            g.CurrentTarget = StampTexture;
            action(g);
            g.CurrentTarget = o;
        });
    }

    public IRenderTask CreateStampTask(IRenderTask innerTask)
    {
        return new StampTask(StampTexture, innerTask);
    }

    public void Dispose()
    {
        StampTexture.Dispose();
        Logger.Debug("Stamp texture disposed");
    }

    private class StampTask(RenderTarget target, IRenderTask innerTask) : IRenderTask
    {
        public void Execute(IGraphics g)
        {
            var o = g.CurrentTarget;
            g.CurrentTarget = target;
            innerTask.Execute(g);
            g.CurrentTarget = o;
        }
    }
}
