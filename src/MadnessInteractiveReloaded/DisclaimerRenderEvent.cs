using System.Numerics;
using Walgelijk;

namespace MIR;

internal class DisclaimerRenderEvent : IGameLoopEvent
{
    private readonly DrawTask task = new();

    public void FixedUpdate(Game game, float interval)
    {
    }

    public void Update(Game game, float dt)
    {
        game.RenderQueue.Add(task, RenderOrder.DebugUI.OffsetLayer(1));
    }

    private class DrawTask : IRenderTask
    {
        private readonly TextMeshGenerator generator;
        private readonly VertexBuffer vb;

        public DrawTask()
        {
            generator = new TextMeshGenerator
            {
                Color = Colors.White.WithAlpha(0.2f),
                Font = Font.Default,
                HorizontalAlign = HorizontalTextAlign.Center,
                VerticalAlign = VerticalTextAlign.Top
            };

            vb = new VertexBuffer(new Vertex[1024], new uint[1024]);
        }

        public void Execute(IGraphics graphics)
        {
            var t = graphics.CurrentTarget;

            t.ViewMatrix = Matrix4x4.CreateTranslation(t.Size.X / 2, 10, 0);
            t.ProjectionMatrix = t.OrthographicMatrix;

            graphics.DrawTextScreenspace("BETA BUILD - YOU WILL ENCOUNTER BUGS AND UNFINISHED CONTENT\nPlease contribute to the repository :)", 
                default, generator, vb, Font.Default.Material!);
        }
    }
}