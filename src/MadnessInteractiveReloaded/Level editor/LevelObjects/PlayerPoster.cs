using System;
using System.Numerics;
using Walgelijk;
using Walgelijk.Onion;
using Walgelijk.Onion.Controls;
using Walgelijk.SimpleDrawing;

namespace MIR.LevelEditor.Objects;

/// <summary>
/// Look, ma, I'm famous!
/// </summary>
public class PlayerPoster : LevelObject
{
    public PosterType Type;
    public float Angle = 0;
    public Vector2 Position;
    public float Scale = 1;

    public PlayerPoster(LevelEditor.LevelEditorComponent editor, PosterType type, Vector2 position) : base(editor)
    {
        Type = type;
        Position = position;
    }

    public override object Clone()
    {
        return new PlayerPoster(Editor, Type, Position)
        {
            Angle = Angle,
            Scale = Scale
        };
    }

    public static IReadableTexture GetOverlayFor(PosterType p) => Textures.PlayerPosterOverlays[(int)p].Value;
    public static IReadableTexture GetTextureFor(PosterType p) => ThumbnailRenderer.GetOrGeneratePlayerPoster((int)p);

    public override bool ContainsPoint(Vector2 worldPoint)
    {
        if (Matrix3x2.Invert(Matrix3x2.CreateRotation(-Angle * Utilities.DegToRad, Position), out var inverted))
        {
            var invertedMousePoint = Vector2.Transform(worldPoint, inverted);
            var o = GetOverlayFor(Type);
            return new Rect(Position, o.Size * Scale).ContainsPoint(invertedMousePoint);
        }
        return false;
    }

    public override void ProcessInEditor(Scene scene, InputState input)
    {
        var o = Draw.Order;
        ProcessDraggable(input);
        ProcessRotatable(input, ref Angle, Position);
        ProcessScalable(input, ref Scale, Position);
        Draw.Order = o;

        Scale = Utilities.Clamp(Scale, 0.1f, 3);

        var th = Utilities.DegToRad * Angle;
        var tex = GetTextureFor(Type);
        var topLeft = new Vector2(-tex.Size.X * Scale * 0.5f, tex.Size.Y * Scale * 0.5f);

        var cos = MathF.Cos(-th);
        var sin = MathF.Sin(-th);
        var rotatedTopLeft = new Vector2(
            topLeft.X * cos - topLeft.Y * sin,
            topLeft.X * sin + topLeft.Y * cos
            );

        rotatedTopLeft += Position;

        Draw.Order = RenderOrders.BackgroundBehind.WithOrder(1);
        Draw.Colour = Colors.White;
        Draw.Texture = tex;
        Draw.Quad(rotatedTopLeft, tex.Size * Scale, Angle);

        if (Editor.SelectionManager.IsSelected(this))
        {
            Draw.OutlineColour = Colors.Red;
            Draw.OutlineWidth = 2;
            Draw.Colour = Colors.Transparent;
            Draw.ResetTexture();
            Draw.Quad(rotatedTopLeft, tex.Size * Scale, Angle);
        }
        else if (Editor.SelectionManager.IsHovering(this))
        {
            Draw.OutlineColour = Colors.White;
            Draw.OutlineWidth = 2;
            Draw.Colour = Colors.Transparent;
            Draw.ResetTexture();
            Draw.Quad(rotatedTopLeft, tex.Size * Scale, Angle);
        }
    }

    public override void SpawnInGameScene(Scene scene)
    {
        var tex = GetTextureFor(Type);
        var ent = scene.CreateEntity();
        scene.AttachComponent(ent, new TransformComponent
        {
            Position = Position,
            Scale = tex.Size * Scale,
            Rotation = -Angle
        });
        var quad = scene.AttachComponent(ent, new QuadShapeComponent(true));
        quad.Material = SpriteMaterialCreator.Instance.Load(tex);
        quad.RenderOrder = RenderOrders.BackgroundBehind.WithOrder(1);
    }

    public override void ProcessPropertyUi()
    {
        Ui.Label("Type");
        Ui.Layout.FitWidth(false).Height(32);
        Ui.EnumDropdown(ref Type);

        Ui.Label("Angle");
        Ui.Layout.FitWidth(false).Height(32);
        Ui.FloatSlider(ref Angle, Direction.Horizontal, (-180, 180), 1, "{0:0.#} deg");

        Ui.Label("Scale");
        Ui.Layout.FitWidth(false).Height(32);
        Ui.FloatSlider(ref Scale, Direction.Horizontal, (0.5f, 2f), 0.01f, "{0:P0}");
    }

    public override Vector2 GetPosition() => Position;
    public override void SetPosition(Vector2 pos) => Position = pos;

    public enum PosterType
    {
        Loser,
        Ugly,
        Traitor
    }
}
