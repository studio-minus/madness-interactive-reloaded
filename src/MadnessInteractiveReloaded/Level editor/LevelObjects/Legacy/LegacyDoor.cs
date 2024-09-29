using System;
using System.Numerics;
using Walgelijk;
using Walgelijk.SimpleDrawing;

namespace MIR.LevelEditor.Objects;

[Obsolete("Use Door instead")]
public class LegacyDoor : LevelObject, ITagged
{
    public const Key DragKey = Key.G;
    public const Key SnapKey = Key.LeftControl;
    public const Key AltMoveKey = Key.LeftShift;

    public const Key ConstrainHorizontalKey = Key.X;
    public const Key ConstrainVerticalKey = Key.Y;

    private static readonly Vector2[] polygonVertexBuffer = new Vector2[4];

    private TranslateDrag<Vector2> dragTopLeft, dragTopRight, dragBottomleft, dragBottomRight;
    [Flags]
    private enum Corner
    {
        None = 0b_0000,
        TopRight = 0b_0001,
        TopLeft = 0b_0010,
        BottomRight = 0b_0100,
        BottomLeft = 0b_1000
    }
    private Corner targetDragCorner = Corner.None;
    private Corner currentDragCorner = Corner.None;

    public OLD_SpawnDoorInstructions Instructions;

    public Tag? Tag { get; set; }

    public LegacyDoor(LevelEditor.LevelEditorComponent editor, OLD_SpawnDoorInstructions door) : base(editor)
    {
        Instructions = door;
    }

    public float GetDistanceFromPolygon(Vector2 worldPoint)
    {
        polygonVertexBuffer[0] = Instructions.BottomLeft;
        polygonVertexBuffer[1] = Instructions.BottomRight;
        polygonVertexBuffer[2] = Instructions.TopRight;
        polygonVertexBuffer[3] = Instructions.TopLeft;
        return MadnessUtils.DistanceToPolygon(polygonVertexBuffer, worldPoint);
    }

    public override bool ContainsPoint(Vector2 worldPoint)
    {
        return GetDistanceFromPolygon(worldPoint) <= 16 * Editor.PixelSize;
    }

    public override void SpawnInGameScene(Scene scene)
    {
        var e = Prefabs.CreateDoor(scene, Instructions.ConvertToDoorProperties());
        if (Tag.HasValue)
            scene.SetTag(e.Entity, Tag.Value);
    }

    public override object Clone() => new LegacyDoor(Editor, Instructions);

    public override void ProcessInEditor(Scene scene, InputState input)
    {
        IsBeingDragged = currentDragCorner != Corner.None;

        var pixelSize = Editor.PixelSize;
        float dragRadius = 8 * pixelSize;
        bool isSelected = Editor.SelectionManager.SelectedObject == this;

        float scale = (Instructions.TopLeft - Instructions.GetCenter()).Length();
        float os = scale;

        float angle = Utilities.VectorToAngle(Instructions.GetCenter() - Instructions.TopLeft);
        float oa = angle;

        if (ProcessScalable(input, ref scale, Instructions.GetCenter()))
        {
            float ratio = scale / os;
            var scalingMatrix = Matrix3x2.CreateScale(ratio, ratio, Instructions.GetCenter());
            Instructions.TopLeft = Vector2.Transform(Instructions.TopLeft, scalingMatrix);
            Instructions.TopRight = Vector2.Transform(Instructions.TopRight, scalingMatrix);
            Instructions.BottomLeft = Vector2.Transform(Instructions.BottomLeft, scalingMatrix);
            Instructions.BottomRight = Vector2.Transform(Instructions.BottomRight, scalingMatrix);
        }
        else if (ProcessRotatable(input, ref angle, Instructions.GetCenter(), -1))
        {
            var da = Utilities.DeltaAngle(oa, angle);
            var rotationMatrix = Matrix3x2.CreateRotation(da * Utilities.DegToRad, Instructions.GetCenter());
            Instructions.TopLeft = Vector2.Transform(Instructions.TopLeft, rotationMatrix);
            Instructions.TopRight = Vector2.Transform(Instructions.TopRight, rotationMatrix);
            Instructions.BottomLeft = Vector2.Transform(Instructions.BottomLeft, rotationMatrix);
            Instructions.BottomRight = Vector2.Transform(Instructions.BottomRight, rotationMatrix);
        }
        else
        {
            var mousePos = input.WorldMousePosition;
            if (input.IsKeyPressed(DragKey))
            {
                targetDragCorner = Corner.None;

                if (MathF.Abs(GetDistanceFromPolygon(mousePos)) > dragRadius)
                    targetDragCorner = (Corner)int.MaxValue;
                else
                {
                    if (Vector2.Distance(mousePos, Instructions.TopLeft) < dragRadius)
                        targetDragCorner |= Corner.TopLeft;

                    if (Vector2.Distance(mousePos, Instructions.TopRight) < dragRadius)
                        targetDragCorner |= Corner.TopRight;

                    if (Vector2.Distance(mousePos, Instructions.BottomLeft) < dragRadius)
                        targetDragCorner |= Corner.BottomLeft;

                    if (Vector2.Distance(mousePos, Instructions.BottomRight) < dragRadius)
                        targetDragCorner |= Corner.BottomRight;
                }

                AxisConstraint = Axis.Unconstrained;
            }

            if (targetDragCorner != Corner.None)
                ProcessAxisConstraintKeys(input, ConstrainHorizontalKey, ConstrainVerticalKey);

            Draw.Colour = Colors.GreenYellow.WithAlpha(0.8f);

            if (targetDragCorner.HasFlag(Corner.TopLeft))
            {
                ProcessDragCorner(input, Corner.TopLeft, ref dragTopLeft, ref Instructions.TopLeft);
                if (currentDragCorner != Corner.None)
                    Draw.Circle(Instructions.TopLeft, new Vector2(dragRadius));
            }

            if (targetDragCorner.HasFlag(Corner.TopRight))
            {
                ProcessDragCorner(input, Corner.TopRight, ref dragTopRight, ref Instructions.TopRight);
                if (currentDragCorner != Corner.None)
                    Draw.Circle(Instructions.TopRight, new Vector2(dragRadius));
            }

            if (targetDragCorner.HasFlag(Corner.BottomRight))
            {
                ProcessDragCorner(input, Corner.BottomRight, ref dragBottomRight, ref Instructions.BottomRight);
                if (currentDragCorner != Corner.None)
                    Draw.Circle(Instructions.BottomRight, new Vector2(dragRadius));
            }

            if (targetDragCorner.HasFlag(Corner.BottomLeft))
            {
                ProcessDragCorner(input, Corner.BottomLeft, ref dragBottomleft, ref Instructions.BottomLeft);
                if (currentDragCorner != Corner.None)
                    Draw.Circle(Instructions.BottomLeft, new Vector2(dragRadius));
            }
        }

        isSelected |= Editor.SelectionManager.HoveringObject == this;

        var bottomOfDoor = (Instructions.BottomLeft + Instructions.BottomRight) / 2;
        var spawnPoint = OLD_SpawnDoorInstructions.CalculateWorldSpawnPoint(Instructions.BottomLeft, Instructions.BottomRight);
        Instructions.FacingDirection = MadnessVector2.Normalize(bottomOfDoor - spawnPoint);

        Draw.Colour = Colors.White;
        Draw.Texture = Textures.Door.Value;
        Draw.Material = Instructions.Disabled ? Prefabs.Editor.ExampleStaticDoorMaterial : Prefabs.Editor.ExampleDoorMaterial;
        Draw.Order = RenderOrders.BackgroundBehind.WithOrder(100);
        Draw.Quad(Instructions.TopLeft, Instructions.TopRight, Instructions.BottomLeft, Instructions.BottomRight);

        Draw.ResetMaterial();
        Draw.ResetTexture();
        Draw.Order = RenderOrders.UserInterface.WithOrder(-1000);
        Draw.Colour = isSelected ? Colors.White : (Instructions.IsLevelProgressionDoor ? Colors.Green : Colors.Red);
        Draw.Line(Instructions.TopLeft, Instructions.TopRight, 2 * pixelSize);
        Draw.Line(Instructions.TopRight, Instructions.BottomRight, 2 * pixelSize);
        Draw.Line(Instructions.BottomRight, Instructions.BottomLeft, 2 * pixelSize);
        Draw.Line(Instructions.BottomLeft, Instructions.TopLeft, 2 * pixelSize);

        Draw.Colour = Colors.Cyan.WithAlpha(0.5f);

        var doorCenter = Instructions.TopLeft + Instructions.TopRight + Instructions.BottomLeft + Instructions.BottomRight;
        doorCenter /= 4;
        Draw.Colour = Colors.Green;
        Draw.Line(doorCenter, doorCenter + Instructions.FacingDirection * 50, 3 * pixelSize);
        Draw.TriangleIsco(doorCenter + Instructions.FacingDirection * 50 + new Vector2(Instructions.FacingDirection.X > 0 ? 5 : -5),
            new Vector2(10), Utilities.VectorToAngle(Instructions.FacingDirection) + 90);
    }

    private bool ProcessDragCorner(in InputState input, Corner corner, ref TranslateDrag<Vector2> transformation, ref Vector2 position)
    {
        if (UiBlock || IsBeingRotated || IsBeingScaled)
            return false;

        if (currentDragCorner.HasFlag(corner))
        {
            var d = input.IsKeyHeld(AltMoveKey) && !input.IsKeyHeld(SnapKey) ? Editor.MouseDelta * 0.5f : Editor.MouseDelta;
            transformation.DragAccumulation += d;

            var transformedAccumulation = transformation.DragAccumulation;

            if (input.IsKeyHeld(SnapKey))
                transformedAccumulation = Utilities.Snap(transformedAccumulation, input.IsKeyHeld(AltMoveKey) ? 10 : 100);

            ProcessDragAxisConstraint(ref transformedAccumulation, transformation.PositionBeforeDrag);

            position = transformation.PositionBeforeDrag + transformedAccumulation;

            if (input.IsButtonPressed(MouseButton.Left))
            {
                currentDragCorner ^= corner;
                //TODO dit is niet al te best
                MadnessUtils.Delay(0.1f, () => Editor.SelectionManager.Select(this));
            }
            else if (input.IsButtonPressed(MouseButton.Right))
            {
                currentDragCorner ^= corner;
                position = transformation.PositionBeforeDrag;
            }
            return true;
        }
        else if (Editor.SelectionManager.SelectedObject == this && input.IsKeyPressed(DragKey))
        {
            transformation.DragAccumulation = default;
            transformation.PositionBeforeDrag = position;
            AxisConstraint = Axis.Unconstrained;
            currentDragCorner |= corner;
            return true;
        }

        return false;
    }

    public override void ProcessPropertyUi()
    {
        // TODO: Level editor

        //if (Gui.ClickButton("Convert to new door", default, new Vector2(64), style: Styles.LightMode))
        //{
        //    var converted = ConvertToNewDoor(Editor, this);

        //    Editor.RegisterAction(new InlineAction("Convert legacy door",
        //        (e) => // redo
        //        {
        //            converted = ConvertToNewDoor(Editor, this);
        //        },
        //        (e) => // undo
        //        {
        //            Editor.Level!.Objects.Remove(converted);
        //            Editor.Level!.Objects.Add(this);
        //            Editor.Dirty = true;
        //            Editor.SelectionManager.DeselectAll();
        //        }
        //    ));
        //}

        //Gui.Checkbox("Level progression door", default, new Vector2(32), ref Instructions.IsLevelProgressionDoor);
        //Gui.Checkbox("Disabled", default, new Vector2(32), ref Instructions.Disabled);

        //if (Gui.ClickButton("Horizontal flip", default, new Vector2(32)))
        //{
        //    var original = Instructions;

        //    var center = original.GetCenter();

        //    flip(ref Instructions.TopRight);
        //    flip(ref Instructions.TopLeft);
        //    flip(ref Instructions.BottomLeft);
        //    flip(ref Instructions.BottomRight);

        //    void flip(ref Vector2 v)
        //    {
        //        v -= center;
        //        v.X *= -1;
        //        v += center;
        //    }
        //}
    }

    private static Door ConvertToNewDoor(LevelEditorComponent editor, LegacyDoor oldDoor)
    {
        var convertedDoor = new Door(editor, oldDoor.Instructions.ConvertToDoorProperties());
        Game.Main.AudioRenderer.Play(Sounds.UiConfirm);

        editor.Level!.Objects.Remove(oldDoor);
        editor.Level.Objects.Add(convertedDoor);
        editor.Dirty = true;

        // TODO this is far from ideal. it is done to defer the manipulation of Editor.SelectionManager collection
        // to make sure it happens after this add/remove stuff has concluded
        MadnessUtils.Delay(0.03f, () =>
        {
            convertedDoor.TranslationTransformation.DragAccumulation = Vector2.Zero;
            convertedDoor.TranslationTransformation.PositionBeforeDrag = convertedDoor.GetPosition();
            DraggingObject = convertedDoor;
            editor.SelectionManager.Select(convertedDoor);
        });

        return convertedDoor;
    }

    public override Vector2 GetPosition() => Instructions.GetCenter();
    public override void SetPosition(Vector2 pos) => Instructions.SetCenter(pos);
}
