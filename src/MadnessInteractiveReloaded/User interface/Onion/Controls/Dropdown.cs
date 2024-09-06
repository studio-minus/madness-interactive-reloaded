using System.Runtime.CompilerServices;
using Walgelijk.Onion.Controls;
using Walgelijk.Onion;
using Walgelijk.SimpleDrawing;
using Walgelijk;
using Walgelijk.Onion.Layout;
using NativeFileDialogExtendedSharp;
using System;
using System.Numerics;
using Walgelijk.AssetManager;
using System.Collections.Generic;
using Walgelijk.Onion.Animations;
using Walgelijk.Onion.Assets;

namespace MIR.Controls;

public readonly struct Combobox<T> : IControl
{
    public readonly IList<T> Values;
    public readonly bool DrawArrow;

    private static readonly Dictionary<int, CurrentState> currentStates = new();
    private static readonly Dictionary<Type, Array> enumValues = new();

    private record CurrentState
    {
        public int SelectedIndex;
        public bool IncomingChange;
        public Rect DropdownRect = default;
        public float TimeSinceTriggered = float.MaxValue;

        public CurrentState(int selectedIndex)
        {
            SelectedIndex = selectedIndex;
        }
    }

    public Combobox(IList<T> values, bool drawArrow)
    {
        Values = values;
        DrawArrow = drawArrow;
    }

    public static bool Enum<EnumType>(ref EnumType selected, bool arrow = true, int identity = 0, [CallerLineNumber] int site = 0) where EnumType : struct, Enum
    {
        EnumType[] arr;
        if (!enumValues.TryGetValue(typeof(EnumType), out var a))
        {
            arr = global::System.Enum.GetValues<EnumType>();
            enumValues.Add(typeof(EnumType), arr);
        }
        else
            arr = (a as EnumType[])!;

        int selectedIndex = Array.IndexOf(arr, selected);
        var result = Start(arr, ref selectedIndex, arrow, identity, site);
        selected = arr[selectedIndex];
        return result;
    }

    public static bool Start<ValueType>(IList<ValueType> values, ref int selectedIndex, bool arrow = true, int identity = 0, [CallerLineNumber] int site = 0)
    {
        var (instance, node) = Onion.Tree.Start(IdGen.Create(nameof(Dropdown<ValueType>).GetHashCode(), identity, site), new Dropdown<ValueType>(values, arrow));
        bool result = false;
        var dropdownRect = new Rect();
        bool showScrollbars = true;
        if (currentStates.TryGetValue(instance.Identity, out var currentState))
        {
            dropdownRect = currentState.DropdownRect;
            showScrollbars = currentState.TimeSinceTriggered > Onion.Animation.DefaultDurationSeconds;
            if (instance.IsTriggered)
            {
                float height = instance.Rects.ComputedGlobal.Height / Onion.GlobalScale;

                Onion.Layout.Height(dropdownRect.Height).FitContainer(1, null).StickLeft().StickTop().Move(0, height).Overflow(false, true);
                if (!showScrollbars)
                    Onion.Theme.SetAll(instance.Theme).ShowScrollbars(false).Once(); //TODO global scaling issue
                Onion.Tree.Start(instance.Identity + 31468, new ScrollView(true, false));

                for (int i = 0; i < values.Count; i++)
                {
                    Onion.Layout.Move(0, i * height).Height(height).FitWidth(false).CenterHorizontal();
                    Onion.Theme.SetAll(instance.Theme).OutlineWidth(0).Once();
                    Onion.Animation.Add(new MoveInAnimation(instance.Rects.ComputedGlobal.GetCenter()));
                    if (Button.Click(values[i]?.ToString() ?? "???", i + instance.Identity))
                    {
                        result = true;
                        selectedIndex = i;
                    }
                }

                Onion.Tree.End();
            }
        }

        Onion.Tree.End();
        if (!currentStates.TryAdd(instance.Identity, new CurrentState(selectedIndex)))
        {
            var s = currentStates[instance.Identity];
            if (s.IncomingChange)
            {
                result = true;
                selectedIndex = s.SelectedIndex;
            }
            else
                s.SelectedIndex = selectedIndex;
            s.IncomingChange = false;
        }
        return result;
    }

    public void OnAdd(in ControlParams p) { }

    public void OnRemove(in ControlParams p) { }

    public void OnStart(in ControlParams p) { }

    public void OnProcess(in ControlParams p)
    {
        var instance = p.Instance;
        var currentState = currentStates[instance.Identity];
        var old = instance.IsTriggered;
        ControlUtils.ProcessTriggerable(p);

        if (p.Input.CtrlHeld)
            p.Instance.CaptureFlags |= CaptureFlags.Scroll;
        else
            p.Instance.CaptureFlags &= ~CaptureFlags.Scroll;

        p.Node.AlwaysOnTop = p.Instance.IsTriggered;

        if (instance.IsTriggered != old)
            currentState.TimeSinceTriggered = 0;
        else
            currentState.TimeSinceTriggered += p.GameState.Time.DeltaTimeUnscaled;

        var computedGlobal = instance.Rects.ComputedGlobal;

        if (instance.HasScroll)
        {
            var v = p.Input.ScrollDelta.X + p.Input.ScrollDelta.Y;

            if (v < 0)
                currentState.SelectedIndex = (currentState.SelectedIndex + 1) % Values.Count;
            else
            {
                currentState.SelectedIndex--;
                if (currentState.SelectedIndex < 0)
                    currentState.SelectedIndex = Values.Count - 1;
            }

            currentState.IncomingChange = true;
        }

        if (instance.IsTriggered)
        {
            p.Instance.Rects.Raycast = null;

            var dropdownRect = new Rect(computedGlobal.MinX, computedGlobal.MaxY, computedGlobal.MaxX, computedGlobal.MaxY);
            var dropdownRectTargetHeight = instance.Rects.Rendered.Height * Values.Count + p.Theme.Padding * 2;

            //dropdownRectTargetHeight /= Onion.GlobalScale;

            dropdownRectTargetHeight *= Onion.Animation.Easing.Out(Utilities.Clamp(currentState.TimeSinceTriggered / MathF.Max(float.Epsilon, Onion.Animation.DefaultDurationSeconds)));
            dropdownRect.MaxY += dropdownRectTargetHeight;
            dropdownRect.MaxY = float.Min(Game.Main.Window.Height, dropdownRect.MaxY);

            if (instance.Rects.DrawBounds.HasValue)
                instance.Rects.DrawBounds = instance.Rects.DrawBounds.Value.StretchToContain(dropdownRect);

            if (instance.Rects.Raycast.HasValue)
                instance.Rects.Raycast = instance.Rects.Raycast.Value.StretchToContain(dropdownRect);

            currentState.DropdownRect = dropdownRect;

            if (Onion.Navigator.TriggeredControl.HasValue &&
                currentState.TimeSinceTriggered > 0.1f &&
                p.Input.MousePrimaryRelease)
            {
                // var focusedInst = p.Tree.EnsureInstance(Onion.Navigator.FocusedControl.Value);
                //var focusedNode = p.Tree.Nodes[Onion.Navigator.FocusedControl.Value];
                //if (!instance.HasFocus && focusedNode.Parent != null && focusedNode.Parent.Identity == instance.Identity)
                Onion.Navigator.TriggeredControl = null;
            }
        }
    }

    public void OnRender(in ControlParams p)
    {
        (ControlTree tree, Walgelijk.Onion.Layout.LayoutQueue layout, Input input, GameState state, Node node, ControlInstance instance) = p;

        var currentState = currentStates[p.Node.Identity];
        var t = node.GetAnimationTime();
        var anim = instance.Animations;

        var fg = p.Theme.Foreground[instance.State];
        anim.AnimateColour(ref fg.Color, t);

        Draw.Colour = fg.Color;
        Draw.Texture = fg.Texture;
        Draw.ImageMode = fg.ImageMode;
        Draw.OutlineColour = p.Theme.OutlineColour[instance.State];
        Draw.OutlineWidth = p.Theme.OutlineWidth[instance.State];
        anim.AnimateColour(ref Draw.OutlineColour, t);
        anim.AnimateRect(ref instance.Rects.Rendered, t);
        var arrowRect = new Rect(
            0, 0,
            instance.Rects.Rendered.Height, instance.Rects.Rendered.Height)
            .Translate(instance.Rects.Rendered.BottomLeft)
            .Translate(instance.Rects.Rendered.Width - instance.Rects.Rendered.Height, 0);
        var textRect = instance.Rects.Rendered with { Width = instance.Rects.Rendered.Width - instance.Rects.Rendered.Height + 1 };

        Draw.ImageMode = default;
        Draw.Quad(arrowRect, 0, p.Theme.Rounding);
        Draw.Quad(textRect, 0, p.Theme.Rounding);

        if (instance.IsTriggered)
        {
            Draw.Colour = fg.Color.Brightness(0.8f);
            Draw.Texture = fg.Texture;
            Draw.ImageMode = fg.ImageMode;
            Draw.Quad(currentState.DropdownRect, 0, p.Theme.Rounding);
        }

        if (anim.ShouldRenderText(t))
        {
            if (DrawArrow)
            {
                const float arrowSize = 16;
                var arrowPos = arrowRect.GetCenter().Quantise();
                Draw.Colour = p.Theme.Accent[instance.State];
                Draw.ImageMode = default;
                anim.AnimateColour(ref Draw.Colour, t);
                Draw.OutlineWidth = 0;
                Draw.Image(instance.IsTriggered ? BuiltInAssets.Icons.ChevronUp : BuiltInAssets.Icons.ChevronDown, new Rect(arrowPos, new Vector2(arrowSize)), ImageContainmentMode.Contain);
                //Draw.TriangleIscoCentered(arrowPos, new Vector2(arrowSize), instance.IsTriggered ? 0 : 180);
            }

            var ratio = instance.Rects.Rendered.Area / instance.Rects.ComputedGlobal.Area;
            Draw.ResetTexture();
            Draw.Font = p.Theme.Font;
            Draw.Colour = p.Theme.Text[instance.State];
            anim.AnimateColour(ref Draw.Colour, t);
            var selected = GetValue(currentState.SelectedIndex);
            Draw.Text(selected, textRect.GetCenter().Quantise(), new Vector2(ratio), HorizontalTextAlign.Center, VerticalTextAlign.Middle, textRect.Width);
        }
    }

    private string GetValue(int selectedIndex)
    {
        if (Values.Count == 0)
            return "OutOfBounds";

        return (selectedIndex < Values.Count && selectedIndex >= 0) ? (Values[selectedIndex]?.ToString() ?? "NULL") : "OutOfBounds";
    }

    public void OnEnd(in ControlParams p)
    {
    }
}