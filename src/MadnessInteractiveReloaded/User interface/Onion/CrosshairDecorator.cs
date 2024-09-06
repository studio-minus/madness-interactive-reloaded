using System.Numerics;
using System.Reflection;
using Walgelijk;
using Walgelijk.Onion;
using Walgelijk.Onion.Controls;
using Walgelijk.Onion.Decorators;
using Walgelijk.SimpleDrawing;

namespace MIR;

public readonly struct OverlayDecorator : IDecorator
{
    public readonly Vector4 Colour;

    public OverlayDecorator(Vector4 colour)
    {
        Colour = colour;
    }

    public void RenderAfter(in ControlParams p)
    {
        var r = p.Instance.Rects.ComputedDrawBounds;

        Draw.Colour = Colour;
        Draw.OutlineWidth = 0;
        Draw.Quad(r);
    }

    public void RenderBefore(in ControlParams p)
    {
    }
}

public readonly struct CrosshairDecorator : IDecorator
{
    public void RenderAfter(in ControlParams p)
    {
        var r = p.Instance.Rects.ComputedDrawBounds;
        float expand = Utilities.MapRange(-1,1, 0, 5, float.Sin(6 * p.GameState.Time));

        Draw.Reset();
        Draw.ScreenSpace = true;
        Draw.Order = new(Onion.Configuration.RenderLayer, int.MaxValue);

        Draw.ClearMask();
        Draw.WriteMask();
        var h = p.Instance.Rects.ComputedDrawBounds.Expand(-10);
        h.MinX -= 15 + expand;
        h.MaxX += 15 + expand;
        Draw.Quad(h);
        h = p.Instance.Rects.ComputedDrawBounds.Expand(-10);
        h.MinY -= 15 + expand;
        h.MaxY += 15 + expand;
        Draw.Quad(h);

        Draw.TransformMatrix = Matrix3x2.CreateScale(
            new Vector2(
                (r.Width + expand) / r.Width,
                (r.Height + expand) / r.Height),
            p.Instance.Rects.ComputedDrawBounds.GetCenter());

        Draw.OutsideMask();
        Draw.Colour = Colors.Transparent;
        Draw.OutlineColour = Colors.Red;
        Draw.OutlineWidth = 4;
        Draw.Quad(p.Instance.Rects.ComputedDrawBounds.Expand(4));
        Draw.DisableMask();

        Draw.OutlineWidth = 0;
        Draw.Reset();
    }

    public void RenderBefore(in ControlParams p)
    {
    }
}

public readonly struct FancyButtonDecorator : IDecorator
{
    public void RenderAfter(in ControlParams p)
    {
        if (!p.Instance.IsHover)
            return;

        const float s = 8;
        float th = (p.GameState.Time * 4) + p.Node.SiblingIndex * float.Pi / 2;
        var r = p.Instance.Rects.ComputedDrawBounds;

        var spinnyVector = new Vector2(
            float.Cos(th),
            float.Sin(th)
        );
        var o = spinnyVector;

        spinnyVector.X /= float.Max(float.Abs(o.Y), float.Abs(o.X));
        spinnyVector.Y /= float.Max(float.Abs(o.Y), float.Abs(o.X));

        spinnyVector.X *= r.Width * 0.5f;
        spinnyVector.Y *= r.Height * 0.5f;

        Draw.Reset();
        Draw.ScreenSpace = true;
        Draw.Order = new(Onion.Configuration.RenderLayer, int.MaxValue / 2);

        Draw.ClearMask();
        Draw.WriteMask();
        var h = new Rect(-s, -s, s, s).Translate(spinnyVector).Translate(r.GetCenter());
        Draw.Quad(h);
        h = new Rect(-s, -s, s, s).Translate(-spinnyVector).Translate(r.GetCenter());
        Draw.Quad(h);

        Draw.InsideMask();
        Draw.Colour = Colors.Transparent;
        Draw.OutlineColour = Colors.Red;
        Draw.OutlineWidth = 4;
        Draw.Quad(p.Instance.Rects.ComputedDrawBounds.Expand(4));
        Draw.DisableMask();

        Draw.OutlineWidth = 0;
        Draw.Reset();
    }

    public void RenderBefore(in ControlParams p)
    {
    }
}
