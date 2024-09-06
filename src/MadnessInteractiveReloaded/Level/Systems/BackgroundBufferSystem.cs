using Walgelijk;
using Walgelijk.SimpleDrawing;

namespace MIR;

/// <summary>
/// Renders <see cref="BackgroundBufferDrawerComponent"/> to <see cref="BackgroundBufferStorageComponent"/>
/// </summary>
public class BackgroundBufferSystem : Walgelijk.System
{
    public override void PostRender()
    {
        // TODO this should ideally be a rendertask

        var oldTarget = Graphics.CurrentTarget;
        var projMatrix = oldTarget.ProjectionMatrix;
        var viewMatrix = oldTarget.ViewMatrix;
        var modelMatrix = oldTarget.ModelMatrix;

        // this is definitely not recommended. we might be throwing away rendertasks that aren't ours to touch.
        RenderQueue.RenderAndReset(Graphics);

        foreach (var buffer in Scene.GetAllComponentsOfType<BackgroundBufferStorageComponent>())
        {
            Graphics.CurrentTarget = buffer.Buffer;
            Graphics.Clear(Colors.Transparent);

            Draw.Reset();
            Draw.BlendMode = BlendMode.Overwrite;
            Draw.Material = buffer.Material;

            buffer.Buffer.ProjectionMatrix = projMatrix;
            buffer.Buffer.ViewMatrix = viewMatrix;
            buffer.Buffer.ModelMatrix = modelMatrix;

            foreach (var comp in Scene.GetAllComponentsOfType<BackgroundBufferDrawerComponent>())
            {
                Draw.Colour = Colors.White;
                Draw.Texture = comp.Texture;
                Draw.Quad(comp.WorldRect);
            }

            RenderQueue.RenderAndReset(Graphics);
        }

        Graphics.CurrentTarget = oldTarget;
    }

#if false
    public override void Render()
    {
        Draw.Reset();
        Draw.ScreenSpace = true;
        Draw.Order = RenderOrders.UserInterface;
        foreach (var buffer in Scene.GetAllComponentsOfType<DecalMaskBufferComponent>())
        {
            Draw.Image(buffer.Buffer, new Rect(0, 0, 256, 256),
                Walgelijk.SimpleDrawing.ImageContainmentMode.Stretch);
        }
    }
#endif
}
