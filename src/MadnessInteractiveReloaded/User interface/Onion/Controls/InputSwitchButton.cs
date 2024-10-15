using System.Runtime.CompilerServices;
using Walgelijk.Onion.Controls;
using Walgelijk.Onion;
using Walgelijk.SimpleDrawing;
using Walgelijk;
using Walgelijk.Onion.Layout;
using System.Numerics;
using System.Linq;
using Walgelijk.AssetManager;

namespace MIR.Controls;

public readonly struct InputSwitchButton : IControl
{
    private static readonly OptionalControlState<UserInput> inputs = new();

    public static bool Start(ref UserInput input, int identity = 0, [CallerLineNumber] int site = 0)
    {
        var (instance, node) = Onion.Tree.Start(IdGen.Create(nameof(InputSwitchButton).GetHashCode(), identity, site), new InputSwitchButton());
        instance.RenderFocusBox = false;
        instance.Name = input.ToString();
        Onion.Tree.End();

        bool changed = inputs.UpdateFor(instance.Identity, ref input);
        return changed;
    }

    public void OnAdd(in ControlParams p) { }

    public void OnRemove(in ControlParams p) { }

    public void OnStart(in ControlParams p) { }

    public void OnProcess(in ControlParams p)
    {
        p.Instance.CaptureFlags = CaptureFlags.Hover;
        p.Instance.Rects.Raycast = p.Instance.Rects.ComputedGlobal;
        p.Instance.Rects.DrawBounds = p.Instance.Rects.ComputedGlobal;

        bool wasTriggered = p.Instance.IsTriggered;

        if (p.Instance.IsTriggered)
        {
            p.Instance.CaptureFlags = CaptureFlags.None;
            p.Instance.Rects.Raycast = null;

            var input = inputs.GetValue(p.Identity);

            ref var i = ref p.GameState.Input;
            if (i.AnyKey || i.AnyMouseButton)
            {
                if (i.IsKeyHeld(Key.Escape))
                    Game.Main.AudioRenderer.PlayOnce(Sounds.UiBad);
                else
                {
                    if (i.AnyKey && i.KeysHeld != null)
                        input = new UserInput(i.KeysHeld.Take(3).ToArray());
                    else if (i.MouseButtonsHeld != null)
                        input = new UserInput(i.MouseButtonsHeld.Take(3).ToArray());
                    inputs.SetValue(p.Identity, input);
                    Game.Main.AudioRenderer.PlayOnce(Sounds.UiConfirm);
                }
            }

            if (i.KeysUp?.Count > 0 || i.MouseButtonsUp?.Count > 0)
                Onion.Navigator.TriggeredControl = null;
        }

        if (p.Instance.IsActive && !p.Input.MousePrimaryHeld)
            Onion.Navigator.ActiveControl = null;

        if (!wasTriggered && p.Instance.IsHover)
        {
            IControl.SetCursor(DefaultCursor.Pointer);
            if (p.Input.MousePrimaryRelease)
            {
                Onion.Navigator.FocusedControl = p.Instance.Identity;
                Onion.Navigator.ActiveControl ??= p.Instance.Identity;
                Onion.Navigator.TriggeredControl = p.Instance.Identity;
            }
        }
    }

    public void OnRender(in ControlParams p)
    {
        (ControlTree tree, LayoutQueue layout, Input input, GameState state, Node node, ControlInstance instance) = p;

        var t = node.GetAnimationTime();
        var anim = instance.Animations;

        bool triggered = p.Instance.IsTriggered;

        Draw.Font = Assets.Load<Font>("fonts/cascadia-mono.wf");
        Draw.FontSize = p.Theme.FontSize[instance.State];

        if (anim.ShouldRenderText(t))
        {
            var w = Draw.CalculateTextWidth(instance.Name) + instance.Rects.Rendered.Height - 10;
            var keyRect = instance.Rects.Rendered with { Width = w };
            keyRect = keyRect.Translate(w * -0.5f + instance.Rects.Rendered.Width * 0.5f, 0);

            Draw.Colour = triggered ? Colors.White : Colors.Red.WithAlpha(0.1f);
            Draw.OutlineColour = Colors.Red.WithAlpha(p.Instance.IsHover ? 0.8f : 0.7f);
            Draw.OutlineWidth = 4;
            Draw.Quad(keyRect.Expand(-2), 0, 8);

            Draw.Colour = p.Theme.Text[triggered ? ControlState.Hover : instance.State];
            anim.AnimateColour(ref Draw.Colour, t);
            var ratio = instance.Rects.Rendered.Area / instance.Rects.ComputedGlobal.Area;
            Draw.Text(instance.Name, instance.Rects.Rendered.GetCenter(), new Vector2(ratio),
                HorizontalTextAlign.Center, VerticalTextAlign.Middle, instance.Rects.ComputedGlobal.Width);

        }
    }

    public void OnEnd(in ControlParams p)
    {
    }
}