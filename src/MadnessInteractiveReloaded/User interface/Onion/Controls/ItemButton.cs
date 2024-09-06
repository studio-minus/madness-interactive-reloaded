using System.Runtime.CompilerServices;
using Walgelijk.Onion.Controls;
using Walgelijk.Onion;
using Walgelijk.SimpleDrawing;
using Walgelijk;
using Walgelijk.Onion.Layout;
using Walgelijk.AssetManager;

namespace MIR.Controls;

public readonly struct ItemButton : IControl
{
    public readonly IReadableTexture? Texture;

    public ItemButton(IReadableTexture? texture)
    {
        Texture = texture;
    }

    public static bool Hold(IReadableTexture? texture, int identity = 0, [CallerLineNumber] int site = 0)
    {
        return Start(texture, identity, site).Held;
    }

    public static bool Click(IReadableTexture? texture, int identity = 0, [CallerLineNumber] int site = 0)
    {
        return Start(texture, identity, site).Up;
    }

    public static InteractionReport Start(IReadableTexture? texture, int identity = 0, [CallerLineNumber] int site = 0)
    {
        var (instance, node) = Onion.Tree.Start(IdGen.Create(nameof(ItemButton).GetHashCode(), identity, site), new ItemButton(texture));
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
        //laat dit werken
        //ControlUtils.ProcessDraggable(p, p.Instance.Rects.ComputedGlobal);
    }

    public void OnRender(in ControlParams p)
    {
        (ControlTree tree, LayoutQueue layout, Input input, GameState state, Node node, ControlInstance instance) = p;

        var t = node.GetAnimationTime();
        var anim = instance.Animations;

        Draw.Colour = p.Theme.Image[instance.State];
        Draw.ImageMode = ImageMode.Stretch;
        Draw.OutlineColour = p.Theme.OutlineColour[instance.State];
        Draw.OutlineWidth = p.Theme.OutlineWidth[instance.State];

        anim.AnimateRect(ref instance.Rects.Rendered, t);
        anim.AnimateColour(ref Draw.Colour, t);

        Draw.Texture = Assets.Load<Texture>("textures/ui/item_background.png").Value;
        Draw.Quad(instance.Rects.Rendered, 0, p.Theme.Rounding);

        if (Texture != null)
        {
            Draw.ResetMaterial();
            Draw.OutlineWidth = 0;
            Draw.ImageMode = default;
            Draw.Colour = p.Theme.Image[instance.State];
            anim.AnimateColour(ref Draw.Colour, t);
            Draw.Image(Texture, instance.Rects.Rendered.Expand(-4), ImageContainmentMode.Contain, 0, p.Theme.Rounding);
            Draw.ResetTexture();
            Draw.OutlineWidth = 0;
        }
    }

    public void OnEnd(in ControlParams p)
    {
    }
}
