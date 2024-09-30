using System.Runtime.CompilerServices;
using Walgelijk.Onion.Controls;
using Walgelijk.Onion.Layout;
using Walgelijk.Onion;
using Walgelijk.SimpleDrawing;
using Walgelijk;
using System.Numerics;
/// <summary>
/// For creating buttons with little pictures in them.
/// </summary>
public readonly struct ExperimentModeButton : IControl
{
    private readonly static OptionalControlState<(string, IReadableTexture)> state = new();

    public static bool Hold(string label, IReadableTexture thumbnail, int identity = 0, [CallerLineNumber] int site = 0)
        => CreateButton(label, thumbnail, identity, site).Held;

    public static bool Click(string label, IReadableTexture thumbnail, int identity = 0, [CallerLineNumber] int site = 0)
        => CreateButton(label, thumbnail, identity, site).Up;

    private static InteractionReport CreateButton(string label, IReadableTexture thumbnail, int identity = 0, int site = 0)
    {
        var (instance, node) = Onion.Tree.Start(IdGen.Create(nameof(ExperimentModeButton).GetHashCode(), identity, site), new ExperimentModeButton());
        instance.RenderFocusBox = false;
        instance.Name = label;
        var v = (label, thumbnail);
        state.UpdateFor(instance.Identity, ref v);
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

        Draw.Colour = p.Theme.Foreground[instance.State].Color;
        Draw.ImageMode = ImageMode.Stretch;
        Draw.OutlineColour = p.Theme.OutlineColour[instance.State];
        Draw.OutlineWidth = p.Theme.OutlineWidth[instance.State];

        Draw.ResetTexture();
        anim.AnimateRect(ref instance.Rects.Rendered, t);
        anim.AnimateColour(ref Draw.Colour, t);

        Draw.Quad(instance.Rects.Rendered, 0, p.Theme.Rounding);

        Draw.ResetTexture();
        Draw.OutlineWidth = 0;
        var thumbnailRect = instance.Rects.Rendered with
        {
            MaxX = instance.Rects.Rendered.MinX + instance.Rects.Rendered.Height
        };
        Draw.Colour = p.Theme.Image[instance.State];
        anim.AnimateColour(ref Draw.Colour, t);
        Draw.Image(ExperimentModeButton.state[p.Identity].Item2, thumbnailRect.Expand(-p.Theme.Padding), ImageContainmentMode.Contain);
        Draw.ResetTexture();

        Draw.Font = p.Theme.Font;
        Draw.FontSize = p.Theme.FontSize[p.Instance.State] ;
        Draw.Colour = p.Theme.Text[instance.State] with { A = Draw.Colour.A };
        if (anim.ShouldRenderText(t))
        {
            var ratio = instance.Rects.Rendered.Area / instance.Rects.ComputedGlobal.Area;

            Draw.Text(instance.Name, new Vector2(thumbnailRect.MaxX + 10, (thumbnailRect.MinY + thumbnailRect.MaxY) * 0.5f), new Vector2(ratio),
                HorizontalTextAlign.Left, VerticalTextAlign.Middle, instance.Rects.Rendered.Width - instance.Rects.Rendered.Height);
        }
    }

    public void OnEnd(in ControlParams p)
    {
    }
}
