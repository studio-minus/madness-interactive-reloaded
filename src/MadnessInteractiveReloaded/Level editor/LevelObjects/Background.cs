using System;
using System.IO;
using System.Numerics;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.Onion;
using Walgelijk.SimpleDrawing;
using static MIR.Prefabs;

namespace MIR.LevelEditor.Objects;

/// <summary>
/// The background of a level.
/// </summary>
public class Background : LevelObject
{
    /// <summary>
    /// The center of the background.
    /// </summary>
    public Vector2 Center;

    /// <summary>
    /// What texture to use for this background.
    /// </summary>
    public AssetRef<Texture> TexturePath;

    public Texture GetTexture()
    {
        if (TexturePath.IsValid)
            return TexturePath.Value;
        return Texture.ErrorTexture;
    }

    /// <summary>
    /// The size of the background.
    /// </summary>
    public float Scale = MadnessConstants.BackgroundSizeRatio;

    public Background(LevelEditor.LevelEditorComponent editor, AssetRef<Texture> texture, Vector2 center) : base(editor)
    {
        TexturePath = texture;
        Center = center;
    }

    public override object Clone()
    {
        return new Background(Editor, TexturePath, Center);
    }

    /// <summary>
    /// Does the texture rectangle contain this point?
    /// </summary>
    /// <param name="worldPoint"></param>
    /// <returns></returns>
    public override bool ContainsPoint(Vector2 worldPoint)
    {
        return new Rect(Center, GetTexture().Size * Scale).ContainsPoint(worldPoint);
    }

    public override void ProcessInEditor(Scene scene, InputState input)
    {
        Scale = MathF.Max(0.1f, Scale);

        bool m = ProcessDraggable(input);
        m |= ProcessScalable(input, ref Scale, Center);

        if (m)
            Editor.UpdateLevelBounds();

        Draw.Reset();

        var texture = GetTexture();
        var rect = new Rect(Center, texture.Size * Scale);

        var materials = BackgroundMaterialCache.Instance.Load(texture);

        Draw.Order = RenderOrders.BackgroundBehind.OffsetLayer(-1000);
        Draw.Material = materials.bg;
        Draw.Texture = texture;
        Draw.Quad(rect.TopLeft, rect.GetSize());

        Draw.Order = RenderOrders.BackgroundInFront.OffsetLayer(-1000);
        Draw.Material = materials.fg;
        Draw.Texture = texture;
        Draw.Quad(rect.TopLeft, rect.GetSize());

        if (Editor.SelectionManager.SelectedObject == this || Editor.SelectionManager.HoveringObject == this)
        {
            Draw.Order = RenderOrders.UserInterfaceTop;
            Draw.ResetMaterial();
            Draw.ResetTexture();
            Draw.Colour = Colors.Transparent;
            Draw.OutlineWidth = 2;
            Draw.OutlineColour = Editor.SelectionManager.SelectedObject == this ? Colors.Green : Colors.Orange.WithAlpha(0.8f);
            Draw.Quad(rect.TopLeft, rect.GetSize());
        }
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
            },
            static c => c.MimeType.StartsWith("image"));

        Ui.Layout.FitWidth(false).Height(32);
        Ui.FloatSlider(ref Scale, Walgelijk.Onion.Controls.Direction.Horizontal, (0.2775f, 4.44f), 0.01f, "{0:0.##}");

        Ui.Layout.FitWidth(false).Height(32);
        if (Ui.Button("Reset scale"))
        {
            Scale = MadnessConstants.BackgroundSizeRatio;
            Editor.UpdateLevelBounds();
        }
    }

    public override void SpawnInGameScene(Scene scene)
    {
        var tex = GetTexture();
        var mat = BackgroundMaterialCache.Instance.Load(tex);

        create(scene, tex, mat.bg, RenderOrders.BackgroundBehind.WithOrder(-1), Center);
        create(scene, tex, mat.fg, RenderOrders.BackgroundInFront.WithOrder(1), Center);

        {
            var ent = scene.CreateEntity();
            scene.AttachComponent(ent, new BackgroundBufferDrawerComponent(
                tex,
                new Rect(Center, tex.Size * Scale)
                ));
        }

        Entity create(Scene scene, Texture tex, Material mat, RenderOrder order, Vector2 center)
        {
            var ent = scene.CreateEntity();
            var transform = scene.AttachComponent(ent, new TransformComponent());
            var renderer = scene.AttachComponent(ent, new QuadShapeComponent(true));
            transform.Scale = tex.Size * Scale;
            transform.Position = center;
            renderer.Material = mat;
            renderer.RenderOrder = order;
            return ent;
        }
    }

    public override Rect? GetBounds() => new Rect(Center, Scale * GetTexture().Size);

    public override Vector2 GetPosition() => Center;

    public override void SetPosition(Vector2 pos) => Center = pos;
}