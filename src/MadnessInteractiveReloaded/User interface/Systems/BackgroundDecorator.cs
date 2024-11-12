using Walgelijk;
using Walgelijk.Onion.Controls;
using Walgelijk.Onion.Decorators;
using Walgelijk.SimpleDrawing;

namespace MIR;

internal struct BackgroundDecorator : IDecorator
{
    public void RenderAfter(in ControlParams p)
    {
        var b = p.Theme.Background[p.Instance.State];
        Draw.Order = Draw.Order.OffsetOrder(-1);
        Draw.Texture = b.Texture ?? Texture.White;
        Draw.Colour = b.Color;
        Draw.Quad(p.Instance.Rects.Rendered);
    }

    public void RenderBefore(in ControlParams p)
    {
    }
}