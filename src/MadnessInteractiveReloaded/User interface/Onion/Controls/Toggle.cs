using System.Runtime.CompilerServices;
using Walgelijk.Onion.Controls;
using Walgelijk.Onion;
using Walgelijk.SimpleDrawing;
using Walgelijk;
using Walgelijk.Onion.Layout;
using Walgelijk.Onion.Assets;

namespace MIR.Controls;

public readonly struct Toggle(bool value) : IControl
{
    public static bool Hold(ref bool value, int identity = 0, [CallerLineNumber] int site = 0)
        => Start(ref value, identity, site).Held;

    public static bool Click(ref bool value, int identity = 0, [CallerLineNumber] int site = 0)
        => Start(ref value, identity, site).Up;

    public static InteractionReport Start(ref bool value, int identity = 0, [CallerLineNumber] int site = 0)
    {
        var (instance, node) = Onion.Tree.Start(IdGen.Create(nameof(Toggle).GetHashCode(), identity, site), new Toggle(value));
        instance.RenderFocusBox = false;
        Onion.Tree.End();
        var b = new InteractionReport(instance, node, InteractionReport.CastingBehaviour.Up);

        if (b)
            value = !value;

        return b;
    }

    public void OnAdd(in ControlParams p) { }

    public void OnRemove(in ControlParams p) { }

    public void OnStart(in ControlParams p) { }

    public void OnProcess(in ControlParams p) => ControlUtils.ProcessButtonLike(p);

    public void OnRender(in ControlParams p)
    {
        (ControlTree tree, LayoutQueue layout, Input input, GameState state, Node node, ControlInstance instance) = p;

        var v = value;
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

        if (v)
        {
            var checkBoxRect = instance.Rects.Rendered.Expand(-p.Theme.Padding);
            Draw.ImageMode = default;
            Draw.OutlineWidth = 0;
            Draw.Colour = p.Theme.Accent[p.Instance.State];
            anim.AnimateColour(ref Draw.Colour, t);
            Draw.Colour.A *= 0.1f;
            Draw.Quad(checkBoxRect, 0, p.Theme.Rounding);
            Draw.Colour.A /= 0.1f;
            Draw.Image(BuiltInAssets.Icons.Check, checkBoxRect.Expand(-p.Theme.Padding / 4), ImageContainmentMode.Contain, 0, p.Theme.Rounding);
        }
        else
        {
            var checkBoxRect = instance.Rects.Rendered.Expand(-p.Theme.Padding);
            Draw.Colour = p.Theme.Accent[default].Saturation(0.2f).WithAlpha(0.3f);
            Draw.OutlineColour = default;
            Draw.Image(BuiltInAssets.Icons.Check, checkBoxRect.Expand(-p.Theme.Padding / 4), ImageContainmentMode.Contain, 0, p.Theme.Rounding);
        }
    }

    public void OnEnd(in ControlParams p)
    {
    }
}
