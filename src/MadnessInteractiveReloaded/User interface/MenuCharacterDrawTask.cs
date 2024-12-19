using System.Numerics;
using Walgelijk;

namespace MIR;

/// <summary>
/// Ensures a properly prepared render target to draw a character onto for UI elements and such
/// </summary>
public class MenuCharacterDrawTask : IRenderTask
{
    public RenderTarget? Target;
    public bool HorizontalFlip = false;

    public float Scale = 0.75f;

    public void Execute(IGraphics g)
    {
        if (Target == null)
            return;

        var ratio = float.Max(Target.Size.X, Target.Size.Y) / 512;
        float scale = ratio * Scale;

        Target.ProjectionMatrix =
            Matrix4x4.CreateScale(scale * (HorizontalFlip ? -1 : 1), scale, 1)
            * Matrix4x4.CreateTranslation(0, -Target.Size.Y / 2 + 30 * ratio, 0)
            * Matrix4x4.CreateOrthographic(Target.Size.X, Target.Size.Y, 0, 1);

        var t = g.CurrentTarget;
        g.CurrentTarget = Target;
        g.Clear(Colors.Transparent);
        g.CurrentTarget = t;
    }
}
