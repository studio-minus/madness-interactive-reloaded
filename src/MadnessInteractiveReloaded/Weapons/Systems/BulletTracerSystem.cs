namespace MIR;

using System;
using System.Numerics;
using Walgelijk;

/// <summary>
/// Visual tracers for bullets.
/// </summary>
public class BulletTracerSystem : Walgelijk.System
{
    private class Tracer
    {
        public Vector2 From;
        public Vector2 To;
        public float Width = 1;
        public float Lifetime;

        public TracerRenderTask RenderTask;

        public Tracer(TracerRenderTask renderTask)
        {
            RenderTask = renderTask;
        }

        public bool CanBeReused => !ShouldBeRendered;
        public bool ShouldBeRendered => Lifetime >= 0;
    }

    private class TracerRenderTask : IRenderTask
    {
        public float Time = 0;
        public Vector2 From, To;
        public Material Material;
        public float TracerLength;
        public float TracerWidth;

        public TracerRenderTask(Material material)
        {
            Material = material;
        }

        public void Execute(IGraphics graphics)
        {
            // TODO when a bullet passes through an object, the tracers dont line up because the actual bullet impact is instant
            // TODO some of this code can be calculated once the moment this tracer is created or reinitialised

            const float RefDist = 5000;

            var dist = Vector2.Distance(To, From);
            float p = Utilities.Clamp(Time / Duration / (dist / RefDist));
            var a = Vector2.Lerp(From, To, p);
            var b = Vector2.Lerp(From, To, Utilities.Clamp(p + TracerLength));

            var delta = (b - a);
            var length = delta.Length();

            var angle = MathF.Atan2(delta.Y, delta.X);
            var transform = Matrix3x2.CreateScale(length, float.Max(3, TracerWidth)) * Matrix3x2.CreateRotation(angle) * Matrix3x2.CreateTranslation(a.X, a.Y);

            graphics.CurrentTarget.ModelMatrix = new Matrix4x4(transform);
            graphics.Draw(PrimitiveMeshes.Quad, Material);
        }
    }

    private readonly Tracer?[] tracers = new Tracer?[MaxTracerCount];

    public const int MaxTracerCount = 64;
    public const float Duration = 0.07f;

    public Material TracerMaterial = new Material(Material.DefaultTextured);

    public override void Initialise()
    {
        TracerMaterial.SetUniform(ShaderDefaults.MainTextureUniform, Texture.White);
    }

    /// <summary>
    /// Request a tracer be rendered.
    /// </summary>
    public void ShowTracer(Vector2 from, Vector2 to, float width = 1)
    {
        var i = GetAvailable();
        if (i == null)
            return;

        i.Lifetime = Duration;
        i.From = from;
        i.To = to;
        i.Width = width;
    }

    public override void Render()
    {
        for (int i = 0; i < MaxTracerCount; i++)
        {
            var tracer = tracers[i];

            if (tracer?.ShouldBeRendered ?? false)
            {
                tracer.Lifetime -= Time.DeltaTime;
                tracer.RenderTask.To = tracer.To;
                tracer.RenderTask.From = tracer.From;
                tracer.RenderTask.Time = (Duration - tracer.Lifetime);
                tracer.RenderTask.TracerLength = Time.TimeScale;
                tracer.RenderTask.TracerWidth = tracer.Width;
                RenderQueue.Add(tracer.RenderTask);
            }
        }
    }

    private Tracer? GetAvailable()
    {
        for (int i = 0; i < MaxTracerCount; i++)
        {
            var t = tracers[i];
            if (t == null)
            {
                t = new Tracer(new TracerRenderTask(TracerMaterial));
                tracers[i] = t;
                return t;
            }

            if (t.CanBeReused)
                return t;
        }

        return null;
    }
}
