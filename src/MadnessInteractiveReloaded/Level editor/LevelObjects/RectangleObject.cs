using System;
using System.Numerics;
using Walgelijk;
using Walgelijk.SimpleDrawing;

namespace MIR.LevelEditor.Objects;

/// <summary>
/// An object best represented by a <see cref="Rect"/>.
/// <br></br>
/// For examples see: <see cref="CameraSection"/>, <see cref="LevelProgressTrigger"/>, <see cref="DjScreen"/>, <see cref="DecalZone"/>.
/// </summary>
public abstract class RectangleObject : LevelObject, ITagged
{
    /// <summary>
    /// The rectangle that this object uses.
    /// </summary>
    public Rect Rectangle;
    public Tag? Tag { get; set; }

    public const Key DragKey = Key.S;
    public const Key SnapKey = Key.LeftControl;
    public const Key AltMoveKey = Key.LeftShift;
    public const float DragEdgeWidth = 4;

    private TranslateDrag<float> dragTop, dragRight, dragBottom, dragLeft;
    [Flags]
    private enum Side
    {
        None = 0b_0000,
        Top = 0b_0001,
        Left = 0b_0010,
        Right = 0b_0100,
        Bottom = 0b_1000
    }
    private Side targetDragSide = Side.None;
    private Side currentDragSide = Side.None;

    protected RectangleObject(LevelEditor.LevelEditorComponent editor) : base(editor)
    {
    }

    public override Vector2 GetPosition() => Rectangle.GetCenter();
    public override void SetPosition(Vector2 pos) => Rectangle.SetCenter(pos);

    public override bool ContainsPoint(Vector2 worldPoint) => Rectangle.ContainsPoint(worldPoint, DragEdgeWidth * Editor.PixelSize);

    protected bool ProcessRectangle(InputState input)
    {
        const float MinSize = 8;
        float scaledDragEdgeWidth = DragEdgeWidth * Editor.PixelSize;

        if (Rectangle.Width < MinSize)
            Rectangle.Width = MinSize;
        if (Rectangle.Height < MinSize)
            Rectangle.Height = MinSize;

        if (UiBlock)
            return false;

        IsBeingScaled = currentDragSide != Side.None;
        if (ProcessDraggable(input))
            return true;

        var mousePos = input.WorldMousePosition;
        if (input.IsKeyPressed(DragKey))
        {
            targetDragSide = Side.None;

            if (MathF.Abs(mousePos.Y - Rectangle.MaxY) <= scaledDragEdgeWidth)
                targetDragSide |= Side.Top;
            if (MathF.Abs(mousePos.Y - Rectangle.MinY) <= scaledDragEdgeWidth)
                targetDragSide |= Side.Bottom;

            if (MathF.Abs(mousePos.X - Rectangle.MaxX) <= scaledDragEdgeWidth)
                targetDragSide |= Side.Right;
            if (MathF.Abs(mousePos.X - Rectangle.MinX) <= scaledDragEdgeWidth)
                targetDragSide |= Side.Left;
        }
        Draw.Colour = Colors.GreenYellow.WithAlpha(0.8f);

        if (targetDragSide.HasFlag(Side.Left))
        {
            ProcessDragSide(input, Side.Left, ref dragLeft, ref Rectangle.MinX);
            if (currentDragSide != Side.None)
            {
                Draw.Line(Rectangle.TopLeft, Rectangle.BottomLeft, scaledDragEdgeWidth);
                Rectangle.MinX = MathF.Min(Rectangle.MaxX - MinSize, Rectangle.MinX);
            }
        }

        if (targetDragSide.HasFlag(Side.Top))
        {
            ProcessDragSide(input, Side.Top, ref dragTop, ref Rectangle.MaxY);
            if (currentDragSide != Side.None)
            {
                Draw.Line(Rectangle.TopLeft, Rectangle.TopRight, scaledDragEdgeWidth);
                Rectangle.MaxY = MathF.Max(Rectangle.MaxY, Rectangle.MinY + MinSize);
            }
        }

        if (targetDragSide.HasFlag(Side.Bottom))
        {
            ProcessDragSide(input, Side.Bottom, ref dragBottom, ref Rectangle.MinY);
            if (currentDragSide != Side.None)
            {
                Draw.Line(Rectangle.BottomLeft, Rectangle.BottomRight, scaledDragEdgeWidth);
                Rectangle.MinY = MathF.Min(Rectangle.MaxY - MinSize, Rectangle.MinY);
            }
        }

        if (targetDragSide.HasFlag(Side.Right))
        {
            ProcessDragSide(input, Side.Right, ref dragRight, ref Rectangle.MaxX);
            if (currentDragSide != Side.None)
            {
                Draw.Line(Rectangle.TopRight, Rectangle.BottomRight, scaledDragEdgeWidth);
                Rectangle.MaxX = MathF.Max(Rectangle.MaxX, Rectangle.MinX + MinSize);
            }
        }

        return currentDragSide != Side.None;
    }

    private bool ProcessDragSide(in InputState input, Side side, ref TranslateDrag<float> transformation, ref float position)
    {
        if (UiBlock || IsBeingRotated || IsBeingDragged)
            return false;

        if (currentDragSide.HasFlag(side))
        {
            var d = input.IsKeyHeld(AltMoveKey) && !input.IsKeyHeld(SnapKey) ? Editor.MouseDelta * 0.5f : Editor.MouseDelta;
            switch (side)
            {
                case Side.Right:
                case Side.Left:
                    transformation.DragAccumulation += d.X;
                    break;
                case Side.Bottom:
                case Side.Top:
                    transformation.DragAccumulation += d.Y;
                    break;
            }

            if (input.IsKeyHeld(SnapKey))
                position = transformation.PositionBeforeDrag + Utilities.Snap(transformation.DragAccumulation, input.IsKeyHeld(AltMoveKey) ? 10 : 100);
            else
                position = transformation.PositionBeforeDrag + transformation.DragAccumulation;

            if (input.IsButtonPressed(MouseButton.Left))
            {
                currentDragSide ^= side;
                //TODO dit is niet al te best
                MadnessUtils.Delay(0.1f, () => Editor.SelectionManager.Select(this));
            }
            else if (input.IsButtonPressed(MouseButton.Right))
            {
                currentDragSide ^= side;
                position = transformation.PositionBeforeDrag;
            }
            return true;
        }
        else if (Editor.SelectionManager.SelectedObject == this && input.IsKeyPressed(DragKey))
        {
            transformation.DragAccumulation = 0;
            transformation.PositionBeforeDrag = position;
            currentDragSide |= side;
            return true;
        }

        return false;
    }
}
