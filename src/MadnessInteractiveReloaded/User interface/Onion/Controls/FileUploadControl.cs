using System.Runtime.CompilerServices;
using Walgelijk.Onion.Controls;
using Walgelijk.Onion;
using Walgelijk.SimpleDrawing;
using Walgelijk;
using Walgelijk.Onion.Layout;
using System.Numerics;
using Walgelijk.AssetManager;

namespace MIR.Controls;

/// <summary>
/// For creating a file upload dialog with Onion.
/// </summary>
public readonly struct FileUploadControl : IControl
{
    private static readonly OptionalControlState<string> controlState = new();

    /// <summary>
    /// Add a button to the dialog.
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="identity"></param>
    /// <param name="site"></param>
    /// <returns></returns>
    public static bool CreateButton(ref string filePath, int identity = 0, [CallerLineNumber] int site = 0)
    {
        var (instance, node) = Onion.Tree.Start(IdGen.Create(nameof(FileUploadControl).GetHashCode(), identity, site), new FileUploadControl());
        instance.RenderFocusBox = false;
        Onion.Tree.End();
        bool a = controlState.UpdateFor(instance.Identity, ref filePath);
        return a;
    }

    public void OnAdd(in ControlParams p) { }

    public void OnRemove(in ControlParams p) { }

    public void OnStart(in ControlParams p) { }

    public void OnProcess(in ControlParams p)
    {
        ControlUtils.ProcessButtonLike(p);

        if (p.Instance.IsActive)
        {
            var r = FileDialog.OpenFile([], out var path);
            if (r)
                controlState.SetValue(p.Identity, path);

            Onion.Navigator.ActiveControl = null;
        }
    }

    public void OnRender(in ControlParams p)
    {
        (ControlTree tree, LayoutQueue layout, Input input, GameState state, Node node, ControlInstance instance) = p;

        var t = node.GetAnimationTime();
        var anim = instance.Animations;

        var fg = p.Theme.Foreground[instance.State];
        Draw.Colour = fg.Color;
        Draw.Texture = fg.Texture;
        Draw.OutlineColour = p.Theme.OutlineColour[instance.State];
        Draw.OutlineWidth = p.Theme.OutlineWidth[instance.State];

        anim.AnimateRect(ref instance.Rects.Rendered, t);

        anim.AnimateColour(ref Draw.Colour, t);
        Draw.Quad(instance.Rects.Rendered, 0, p.Theme.Rounding);
        Draw.ResetTexture();

        Draw.Font = p.Theme.Font;
        Draw.Colour = p.Theme.Text[instance.State].WithAlpha(0.5f);
        anim.AnimateColour(ref Draw.Colour, t);

        if (anim.ShouldRenderText(t))
        {
            var ratio = instance.Rects.Rendered.Area / instance.Rects.ComputedGlobal.Area;
            var r = instance.Rects.Rendered;
            r.MinX += instance.Rects.Rendered.Height;
            var s = controlState[node.Identity];
            Draw.Text(string.IsNullOrEmpty(s) ? "..." : s, r.GetCenter(), new Vector2(ratio), HorizontalTextAlign.Center, VerticalTextAlign.Middle);
        }

        var rr = instance.Rects.Rendered;
        rr.MaxX = rr.MinX + rr.Height;

        Draw.Colour = p.Theme.Image[instance.State];
        Draw.OutlineWidth = 0;
        Draw.Image(Assets.Load<Texture>("textures/ui/open_file.png").Value, rr.Scale(0.5f), ImageContainmentMode.Contain);
    }

    public void OnEnd(in ControlParams p)
    {
    }
}
