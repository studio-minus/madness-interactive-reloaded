using System.Runtime.CompilerServices;
using Walgelijk.Onion.Controls;
using Walgelijk.Onion.Layout;
using Walgelijk.Onion;
using Walgelijk.SimpleDrawing;
using Walgelijk;
using System.Numerics;
using Walgelijk.AssetManager;

/// <summary>
/// For creating buttons with little pictures in them.
/// </summary>
public readonly struct ThumbnailButton(IReadableTexture? texture) : IControl
{
    private readonly static OptionalControlState<float> state = new();

    public static bool Hold(string label, IReadableTexture? thumbnail, int identity = 0, [CallerLineNumber] int site = 0)
        => CreateButton(label, thumbnail, identity, site).Held;

    public static bool Click(string label, IReadableTexture? thumbnail, int identity = 0, [CallerLineNumber] int site = 0)
        => CreateButton(label, thumbnail, identity, site).Up;

    private static InteractionReport CreateButton(string label, IReadableTexture? thumbnail, int identity = 0, int site = 0)
    {
        var (instance, node) = Onion.Tree.Start(IdGen.Create(nameof(ThumbnailButton).GetHashCode(), identity, site), new ThumbnailButton(thumbnail));
        instance.RenderFocusBox = false;
        instance.Name = label;

        Onion.Tree.End();
        return new InteractionReport(instance, node, InteractionReport.CastingBehaviour.Up);
    }

    public void OnAdd(in ControlParams p) { }

    public void OnRemove(in ControlParams p) { }

    public void OnStart(in ControlParams p) { }

    public void OnProcess(in ControlParams p)
    {
        ControlUtils.ProcessButtonLike(p);
    }

    public void OnRender(in ControlParams p)
    {
        (ControlTree tree, LayoutQueue layout, Input input, GameState state, Node node, ControlInstance instance) = p;

        float scaleMultiplier = 1;
        ThumbnailButton.state.UpdateFor(p.Identity, ref scaleMultiplier);

        var t = node.GetAnimationTime();
        var anim = instance.Animations;

        if (p.Instance.IsHover)
            scaleMultiplier = Utilities.SmoothApproach(scaleMultiplier, 1.1f, 25, p.GameState.Time.DeltaTime);
        else
            scaleMultiplier = Utilities.SmoothApproach(scaleMultiplier, 1, 25, p.GameState.Time.DeltaTime);

        Draw.Colour = p.Theme.Image[instance.State];
        Draw.ImageMode = ImageMode.Stretch;
        Draw.OutlineColour = p.Theme.OutlineColour[instance.State];
        Draw.OutlineWidth = p.Theme.OutlineWidth[instance.State];

        anim.AnimateRect(ref instance.Rects.Rendered, t);

        anim.AnimateColour(ref Draw.Colour, t);
        //Draw.Colour.A *= 0.5f;
        Draw.ImageMode = ImageMode.Slice;
        Draw.Texture = Assets.Load<Texture>("textures/ui/item_background.png").Value;
        Draw.Quad(instance.Rects.Rendered, 0, p.Theme.Rounding);

        if (texture != null)
        {
            Draw.ImageMode = default;
            Draw.ResetTexture();
            Draw.OutlineWidth = 0;
            var thumbnailRect = instance.Rects.Rendered;
            thumbnailRect.MaxY -= 30;
            thumbnailRect = thumbnailRect.Translate(0, 5);
            Draw.Colour = Colors.White;
            anim.AnimateColour(ref Draw.Colour, t);
            Draw.TransformMatrix = Matrix3x2.CreateScale(scaleMultiplier, thumbnailRect.GetCenter());
            Draw.Image(texture, thumbnailRect.Scale(0.85f), ImageContainmentMode.Contain);
            Draw.ResetTransformation();
            Draw.ResetTexture();

            Draw.DrawBounds = new(instance.Rects.ComputedDrawBounds);
        }

        Draw.Font = p.Theme.Font;
        Draw.FontSize = p.Theme.FontSize[p.Instance.State] - 2;
        Draw.Colour = p.Theme.Text[instance.State] with { A = Draw.Colour.A };
        if (anim.ShouldRenderText(t))
        {
            var ratio = instance.Rects.Rendered.Area / instance.Rects.ComputedGlobal.Area;
            var textRect = instance.Rects.Rendered;
            textRect.MinY = textRect.MaxY - 30;

            Draw.Text(instance.Name, textRect.GetCenter(), new Vector2(ratio),
                HorizontalTextAlign.Center, VerticalTextAlign.Middle, textRect.Width);
        }

        ThumbnailButton.state.SetValue(p.Identity, scaleMultiplier);

    }

    public void OnEnd(in ControlParams p)
    {
    }
}