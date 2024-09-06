using System;
using System.IO;
using System.Numerics;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.Onion;
using Walgelijk.Onion.Controls;
using Walgelijk.SimpleDrawing;

namespace MIR.LevelEditor.Objects;

/// <summary>
/// Create a flipbook in a level.
/// </summary>
public class FlipbookRect : LevelObject, ITagged
{
    /// <summary>
    /// The flipbook texture on disk.
    /// </summary>
    public AssetRef<Texture> TexturePath;

    /// <summary>
    /// The rotation angle.
    /// </summary>
    public float Angle = 0;

    /// <summary>
    /// Where the flipbook is in the level.
    /// </summary>
    public Vector2 Position;

    /// <summary>
    /// How big the object is.
    /// </summary>
    public float Scale = 1;

    /// <summary>
    /// Number of flipbook texture columns.
    /// </summary>
    public int Columns = 1;

    /// <summary>
    /// Number of flipbook texture rows.
    /// </summary>
    public int Rows = 1;

    /// <summary>
    /// What color to tint the flipbook texture.
    /// </summary>
    public Color Color = Colors.White;

    /// <summary>
    /// How long is the flipbook animation? (in seconds)
    /// </summary>
    public float Duration = 1;

    /// <summary>
    /// How many frames of animation there are.
    /// </summary>
    public int MaxFrames = 0;

    /// <summary>
    /// The render layer.
    /// </summary>
    public int Layer = RenderOrders.BackgroundInFront.Layer;

    /// <summary>
    /// Determines whether alpha clipping or alpha blending should be used
    /// </summary>
    public bool AlphaClip = true;

    /// <summary>
    /// The order inside the render layer. Render in front of 
    /// or behind some things in the same layer.
    /// </summary>
    public int OrderInLayer = 0;

    private Material? previewMaterial;

    public Tag? Tag { get; set; }

    public FlipbookRect(LevelEditor.LevelEditorComponent editor, Vector2 position) : base(editor)
    {
        Position = position;
    }

    public override object Clone()
    {
        return new FlipbookRect(Editor, Position)
        {
            TexturePath = TexturePath,
            Angle = Angle,
            Scale = Scale,
            Color = Color,
            Duration = Duration,
            Columns = Columns,
            Rows = Rows,
            AlphaClip = AlphaClip
        };
    }

    public Vector2 GetFinalSize()
    {
        var tex = GetTexture();
        var f = tex.Size;
        f.X /= Columns;
        f.Y /= Rows;
        return f * Scale;
    }

    public Texture GetTexture()
    {
        try
        {
            return !TexturePath.IsValid ? Textures.Error : TexturePath.Value;
        }
        catch (Exception)
        {
            Logger.Error($"Flipbook rect file not found: \"{TexturePath}\"");
            return Textures.Error;
        }
    }

    public override bool ContainsPoint(Vector2 worldPoint)
    {
        if (Matrix3x2.Invert(Matrix3x2.CreateRotation(-Angle * Utilities.DegToRad, Position), out var inverted))
        {
            var invertedMousePoint = Vector2.Transform(worldPoint, inverted);
            return new Rect(Position, GetFinalSize()).ContainsPoint(invertedMousePoint);
        }
        return false;
    }

    public override void ProcessInEditor(Scene scene, InputState input)
    {
        var o = Draw.Order;
        ProcessDraggable(input);
        ProcessRotatable(input, ref Angle, Position);
        ProcessScalable(input, ref Scale, Position);

        Scale = Utilities.Clamp(Scale, 0.1f, 10);
        var finalSize = GetFinalSize();

        var th = Utilities.DegToRad * Angle;
        var tex = GetTexture();
        var topLeft = new Vector2(-finalSize.X * 0.5f, finalSize.Y * 0.5f);

        if (previewMaterial == null)
            previewMaterial = FlipbookMaterialCreator.CreateUncached(tex, Columns, Rows, Color, AlphaClip, MaxFrames);

        previewMaterial.SetUniform("progress", scene.Game.State.Time.SecondsSinceLoad / Duration % 1);

        var cos = MathF.Cos(-th);
        var sin = MathF.Sin(-th);
        var rotatedTopLeft = new Vector2(
            topLeft.X * cos - topLeft.Y * sin,
            topLeft.X * sin + topLeft.Y * cos
            );

        rotatedTopLeft += Position;

        Draw.Colour = Color;
        Draw.Texture = tex;
        Draw.Material = previewMaterial;
        Draw.Order = new RenderOrder(Layer, OrderInLayer);
        Draw.Quad(rotatedTopLeft, finalSize, Angle);
        Draw.ResetMaterial();

        Draw.Order = o;
        if (Editor.SelectionManager.IsSelected(this))
        {
            Draw.OutlineColour = Colors.Red;
            Draw.OutlineWidth = 2 * Editor.PixelSize;
            Draw.Colour = Colors.Transparent;
            Draw.ResetTexture();
            Draw.Quad(rotatedTopLeft, finalSize, Angle);
        }
        else if (Editor.SelectionManager.IsHovering(this))
        {
            Draw.OutlineColour = Colors.White;
            Draw.OutlineWidth = 2 * Editor.PixelSize;
            Draw.Colour = Colors.Transparent;
            Draw.ResetTexture();
            Draw.Quad(rotatedTopLeft, finalSize, Angle);
        }
    }

    public override void SpawnInGameScene(Scene scene)
    {
        var tex = GetTexture();
        var ent = scene.CreateEntity();
        scene.AttachComponent(ent, new TransformComponent
        {
            Position = Position,
            Scale = GetFinalSize(),
            Rotation = -Angle
        });

        var mat = FlipbookMaterialCreator.LoadMaterialFor(tex, Columns, Rows, MaxFrames, Color, AlphaClip, 0);
        scene.AttachComponent(ent, new FlipbookComponent(mat) { Loop = true, Duration = Duration });
        var quad = scene.AttachComponent(ent, new QuadShapeComponent(true));
        quad.Material = mat;
        quad.RenderOrder = new RenderOrder(Layer, OrderInLayer);
        if (Tag.HasValue)
            scene.SetTag(ent, Tag.Value);
    }

    public override void ProcessPropertyUi()
    {
        Ui.Layout.FitWidth(false).Height(32);
        MadnessUi.AssetPicker(
            TexturePath.Id,
            c =>
            {
                TexturePath = new(c);
                Editor.UpdateLevelBounds();
                previewMaterial?.Dispose();
                previewMaterial = null;
            },
            static c => c.MimeType.Contains("image"));

        Ui.Layout.FitWidth(false).Height(32);
        Ui.FloatSlider(ref Angle, Direction.Horizontal, (-180, 180), 1, "Angle: {0:0.#} deg");

        Ui.Layout.FitWidth(false).Height(32);
        Ui.FloatSlider(ref Scale, Direction.Horizontal, (0.5f, 2), 0.01f, "Scale: {0:P0}");

        Ui.Label("Rows");
        Ui.Layout.FitWidth(false).Height(32);
        if (Ui.IntStepper(ref Rows, (0, 32), 1))
        {
            previewMaterial?.Dispose();
            previewMaterial = null;
        }
        Ui.Label("Columns");
        Ui.Layout.FitWidth(false).Height(32);
        if (Ui.IntStepper(ref Columns, (0, 32), 1))
        {
            previewMaterial?.Dispose();
            previewMaterial = null;
        }
        Ui.Label("Max frames");
        Ui.Layout.FitWidth(false).Height(32);
        if (Ui.IntStepper(ref MaxFrames, (0, int.MaxValue), 1))
        {
            previewMaterial?.Dispose();
            previewMaterial = null;
        }
        Ui.Layout.FitWidth(false).Height(32);
        if (Ui.Checkbox(ref AlphaClip, "Alpha clip"))
        {
            previewMaterial?.Dispose();
            previewMaterial = null;
        }

        Ui.Spacer(8);

        Ui.Label("Layer");
        Ui.Layout.FitWidth(false).Height(32);
        if (Ui.IntStepper(ref Layer, (int.MinValue, int.MaxValue), 1))
        {
            previewMaterial?.Dispose();
            previewMaterial = null;
        }

        Ui.Label("Order in layer");
        Ui.Layout.FitWidth(false).Height(32);
        if (Ui.IntStepper(ref OrderInLayer, (int.MinValue, int.MaxValue), 1))
        {
            previewMaterial?.Dispose();
            previewMaterial = null;
        }

        Ui.Layout.FitWidth(false).Height(32);
        if (Ui.Button("Behind background"))
        {
            Layer = RenderOrders.BackgroundBehind.Layer;
            OrderInLayer = RenderOrders.BackgroundBehind.OrderInLayer;
        }

        Ui.Layout.FitWidth(false).Height(32);
        if (Ui.Button("On background"))
        {
            Layer = RenderOrders.BackgroundDecals.Layer;
            OrderInLayer = RenderOrders.BackgroundDecals.OrderInLayer;
        }

        Ui.Layout.FitWidth(false).Height(32);
        if (Ui.Button("In front of background"))
        {
            Layer = RenderOrders.BackgroundInFront.Layer;
            OrderInLayer = RenderOrders.BackgroundInFront.OrderInLayer;
        }

        Ui.Spacer(8);

        Ui.Label("Duration in seconds");
        Ui.Layout.FitWidth(false).Height(32);
        Ui.FloatInputBox(ref Duration, (0, 3600));

        float fps = 1 / (Duration / (Columns * Rows));
        Ui.Layout.FitWidth(false).Height(32);
        if (Ui.FloatSlider(ref fps, Direction.Horizontal, (1, 120), 1, "{0:###} FPS"))
            Duration = (1 / fps) * (Columns * Rows);

        Ui.Spacer(8);

        Ui.Layout.FitWidth(false).Height(256);
        Ui.ColourPicker(ref Color);
    }

    public override void Dispose()
    {
        previewMaterial?.Dispose();
        base.Dispose();
        //mat.Dispose();
    }

    public override Vector2 GetPosition() => Position;
    public override void SetPosition(Vector2 pos) => Position = pos;
}
