using System.Runtime.CompilerServices;
using Walgelijk.Onion.Controls;
using Walgelijk.Onion;
using Walgelijk.SimpleDrawing;
using Walgelijk;
using Walgelijk.Onion.Layout;
using System.Numerics;

namespace MIR.Controls;

public readonly struct LeftAlignedButton : IControl
{
    public static bool Hold(string label, int identity = 0, [CallerLineNumber] int site = 0)
        => Start(label, identity, site).Held;

    public static bool Click(string label, int identity = 0, [CallerLineNumber] int site = 0)
        => Start(label, identity, site).Up;

    public static InteractionReport Start(string label, int identity = 0, [CallerLineNumber] int site = 0)
    {
        var (instance, node) = Onion.Tree.Start(IdGen.Create(nameof(LeftAlignedButton).GetHashCode(), identity, site), new LeftAlignedButton());
        instance.RenderFocusBox = false;
        instance.Name = label;
        Onion.Tree.End();
        return new InteractionReport(instance, node, InteractionReport.CastingBehaviour.Up);
    }

    public void OnAdd(in ControlParams p) { }

    public void OnRemove(in ControlParams p) { }

    public void OnStart(in ControlParams p) { }

    public void OnProcess(in ControlParams p) => ControlUtils.ProcessButtonLike(p);

    public void OnRender(in ControlParams p)
    {
        (ControlTree tree, LayoutQueue layout, Input input, GameState state, Node node, ControlInstance instance) = p;

        var t = node.GetAnimationTime();
        var anim = instance.Animations;

        var fg = p.Theme.Foreground[instance.State];
        Draw.Colour = fg.Color;
        Draw.Texture = fg.Texture;
        Draw.ImageMode = fg.ImageMode;
        Draw.OutlineColour = p.Theme.OutlineColour[instance.State];
        Draw.OutlineWidth = p.Theme.OutlineWidth[instance.State];

        anim.AnimateColour(ref Draw.OutlineColour, t);
        anim.AnimateRect(ref instance.Rects.Rendered, t);
        anim.AnimateColour(ref Draw.Colour, t);

        Draw.Quad(instance.Rects.Rendered, 0, p.Theme.Rounding);
        Draw.ResetTexture();

        Draw.Font = p.Theme.Font;
        Draw.Colour = p.Theme.Text[instance.State];
        anim.AnimateColour(ref Draw.Colour, t);

        if (anim.ShouldRenderText(t))
        {
            var ratio = instance.Rects.Rendered.Area / instance.Rects.ComputedGlobal.Area;
            Draw.Text(instance.Name, 
                new Vector2(instance.Rects.Rendered.MinX + p.Theme.Padding, 
                instance.Rects.Rendered.GetCenter().Y), new Vector2(ratio),
                HorizontalTextAlign.Left, VerticalTextAlign.Middle);
        }
    }

    public void OnEnd(in ControlParams p)
    {
    }
}
