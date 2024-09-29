using Newtonsoft.Json;
using System;
using System.Numerics;
using Walgelijk;
using Walgelijk.Onion;
using Walgelijk.SimpleDrawing;

namespace MIR.LevelEditor.Objects;

/// <summary>
/// Represent the gizmo drag of the level object's rotation value.
/// </summary>
public struct RotationDrag
{
    public Vector2 Center;
    public float RadiansOffset;
    public float RadiansBeforeRotate;
    public Vector2 RotationPointTarget;

    public float CurrentProxyRadians;
    public float RadiansAccumulation;
}

/// <summary>
/// Represent the gizmo drag of the level object's scale value.
/// </summary>
public struct ScaleDrag
{
    public Vector2 Center;
    public float InitialDistance;
    public float ScaleBeforeDrag;
    public Vector2 ScalePointTarget;
}

/// <summary>
/// Represent the gizmo drag of the level object's translation.
/// </summary>
public struct TranslateDrag<T> where T : IEquatable<T>
{
    public T PositionBeforeDrag;
    public T DragAccumulation;
}

/// <summary>
/// Something that can be placed in a level via the <see cref="LevelEditor"/>.
/// <br></br>
/// <br></br>
/// Since Walgelijk/MRE uses an ECS architecture, this typically doesn't represent the actual
/// object which will live during the game loop.
/// <br></br>
/// Most often, it is just a data representation that gets read during level load, and then the corresponding 
/// <see cref="Walgelijk.System"/>s and <see cref="Walgelijk.Component"/>s are instantiated and
/// fed the data from the <see cref="LevelObject"/>s.
/// <br></br>
/// <br></br>
/// Typically, the <see cref="LevelObject"/> will itself instantiate an entity into the <see cref="Scene"/>
/// and attach the necessary components onto it using the data serialized onto the level object.
/// <br></br>
/// For an example of this, see <see cref="LevelWeapon.SpawnInGameScene(Scene)"/>.
/// </summary>
public abstract class LevelObject : ICloneable, ISelectable, IDisposable
{
    [NonSerialized]
    internal TranslateDrag<Vector2> TranslationTransformation;
    [NonSerialized]
    internal RotationDrag RotationTransformation;
    [NonSerialized]
    internal ScaleDrag ScaleTransformation;
    [NonSerialized]
    public LevelEditor.LevelEditorComponent Editor;

    protected LevelObject(LevelEditor.LevelEditorComponent editor)
    {
        Editor = editor;
    }

    /// <summary>
    /// What axis an object is being moved on.
    /// </summary>
    public enum Axis
    {
        Unconstrained,
        Vertical,
        Horizontal
    }

    [NonSerialized] //TODO moet dit hier??? weet je het zeker?
    public static LevelObject? DraggingObject, RotatingObject, ScalingObject;
    [NonSerialized]
    public static Axis AxisConstraint = Axis.Unconstrained;

    /// <summary>
    /// Is this object being moved right now?
    /// </summary>
    [JsonIgnore]
    public bool IsBeingDragged
    {
        set => DraggingObject = (value ? this : (DraggingObject == this ? null : DraggingObject));
        get => DraggingObject == this;
    }

    /// <summary>
    /// Is this object being rotated right now?
    /// </summary>
    [JsonIgnore]
    public bool IsBeingRotated
    {
        set => RotatingObject = (value ? this : (RotatingObject == this ? null : RotatingObject));
        get => RotatingObject == this;
    }

    /// <summary>
    /// Is this object being scaled right now?
    /// </summary>
    [JsonIgnore]
    public bool IsBeingScaled
    {
        set => ScalingObject = (value ? this : (ScalingObject == this ? null : ScalingObject));
        get => ScalingObject == this;
    }

    /// <summary>
    /// Get where this object is in the world.
    /// </summary>
    /// <returns></returns>
    public abstract Vector2 GetPosition();

    /// <summary>
    /// Set the object's world position.
    /// </summary>
    /// <param name="pos"></param>
    public abstract void SetPosition(Vector2 pos);

    /// <summary>
    /// The order in which the <see cref="SelectionManager{T}"/>
    /// will sort this by.
    /// </summary>
    [JsonIgnore]
    public int RaycastOrder { get; set; }

    /// <summary>
    /// If this levelobject is turned off.
    /// </summary>
    [JsonIgnore]
    public bool Disabled { get; set; } = false;

    /// <summary>
    /// This is run when the object is spawned in-game.
    /// Create entities and attach components from here.
    /// </summary>
    /// <param name="scene"></param>
    public abstract void SpawnInGameScene(Scene scene);

    /// <summary>
    /// Does the levelobject contain this point?
    /// Note: can be used for any kind of check. Shape intersections, distances, etc.
    /// Up to the object's implementation.
    /// </summary>
    /// <param name="worldPoint"></param>
    /// <returns></returns>
    public abstract bool ContainsPoint(Vector2 worldPoint);

    /// <summary>
    /// The <see cref="Rect"/> bounds of this object, if it has one.
    /// </summary>
    /// <returns></returns>
    public virtual Rect? GetBounds() => null;

    /// <summary>
    /// A level object can modify where the floor is.
    /// <br></br>
    /// Example: <see cref="RectWall.GetFloorPointAt(float)"/>.
    /// </summary>
    /// <param name="x"></param>
    /// <returns>The floor point as modified by this levelobject.</returns>
    public virtual float? GetFloorPointAt(float x) => null;

    /// <summary>
    /// If this levelobject should be considered part of the floor.
    /// </summary>
    /// <returns></returns>
    public virtual bool IsFloor() => false;

    /// <summary>
    /// Duplicate this object.
    /// </summary>
    /// <returns></returns>
    public abstract object Clone();

    /// <summary>
    /// Run during <see cref="Walgelijk.System.Update"/> in the <see cref="LevelEditor"/>.
    /// <br></br>
    /// For code that should run <b>in the LevelEditor</b>.
    /// So, things like updating gizmo data.
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="input"></param>
    public abstract void ProcessInEditor(Scene scene, InputState input);

    [JsonIgnore]
    protected static bool UiBlock => Onion.Navigator.IsBeingUsed;

    /// <summary>
    /// If the property window is open for this object.
    /// </summary>
    [JsonIgnore]
    public bool PropertyWindowOpen;

    /// <summary>
    /// Display property GUI for your level object in editor.
    /// </summary>
    public abstract void ProcessPropertyUi();

    protected bool ProcessDraggable(InputState input)
    {
        const Key DragKey = Key.G;
        const Key SnapKey = Key.LeftControl;
        const Key AltMoveKey = Key.LeftShift;

        const Key ConstrainHorizontalKey = Key.X;
        const Key ConstrainVerticalKey = Key.Y;

        var position = GetPosition();

        if (UiBlock || IsBeingRotated || IsBeingScaled)
            return false;

        if (IsBeingDragged)
        {
            TranslationTransformation.DragAccumulation += input.IsKeyHeld(AltMoveKey) && !input.IsKeyHeld(SnapKey) ? Editor.MouseDelta * 0.5f : Editor.MouseDelta;

            var transformedAccumulation = TranslationTransformation.DragAccumulation;

            if (input.IsKeyHeld(SnapKey))
                transformedAccumulation = Utilities.Snap(TranslationTransformation.DragAccumulation, input.IsKeyHeld(AltMoveKey) ? 10 : 100);

            ProcessAxisConstraintKeys(input, ConstrainHorizontalKey, ConstrainVerticalKey);
            ProcessDragAxisConstraint(ref transformedAccumulation, TranslationTransformation.PositionBeforeDrag);

            position = TranslationTransformation.PositionBeforeDrag + transformedAccumulation;

            if (input.IsButtonPressed(MouseButton.Left))
            {
                Editor.RegisterAction();
                var before = TranslationTransformation.PositionBeforeDrag;
                var after = position;
                IsBeingDragged = false;
                MadnessUtils.Delay(0.1f, () => Editor.SelectionManager.Select(this));
                AxisConstraint = Axis.Unconstrained;
            }
            else if (input.IsButtonPressed(MouseButton.Right))
            {
                IsBeingDragged = false;
                Editor.Dirty = true;
                position = TranslationTransformation.PositionBeforeDrag;
                AxisConstraint = Axis.Unconstrained;
            }
            SetPosition(position);
            return true;
        }
        else if (Editor.SelectionManager.SelectedObject == this && input.IsKeyPressed(DragKey))
        {
            TranslationTransformation.DragAccumulation = Vector2.Zero;
            TranslationTransformation.PositionBeforeDrag = position;
            AxisConstraint = Axis.Unconstrained;
            IsBeingDragged = true;
            SetPosition(position);
            return true;
        }
        SetPosition(position);
        return false;
    }

    protected void ProcessAxisConstraintKeys(in InputState input, Key ConstrainHorizontalKey, Key ConstrainVerticalKey)
    {
        switch (AxisConstraint)
        {
            case Axis.Vertical:
                if (input.IsKeyPressed(ConstrainVerticalKey))
                    AxisConstraint = Axis.Unconstrained;
                if (input.IsKeyPressed(ConstrainHorizontalKey))
                    AxisConstraint = Axis.Horizontal;
                break;
            case Axis.Horizontal:
                if (input.IsKeyPressed(ConstrainVerticalKey))
                    AxisConstraint = Axis.Vertical;
                if (input.IsKeyPressed(ConstrainHorizontalKey))
                    AxisConstraint = Axis.Unconstrained;
                break;
            default:
                if (input.IsKeyPressed(ConstrainVerticalKey))
                    AxisConstraint = Axis.Vertical;
                if (input.IsKeyPressed(ConstrainHorizontalKey))
                    AxisConstraint = Axis.Horizontal;
                break;
        }
    }

    protected void ProcessDragAxisConstraint(ref Vector2 transformedAccumulation, Vector2 origin)
    {
        switch (AxisConstraint)
        {
            case Axis.Vertical:
                Draw.Colour = Colors.Green;
                Draw.Line(new Vector2(origin.X, origin.Y - 10000), new Vector2(origin.X, origin.Y + 10000), Editor.PixelSize);
                transformedAccumulation.X = 0;
                break;
            case Axis.Horizontal:
                Draw.Colour = Colors.Red;
                Draw.Line(new Vector2(origin.X - 10000, origin.Y), new Vector2(origin.X + 10000, origin.Y), Editor.PixelSize);
                transformedAccumulation.Y = 0;
                break;
        }
    }

    protected bool ProcessRotatable(InputState input, ref float angle, Vector2 pivot, float speed = 1)
    {
        const Key RotateKey = Key.R;
        const Key SnapKey = Key.LeftControl;
        const Key AltMoveKey = Key.LeftShift;

        if (UiBlock || IsBeingDragged || IsBeingScaled)
            return false;

        static float normaliseRadians(float r) => r >= 0 ? r % MathF.Tau : (MathF.Tau + r) % MathF.Tau;
        static float normaliseDegrees(float r) => Utilities.Mod(r + 180, 360) - 180;
        static float deltaRadians(float x, float y) => MathF.Min(MathF.Tau - MathF.Abs(x - y), MathF.Abs(x - y));

        if (IsBeingRotated)
        {
            //rotationTransformation.RotationPointTarget += input.IsKeyHeld(AltMoveKey) && !input.IsKeyHeld(SnapKey) ? editor.MouseDelta * 0.125f : editor.MouseDelta;
            RotationTransformation.RotationPointTarget += Editor.MouseDelta;

            var dir = MadnessVector2.Normalize(RotationTransformation.RotationPointTarget - RotationTransformation.Center);

            var previousRadians = normaliseRadians(RotationTransformation.CurrentProxyRadians);
            RotationTransformation.CurrentProxyRadians = normaliseRadians(RotationTransformation.RadiansOffset - MathF.Atan2(dir.Y, dir.X));
            var delta = deltaRadians(previousRadians, RotationTransformation.CurrentProxyRadians)
                            * MathF.Sign(RotationTransformation.CurrentProxyRadians - previousRadians)
                            * (input.IsKeyHeld(AltMoveKey) && !input.IsKeyHeld(SnapKey) ? 0.1f : 1);

            RotationTransformation.RadiansAccumulation += delta * speed;

            Draw.Order = RenderOrder.Top;
            Draw.Line(RotationTransformation.Center, RotationTransformation.RotationPointTarget, 2);
            Draw.Line(RotationTransformation.RotationPointTarget + new Vector2(dir.Y, -dir.X) * -20,
                RotationTransformation.RotationPointTarget + new Vector2(dir.Y, -dir.X) * 20, 1);
            Draw.Order = default;

            if (input.IsKeyHeld(SnapKey))
                angle = Utilities.RadToDeg * (RotationTransformation.RadiansBeforeRotate + Utilities.Snap(RotationTransformation.RadiansAccumulation, input.IsKeyHeld(AltMoveKey) ? (MathF.PI / 16) : (MathF.PI / 4)));
            else
                angle = Utilities.RadToDeg * (RotationTransformation.RadiansBeforeRotate + RotationTransformation.RadiansAccumulation);
            angle = normaliseDegrees(angle);

            if (input.IsButtonPressed(MouseButton.Left))
            {
                IsBeingRotated = false;
                Editor.Dirty = true;
                //TODO dit is niet al te best
                MadnessUtils.Delay(0.1f, () => Editor.SelectionManager.Select(this));
            }
            else if (input.IsButtonPressed(MouseButton.Right))
            {
                IsBeingRotated = false;
                angle = RotationTransformation.RadiansBeforeRotate * Utilities.RadToDeg;
            }
            return true;
        }
        else if (Editor.SelectionManager.SelectedObject == this && input.IsKeyPressed(RotateKey))
        {
            RotationTransformation.Center = pivot;
            RotationTransformation.RotationPointTarget = input.WorldMousePosition;
            RotationTransformation.RadiansBeforeRotate = Utilities.DegToRad * angle;
            RotationTransformation.RadiansAccumulation = 0;
            RotationTransformation.CurrentProxyRadians = 0;

            var dir = MadnessVector2.Normalize(RotationTransformation.RotationPointTarget - RotationTransformation.Center);
            RotationTransformation.RadiansOffset = normaliseRadians(MathF.Atan2(dir.Y, dir.X));

            IsBeingRotated = true;
            return true;
        }

        return false;
    }

    protected bool ProcessScalable(InputState input, ref float scale, Vector2 pivot)
    {
        const Key ScaleKey = Key.S;
        const Key SnapKey = Key.LeftControl;
        const Key AltMoveKey = Key.LeftShift;

        if (UiBlock || IsBeingRotated || IsBeingDragged)
            return false;

        if (IsBeingScaled)
        {
            ScaleTransformation.ScalePointTarget += input.IsKeyHeld(AltMoveKey) && !input.IsKeyHeld(SnapKey) ? Editor.MouseDelta * 0.125f : Editor.MouseDelta;

            var dist = Vector2.Distance(ScaleTransformation.ScalePointTarget, ScaleTransformation.Center);
            var targetScaleAccumulation = dist / ScaleTransformation.InitialDistance;

            Draw.ResetTexture();
            Draw.ResetMaterial();
            Draw.Order = RenderOrder.Top;
            Draw.Line(ScaleTransformation.Center, ScaleTransformation.ScalePointTarget, 2);
            Draw.Order = default;

            if (input.IsKeyHeld(SnapKey))
                scale = ScaleTransformation.ScaleBeforeDrag * Utilities.Snap(targetScaleAccumulation, input.IsKeyHeld(AltMoveKey) ? 0.01f : 0.1f);
            else
                scale = ScaleTransformation.ScaleBeforeDrag * targetScaleAccumulation;

            if (input.IsButtonPressed(MouseButton.Left))
            {
                IsBeingScaled = false;
                Editor.Dirty = true;
                //TODO dit is niet al te best
                MadnessUtils.Delay(0.1f, () => Editor.SelectionManager.Select(this));
            }
            else if (input.IsButtonPressed(MouseButton.Right))
            {
                IsBeingScaled = false;
                scale = ScaleTransformation.ScaleBeforeDrag;
            }
            return true;
        }
        else if (Editor.SelectionManager.SelectedObject == this && input.IsKeyPressed(ScaleKey))
        {
            ScaleTransformation.Center = pivot;
            ScaleTransformation.ScalePointTarget = input.WorldMousePosition;
            ScaleTransformation.ScaleBeforeDrag = scale;
            ScaleTransformation.InitialDistance = Vector2.Distance(ScaleTransformation.ScalePointTarget, ScaleTransformation.Center);

            IsBeingScaled = true;
            return true;
        }

        return false;
    }

    public virtual void Dispose() { }
}
