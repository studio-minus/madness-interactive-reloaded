using System.Numerics;
using Walgelijk;
using Walgelijk.SimpleDrawing;

namespace MIR;

/// <summary>
/// Renders <see cref="BackgroundBufferDrawerComponent"/> to <see cref="BackgroundBufferStorageComponent"/>
/// </summary>
public class BackgroundBufferSystem : Walgelijk.System
{
    public override void Render()
    {
        BackgroundBufferTask.Shared.Scene = Scene;
        RenderQueue.Add(BackgroundBufferTask.Shared, RenderOrder.CameraOperations);
    }

    private class BackgroundBufferTask : IRenderTask
    {
        public Scene? Scene;

        public static BackgroundBufferTask Shared { get; } = new();

        public void Execute(IGraphics graphics)
        {
            if (Scene == null)
                return;

            var oldTarget = graphics.CurrentTarget;

            var projMatrix = oldTarget.ProjectionMatrix;
            var viewMatrix = oldTarget.ViewMatrix;
            var modelMatrix = oldTarget.ModelMatrix;

            foreach (var buffer in Scene.GetAllComponentsOfType<BackgroundBufferStorageComponent>())
            {
                graphics.CurrentTarget = buffer.Buffer;
                graphics.Clear(Colors.Transparent);

                buffer.Buffer.ProjectionMatrix = projMatrix;
                buffer.Buffer.ViewMatrix = viewMatrix;
                buffer.Buffer.ModelMatrix = modelMatrix;

                foreach (var comp in Scene.GetAllComponentsOfType<BackgroundBufferDrawerComponent>())
                {
                    var rect = comp.WorldRect;

                    graphics.CurrentTarget.ModelMatrix = 
                        new Matrix4x4(
                            Matrix3x2.CreateScale(rect.Width, rect.Height) *
                            Matrix3x2.CreateTranslation(rect.GetCenter())
                        );

                    buffer.Material.SetUniform("mainTex", comp.Texture);
                    graphics.Draw(PrimitiveMeshes.CenteredQuad, buffer.Material);
                }
            }

            graphics.CurrentTarget = oldTarget;
            Scene = null;
        }
    }
}
