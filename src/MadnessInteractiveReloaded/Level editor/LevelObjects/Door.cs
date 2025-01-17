using System;
using System.Numerics;
using Walgelijk;
using Walgelijk.Onion;
using Walgelijk.Onion.Controls;
using Walgelijk.SimpleDrawing;

namespace MIR.LevelEditor.Objects;

/// <summary>
/// A door as defined in a level. Walk through these to transition to a different level.
/// </summary>
public class Door : LevelObject, ITagged
{
    public const Key DragKey = Key.G;
    public const Key SnapKey = Key.LeftControl;
    public const Key AltMoveKey = Key.LeftShift;

    public const Key ConstrainHorizontalKey = Key.X;
    public const Key ConstrainVerticalKey = Key.Y;

    public DoorProperties Properties;

    private static readonly Vector2[] polygonVertexBuffer = new Vector2[4];
    private Material previewMaterial;

    private TranslateDrag<Vector2> dragTopLeft, dragTopRight, dragBottomleft, dragBottomRight;
    [Flags]
    private enum Corner : byte
    {
        None = 0,
        TopRight = 1,
        TopLeft = 2,
        BottomRight = 4,
        BottomLeft = 8
    }
    private Corner targetDragCorner = Corner.None;
    private Corner currentDragCorner = Corner.None;

    public Tag? Tag { get; set; }

    public Door(LevelEditor.LevelEditorComponent editor, DoorProperties properties) : base(editor)
    {
        Properties = properties;
        previewMaterial = DoorMaterialPool.Instance.ForceCreateNew();
    }

    public float GetDistanceFromPolygon(Vector2 worldPoint)
    {
        polygonVertexBuffer[0] = Properties.BottomLeft;
        polygonVertexBuffer[1] = Properties.BottomRight;
        polygonVertexBuffer[2] = Properties.TopRight;
        polygonVertexBuffer[3] = Properties.TopLeft;
        return MadnessUtils.DistanceToPolygon(polygonVertexBuffer, worldPoint);
    }

    public override bool ContainsPoint(Vector2 worldPoint)
    {
        return GetDistanceFromPolygon(worldPoint) <= 16 * Editor.PixelSize;
    }

    public override void SpawnInGameScene(Scene scene)
    {
        var e = Prefabs.CreateDoor(scene, Properties);
        if (Tag.HasValue)
            scene.SetTag(e.Entity, Tag.Value);
    }

    public override object Clone()
    {
        var copy = Properties;
        return new Door(Editor, copy);
    }

    public override void ProcessInEditor(Scene scene, InputState input)
    {
        IsBeingDragged = currentDragCorner != Corner.None;

        var pixelSize = Editor.PixelSize;
        float dragRadius = 8 * pixelSize;
        bool isSelected = Editor.SelectionManager.SelectedObject == this;

        float scale = (Properties.TopLeft - Properties.Center).Length();
        float os = scale;

        float angle = Utilities.VectorToAngle(Properties.Center - Properties.TopLeft);
        float oa = angle;

        if (ProcessScalable(input, ref scale, Properties.Center))
        {
            float ratio = scale / os;
            var scalingMatrix = Matrix3x2.CreateScale(ratio, ratio, Properties.Center);
            Properties.TopLeft = Vector2.Transform(Properties.TopLeft, scalingMatrix);
            Properties.TopRight = Vector2.Transform(Properties.TopRight, scalingMatrix);
            Properties.BottomLeft = Vector2.Transform(Properties.BottomLeft, scalingMatrix);
            Properties.BottomRight = Vector2.Transform(Properties.BottomRight, scalingMatrix);
        }
        else if (ProcessRotatable(input, ref angle, Properties.Center, -1))
        {
            var da = Utilities.DeltaAngle(oa, angle);
            var rotationMatrix = Matrix3x2.CreateRotation(da * Utilities.DegToRad, Properties.Center);
            Properties.TopLeft = Vector2.Transform(Properties.TopLeft, rotationMatrix);
            Properties.TopRight = Vector2.Transform(Properties.TopRight, rotationMatrix);
            Properties.BottomLeft = Vector2.Transform(Properties.BottomLeft, rotationMatrix);
            Properties.BottomRight = Vector2.Transform(Properties.BottomRight, rotationMatrix);
        }
        else
        {
            var mousePos = input.WorldMousePosition;
            if (input.IsKeyPressed(DragKey))
            {
                targetDragCorner = Corner.None;

                if (float.Abs(GetDistanceFromPolygon(mousePos)) > dragRadius)
                    targetDragCorner = (Corner)byte.MaxValue;
                else
                {
                    if (Vector2.Distance(mousePos, Properties.TopLeft) < dragRadius)
                        targetDragCorner |= Corner.TopLeft;

                    if (Vector2.Distance(mousePos, Properties.TopRight) < dragRadius)
                        targetDragCorner |= Corner.TopRight;

                    if (Vector2.Distance(mousePos, Properties.BottomLeft) < dragRadius)
                        targetDragCorner |= Corner.BottomLeft;

                    if (Vector2.Distance(mousePos, Properties.BottomRight) < dragRadius)
                        targetDragCorner |= Corner.BottomRight;
                }

                AxisConstraint = Axis.Unconstrained;
            }

            if (IsBeingDragged && targetDragCorner != Corner.None)
                ProcessAxisConstraintKeys(input, ConstrainHorizontalKey, ConstrainVerticalKey);

            Draw.Colour = Colors.GreenYellow.WithAlpha(0.8f);

            if (targetDragCorner.HasFlag(Corner.TopLeft))
            {
                ProcessDragCorner(input, Corner.TopLeft, ref dragTopLeft, ref Properties.TopLeft);
                if (currentDragCorner != Corner.None)
                    Draw.Circle(Properties.TopLeft, new Vector2(dragRadius));
            }

            if (targetDragCorner.HasFlag(Corner.TopRight))
            {
                ProcessDragCorner(input, Corner.TopRight, ref dragTopRight, ref Properties.TopRight);
                if (currentDragCorner != Corner.None)
                    Draw.Circle(Properties.TopRight, new Vector2(dragRadius));
            }

            if (targetDragCorner.HasFlag(Corner.BottomRight))
            {
                ProcessDragCorner(input, Corner.BottomRight, ref dragBottomRight, ref Properties.BottomRight);
                if (currentDragCorner != Corner.None)
                    Draw.Circle(Properties.BottomRight, new Vector2(dragRadius));
            }

            if (targetDragCorner.HasFlag(Corner.BottomLeft))
            {
                ProcessDragCorner(input, Corner.BottomLeft, ref dragBottomleft, ref Properties.BottomLeft);
                if (currentDragCorner != Corner.None)
                    Draw.Circle(Properties.BottomLeft, new Vector2(dragRadius));
            }
        }

        isSelected |= Editor.SelectionManager.HoveringObject == this;

        var bottomOfDoor = (Properties.BottomLeft + Properties.BottomRight) / 2;
        Properties.SpawnPoint = OLD_SpawnDoorInstructions.CalculateWorldSpawnPoint(Properties.BottomLeft, Properties.BottomRight);
        Properties.FacingDirection = Vector2.Normalize(bottomOfDoor - Properties.SpawnPoint);

        Draw.Colour = Colors.White;
        Draw.Texture = Properties.EffectiveTexture;
        Draw.Material = previewMaterial;

        var time = scene.Game.State.Time.SecondsSinceLoadUnscaled;
        previewMaterial.SetUniform(DoorComponent.DoorTypeUniform, (float)(int)Properties.Behaviour);
        previewMaterial.SetUniform(DoorComponent.TimeUniform, time);
        previewMaterial.SetUniform(DoorComponent.TimeSinceChangeUniform, float.Floor(time));
        previewMaterial.SetUniform(DoorComponent.IsOpenUniform, time % 2 > 1 ? 0f : 1f);

        Draw.Order = RenderOrders.BackgroundBehind.WithOrder(100);
        Draw.Quad(Properties.TopLeft, Properties.TopRight, Properties.BottomLeft, Properties.BottomRight);

        Draw.ResetMaterial();
        Draw.ResetTexture();
        Draw.Order = RenderOrders.UserInterface.WithOrder(-1000);
        Draw.Colour = isSelected ? Colors.White : (Properties.IsLevelProgressionDoor ? Colors.Green : Colors.Red);
        Draw.Line(Properties.TopLeft, Properties.TopRight, 2 * pixelSize);
        Draw.Line(Properties.TopRight, Properties.BottomRight, 2 * pixelSize);
        Draw.Line(Properties.BottomRight, Properties.BottomLeft, 2 * pixelSize);
        Draw.Line(Properties.BottomLeft, Properties.TopLeft, 2 * pixelSize);

        Draw.Colour = Colors.Cyan.WithAlpha(0.5f);

        var doorCenter = Properties.TopLeft + Properties.TopRight + Properties.BottomLeft + Properties.BottomRight;
        doorCenter /= 4;
        Draw.Colour = Colors.Green;
        Draw.Line(doorCenter, doorCenter + Properties.FacingDirection * 50, 3 * pixelSize);
        Draw.TriangleIsco(doorCenter + Properties.FacingDirection * 50 + new Vector2(Properties.FacingDirection.X > 0 ? 5 : -5),
            new Vector2(10), Utilities.VectorToAngle(Properties.FacingDirection) + 90);
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
        Ui.Layout.FitWidth(false).Height(32);
        Ui.Checkbox(ref Properties.EnemySpawnerDoor, "Enemy spawner");

        Ui.Layout.FitWidth(false).Height(32);
        Ui.Checkbox(ref Properties.IsLevelProgressionDoor, "Level progress exit");

        if (!Properties.IsLevelProgressionDoor)
        {
            Ui.Layout.FitWidth(false).Height(32);
            Ui.Checkbox(ref Properties.IsPortal, "Portal door");
        }

        Ui.Spacer(16);
        Ui.Label("Texture");
        Ui.Layout.FitWidth(false).Height(32);
        MadnessUi.AssetPicker(Properties.Texture ?? default, id =>
        {
            Editor.RegisterAction();

            if (Textures.Door.Id == id)
                Properties.Texture = null;
            else
                Properties.Texture = id;
        }, static c => c.MimeType.Contains("image"));

        if (Properties.Texture.HasValue)
        {
            Ui.Layout.FitWidth(false).Height(32);
            if (Ui.Button("Reset texture"))
            {
                Editor.RegisterAction();
                Properties.Texture = null;
            }
        }

        Ui.Label("Animation");
        Ui.Layout.FitWidth(false).Height(32);
        if (Ui.EnumDropdown(ref Properties.Behaviour))
        {
            Editor.RegisterAction();
        }

        Ui.Spacer(16);

        Properties.IsPortal &= !Properties.IsLevelProgressionDoor;

        if (Properties.IsPortal)
        {
            Properties.DestinationLevel ??= Utilities.PickRandom(Editor.LevelIds);
            int selectedIndex = Array.IndexOf(Editor.LevelIds, Properties.DestinationLevel);

            Ui.Layout.FitWidth(false).Height(32);
            if (Ui.Dropdown(Editor.LevelIds, ref selectedIndex))
                Properties.DestinationLevel = Editor.LevelIds[selectedIndex];

            Properties.PortalID ??= string.Empty;
            Ui.Spacer(18);
            Ui.Label("Optional Portal ID");
            Ui.Layout.FitWidth(false).Height(32);
            Ui.StringInputBox(ref Properties.PortalID, new TextBoxOptions("Optional Portal ID"));
            Ui.Spacer(18);
        }

        Ui.Layout.FitWidth(false).Height(32);
        if (Ui.Button("Horizontal flip"))
        {
            Editor.RegisterAction();

            var original = Properties;

            var center = original.TopRight + original.TopLeft + original.BottomLeft + original.BottomRight;
            center /= 4f;

            flip(ref Properties.TopRight);
            flip(ref Properties.TopLeft);
            flip(ref Properties.BottomLeft);
            flip(ref Properties.BottomRight);

            void flip(ref Vector2 v)
            {
                v -= center;
                v.X *= -1;
                v += center;
            }
        }
    }

    public override Vector2 GetPosition() => Properties.Center;
    public override void SetPosition(Vector2 pos) => Properties.Center = pos;

    public override void Dispose()
    {
        previewMaterial.Dispose();
        base.Dispose();
    }
}
