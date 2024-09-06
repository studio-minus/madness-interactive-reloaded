using System.Numerics;
using System.Runtime.CompilerServices;
using Walgelijk;
using Walgelijk.Onion;
using Walgelijk.Onion.Controls;
using Walgelijk.Onion.Layout;
using Walgelijk.SimpleDrawing;

namespace MIR.Controls;

public readonly struct LayeredImageButton : IControl
{
    public readonly IReadableTexture Foreground, Background;

    public LayeredImageButton(IReadableTexture bg, IReadableTexture fg)
    {
        Background = bg;
        Foreground = fg;
    }

    public static InteractionReport Start(IReadableTexture bg, IReadableTexture fg, int identity = 0, [CallerLineNumber] int site = 0)
    {
        var (instance, node) = Onion.Tree.Start(IdGen.Create(nameof(LayeredImageButton).GetHashCode(), identity, site), new LayeredImageButton(bg, fg));
        instance.RenderFocusBox = false;
        Onion.Tree.End();
        return new InteractionReport(instance, node, InteractionReport.CastingBehaviour.Up);
    }

    public void OnAdd(in ControlParams p) { }

    public void OnRemove(in ControlParams p) { }

    public void OnStart(in ControlParams p)
    {
    }

    public void OnProcess(in ControlParams p)
    {
        ControlUtils.ProcessButtonLike(p);

        //if (instance.State.HasFlag(ControlState.Hover))
        //    IControl.SetCursor(DefaultCursor.Pointer);
    }

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

        anim.AnimateRect(ref instance.Rects.Rendered, t);
        anim.AnimateColour(ref Draw.OutlineColour, t);
        anim.AnimateColour(ref Draw.Colour, t);

        Draw.ResetMaterial();
        Draw.ImageMode = default;
        Draw.OutlineWidth = 0;

        Draw.ImageMode = ImageMode.Slice;
        Draw.Image(Background, instance.Rects.Rendered, ImageContainmentMode.Stretch, 0, p.Theme.Rounding);

        Draw.Colour = p.Theme.Image[instance.State];
        anim.AnimateColour(ref Draw.Colour, t);
        Draw.ImageMode = ImageMode.Stretch;
        Draw.Image(Foreground, instance.Rects.Rendered, ImageContainmentMode.Center, 0, p.Theme.Rounding);
        Draw.ResetTexture();
    }

    public void OnEnd(in ControlParams p)
    {
    }
}
