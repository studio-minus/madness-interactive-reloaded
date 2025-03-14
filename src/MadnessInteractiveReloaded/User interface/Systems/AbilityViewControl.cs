using Walgelijk.Onion;
using Walgelijk.SimpleDrawing;
using Walgelijk;
using System.Numerics;
using Walgelijk.Onion.Controls;
using System.Runtime.CompilerServices;
using Walgelijk.Onion.Layout;

namespace MIR;

public readonly struct AbilityViewControl(AbilityDescriptor AbilityDescriptor) : IControl
{
    public static void Start(AbilityDescriptor desc, int identity = 0, [CallerLineNumber] int site = 0)
    {
        var (instance, node) = Onion.Tree.Start(IdGen.Create(nameof(AbilityViewControl).GetHashCode(), identity, site), new AbilityViewControl(desc));
        instance.RenderFocusBox = false;
        Onion.Tree.End();
    }

    public void OnAdd(in ControlParams p)
    {
    }

    public void OnStart(in ControlParams p)
    {
        p.Instance.Rects.Local = new Rect(0, 0, 1, 1);
        p.Instance.CaptureFlags = CaptureFlags.None;
        p.Instance.Rects.Raycast = null;
        p.Instance.Rects.DrawBounds = null;
    }

    public void OnProcess(in ControlParams p)
    {
        p.Instance.Rects.Rendered = p.Instance.Rects.ComputedGlobal;
        p.Instance.CaptureFlags = CaptureFlags.Hover;
        p.Instance.Rects.Raycast = p.Instance.Rects.Rendered;
        p.Instance.Rects.DrawBounds = p.Instance.Rects.Rendered;
    }

    public void OnRender(in ControlParams p)
    {
        (ControlTree tree, LayoutQueue layout, Input input, GameState state, Node node, ControlInstance instance) = p;

        instance.Rects.Rendered = instance.Rects.ComputedGlobal;
        var t = node.GetAnimationTime();

        if (t <= float.Epsilon)
            return;

        var anim = instance.Animations;

        var fg = p.Theme.Foreground[ControlState.None];
        Draw.Colour = Colors.Transparent;
        Draw.OutlineColour = Colors.White.WithAlpha(0.5f);
        Draw.OutlineWidth = 4;

        anim.AnimateRect(ref instance.Rects.Rendered, t);
        anim.AnimateColour(ref Draw.Colour, t);
        anim.AnimateColour(ref Draw.OutlineColour, t);

        ref var r = ref instance.Rects.Rendered;

        Draw.Quad(r, 0, 10);

        Draw.Colour = Color.White;
        Draw.FontSize = 16;
        Draw.Text(AbilityDescriptor.Name, new Vector2(r.MinX + 10, r.MinY + 12), Vector2.One, textBoxWidth: r.Width - 20);

        Draw.Colour = Color.White.WithAlpha(0.8f);
        Draw.FontSize = 14;
        var h = Draw.CalculateTextHeight(AbilityDescriptor.Description, r.Width - 15) + 5;
        Draw.Text(AbilityDescriptor.Description, new Vector2(r.MinX + 10, r.MinY + 5 + 32), Vector2.One, textBoxWidth: r.Width - 20);

        instance.PreferredHeight = h + 32 + 10;
    }

    public void OnEnd(in ControlParams p) { }

    public void OnRemove(in ControlParams p) { }
}

