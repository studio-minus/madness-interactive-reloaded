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
        public float StartDelay = 0;
        public int SequenceID = -1; // Track which bullet sequence this belongs to

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
        public float StartDelay = 0;

        public TracerRenderTask(Material material)
        {
            Material = material;
        }

        public void Execute(IGraphics graphics)
        {
            const float RefDist = 5000;

            // Skip if not ready to show
            float adjustedTime = Time - StartDelay;
            if (adjustedTime < 0) return;

            var dist = Vector2.Distance(To, From);
            float p = Utilities.Clamp(adjustedTime / Duration / (dist / RefDist));
            var a = Vector2.Lerp(From, To, p);
            var b = Vector2.Lerp(From, To, Utilities.Clamp(p + TracerLength));

            var delta = (b - a);
            var length = delta.Length();

            var angle = MathF.Atan2(delta.Y, delta.X);
            var transform = Matrix3x2.CreateScale(length, float.Max(3, TracerWidth)) * 
                          Matrix3x2.CreateRotation(angle) * 
                          Matrix3x2.CreateTranslation(a.X, a.Y);

            graphics.CurrentTarget.ModelMatrix = new Matrix4x4(transform);
            graphics.Draw(PrimitiveMeshes.Quad, Material);
        }
    }

    private readonly Tracer?[] tracers = new Tracer?[MaxTracerCount];
    private int currentSequenceID = 0;
    private float currentSequenceDelay = 0;
    private const float BaseSpeed = 15000f; // Units per second

    public const int MaxTracerCount = 64;
    public const float Duration = 0.07f;

    public Material TracerMaterial = new Material(Material.DefaultTextured);

    public override void Initialise()
    {
        TracerMaterial.SetUniform(ShaderDefaults.MainTextureUniform, Texture.White);
        ResetSequence();
    }

    private void ResetSequence()
    {
        currentSequenceID++;
        currentSequenceDelay = 0;
        if (currentSequenceID > 10000) currentSequenceID = 0; // Prevent potential overflow
    }

    /// <summary>
    /// Request a tracer be rendered.
    /// </summary>
    public void ShowTracer(Vector2 from, Vector2 to, float width = 1)
    {
        var i = GetAvailable();
        if (i == null)
        {
            ResetSequence(); // If we can't get a tracer, start fresh
            return;
        }

        float dist = Vector2.Distance(to, from);
        
        // Calculate delay based on distance from start
        float travelTime = dist / BaseSpeed;
        
        i.Lifetime = Duration;
        i.From = from;
        i.To = to;
        i.Width = width;
        i.StartDelay = currentSequenceDelay;
        i.SequenceID = currentSequenceID;
        i.RenderTask.StartDelay = currentSequenceDelay;
        
        // Update sequence delay for next segment
        currentSequenceDelay += travelTime;
    }

    public override void Render()
    {
        bool hasActiveTracers = false;
        
        for (int i = 0; i < MaxTracerCount; i++)
        {
            var tracer = tracers[i];

            if (tracer?.ShouldBeRendered ?? false)
            {
                hasActiveTracers = true;
                tracer.Lifetime -= Time.DeltaTime;
                tracer.RenderTask.To = tracer.To;
                tracer.RenderTask.From = tracer.From;
                tracer.RenderTask.Time = (Duration - tracer.Lifetime);
                tracer.RenderTask.TracerLength = Time.TimeScale;
                tracer.RenderTask.TracerWidth = tracer.Width;
                RenderQueue.Add(tracer.RenderTask);
            }
        }

        // Reset sequence when all tracers are done
        if (!hasActiveTracers)
        {
            ResetSequence();
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
