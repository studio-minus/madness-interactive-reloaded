using System;
using System.Numerics;
using Walgelijk;

namespace MIR.LevelEditor.Objects;

/// <summary>
/// A <see cref="LevelObject"/> representing a line between two points.
/// <br></br>
/// For an example implementation: <see cref="LineWall"/>
/// </summary>
public abstract class LineObject : LevelObject, ITagged
{
    public const Key DragKey = Key.S;
    public const Key SnapKey = Key.LeftControl;
    public const Key AltMoveKey = Key.LeftShift;

    /// <summary>
    /// The start/end points.
    /// </summary>
    public Vector2 A, B;

    /// <summary>
    /// Thickness of the line.
    /// </summary>
    public float Radius = 32;

    private TranslateDrag<Vector2> aDrag, bDrag;

    protected LineObject(LevelEditor.LevelEditorComponent editor, Vector2 a, Vector2 b, float radius) : base(editor)
    {
        A = a;
        B = b;
        Radius = radius;
    }

    public Tag? Tag { get; set; }

    public override bool ContainsPoint(Vector2 worldPoint) => SDF.LineSegment(worldPoint, A, B) < Radius;

    public override Rect? GetBounds() => null;

    public override float? GetFloorPointAt(float x)
    {
        var left = A.X < B.X ? A : B;
        var right = A.X > B.X ? A : B;

        var leftX = left.X;
        var rightX = right.X;

        if (x < leftX - Radius)
            return null;

        if (x > rightX + Radius)
            return null;

        var bounds = new Rect(float.Min(A.X, B.X), float.Min(A.Y, B.Y), float.Max(A.X, B.X), float.Max(A.Y, B.Y));

        var y = bounds.MaxY;
        var cursor = new Vector2(x, y + Radius * 2);
        const float threshold = 0.01f;

        // TODO possibly
        // (zooi): this code is sphere marching to get the topmost point on the shape.
        // I can't figure out how else to do it.
        // If you can do this properly, with maths magic, go ahead
        for (int i = 0; i < 100; i++)
        {
            var d = SDF.LineSegment(cursor, A, B) - Radius;
            if (d <= threshold)
                return cursor.Y;
            cursor.Y -= d;
        }
        return null;
    }

    public override void ProcessInEditor(Scene scene, InputState input)
    {
        float scale = Vector2.Distance(A, B);
        float os = scale;

        float angle = Utilities.VectorToAngle(A - B);
        float oa = angle;

        if (ProcessScalable(input, ref scale, GetPosition()))
        {
            float ratio = scale / os;
            var scalingMatrix = Matrix3x2.CreateScale(ratio, ratio, GetPosition());
            A = Vector2.Transform(A, scalingMatrix);
            B = Vector2.Transform(B, scalingMatrix);
        }
        else if (ProcessRotatable(input, ref angle, GetPosition(), -1))
        {
            var da = Utilities.DeltaAngle(oa, angle);
            var rotationMatrix = Matrix3x2.CreateRotation(da * Utilities.DegToRad, GetPosition());
            A = Vector2.Transform(A, rotationMatrix);
            B = Vector2.Transform(B, rotationMatrix);
        }
        else
        {
            var mousePos = input.WorldMousePosition;
            var dist = SDF.LineSegment(mousePos, A, B) - Radius;
            if (ProcessDraggable(input))
                Editor.Dirty = true;
        }
    }

    public override Vector2 GetPosition() => (A + B) * 0.5f;

    public override void SetPosition(Vector2 pos)
    {
        var p = GetPosition();
        var delta = pos - p;
        A += delta;
        B += delta;
    }
}
