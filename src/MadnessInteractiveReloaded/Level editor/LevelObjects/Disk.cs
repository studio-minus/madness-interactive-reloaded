using MIR.Tutorials;
using System;
using System.Numerics;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.Onion;
using Walgelijk.Onion.Controls;
using Walgelijk.SimpleDrawing;

namespace MIR.LevelEditor.Objects;

public class Turret : LevelObject, ITagged
{
    public Vector2 Position;
    public float Angle;
    public string Faction = "aahw";
    public RenderOrder RenderOrder = RenderOrders.BackgroundInFront;
    public float MinAngle = -80;
    public float MaxAngle = 80;

    public Turret(LevelEditorComponent editor, Vector2 pos) : base(editor)
    {
        Position = pos;
    }

    public Tag? Tag { get; set; }

    public override object Clone() => new Turret(Editor, Position)
    {
        Faction = Faction,
        Angle = Angle,
        RenderOrder = RenderOrder,
        MinAngle = MinAngle,
        MaxAngle = MaxAngle
    };

    public override bool ContainsPoint(Vector2 worldPoint)
    {
        return Vector2.DistanceSquared(worldPoint, Position) < (128 * 128);
    }

    public override Vector2 GetPosition() => Position;

    public override void ProcessInEditor(Scene scene, InputState input)
    {
        const int size = 40;

        ProcessDraggable(input);
        ProcessRotatable(input, ref Angle, Position);

        var body = Assets.Load<Texture>("textures/turrets/default/body.png").Value;
        var head = Assets.Load<Texture>("textures/turrets/default/head.png").Value;

        var isSelected = Editor.SelectionManager.SelectedObject == this;
        var isHover = Editor.SelectionManager.HoveringObject == this;

        Draw.Colour = (isSelected || isHover ? Colors.Red.Saturation(0.2f).Brightness(4) : Colors.White);

        Draw.Order = RenderOrder;
        var o = Draw.TransformMatrix = Matrix3x2.CreateTranslation(Position) * Matrix3x2.CreateRotation(-float.DegreesToRadians(Angle), Position);
        Draw.Texture = body;
        Draw.Quad(new Rect(default, body.Size));

        float t = (scene.Game.State.Time * 0.5f) % 2;
        t = t > 1 ? 1 - t % 1 : t;
        var th = Utilities.MapRange(0, 1, MinAngle, MaxAngle, Easings.Cubic.InOut(t));

        Draw.TransformMatrix = Matrix3x2.CreateRotation(float.DegreesToRadians(th), default) * Matrix3x2.CreateTranslation(body.Width * 0.5f, -6) * Draw.TransformMatrix;
        Draw.Texture = head;
        Draw.Order = Draw.Order.OffsetOrder(-1);
        Draw.Quad(new Rect(default, head.Size).Translate(head.Width * 0.5f - 85, -3));

        if (isSelected)
        {
            Draw.ResetTexture();
            Draw.Order = RenderOrders.UserInterface;
            Draw.TransformMatrix = Matrix3x2.CreateTranslation(body.Width * 0.5f, -6) * o;
            Draw.Colour = Colors.Red;

            var d = new Vector2(500, 0);
            int r = 16;

            Draw.Line(default, Utilities.RotatePoint(d, MinAngle), 5);
            for (int i = 0; i < r; i++)
            {
                var a = Utilities.RotatePoint(d, float.Lerp(MinAngle, MaxAngle, (i + 1) / (float)r));
                var b = Utilities.RotatePoint(d, float.Lerp(MinAngle, MaxAngle, (i) / (float)r));
                Draw.Line(a, b, 5);
            }
            Draw.Line(default, Utilities.RotatePoint(d, MaxAngle), 5);
        }

        float EaseOutElastic(float x)
        {
            const float c4 = float.Tau / 3;

            return x switch
            {
                0 => 0,
                1 => 1,
                _ => float.Pow(2, -10 * x) * float.Sin((x * 10 - 0.75f) * c4) + 1
            };
        }
    }

    public override void ProcessPropertyUi()
    {
        Ui.Spacer(5);

        Ui.Layout.FitWidth(false).Height(32);
        Ui.FloatSlider(ref MinAngle, Direction.Horizontal, (-180, 180), 1, "Min aim angle: {0:0.#} deg");
        Ui.Layout.FitWidth(false).Height(32);
        Ui.FloatSlider(ref MaxAngle, Direction.Horizontal, (-180, 180), 1, "Max aim angle: {0:0.#} deg");

        Ui.Layout.FitWidth(false).Height(32);
        Ui.FloatSlider(ref Angle, Direction.Horizontal, (-180, 180), 1, "Angle: {0:0.#} deg");

        Ui.Label("Faction");
        int selectedIndex = Array.IndexOf(Editor.Factions, Faction);
        Ui.Layout.Height(32).FitWidth(false);
        if (Ui.Dropdown(Editor.Factions, ref selectedIndex))
        {
            Faction = Editor.Factions[selectedIndex];
            Editor.Dirty = true;
        }

        Ui.Label("Layer");
        Ui.Layout.FitWidth(false).Height(32);
        Ui.IntStepper(ref RenderOrder.Layer, (int.MinValue, int.MaxValue), 1);

        Ui.Label("Order in layer");
        Ui.Layout.FitWidth(false).Height(32);
        Ui.IntStepper(ref RenderOrder.OrderInLayer, (int.MinValue, int.MaxValue), 1);
    }

    public override void SetPosition(Vector2 pos) => Position = pos;

    public override void SpawnInGameScene(Scene scene)
    {
        var entity = scene.CreateEntity();
        scene.AttachComponent(entity, new TurretComponent
        {
            Faction = Faction,
            AngleRangeRads = new Vector2(float.DegreesToRadians(MinAngle), float.DegreesToRadians(MaxAngle)),
            Position = Position,
            RenderOrder = RenderOrder,
            AngleRads = float.DegreesToRadians(Angle)
        });

        if (!scene.HasSystem<TurretSystem>())
            scene.AddSystem(new TurretSystem());
    }
}

/// <summary>
/// A clickable object that unlocks a specific improbabilty disk 🎈🎈
/// </summary>
public class Disk : LevelObject, ITagged
{
    public Vector2 Position;

    /// <summary>
    /// Degrees
    /// </summary>
    public float Angle;
    public string DiskId = string.Empty;
    public RenderOrder RenderOrder = RenderOrders.BackgroundBehind;
    public AssetRef<Texture> Texture = DiskTextures[0];

    public readonly static AssetRef<Texture>[] DiskTextures = [
        new("textures/props/floppy_disk1.png"),
        new("textures/props/floppy_disk2.png"),
        new("textures/props/floppy_disk3.png"),
    ];

    public Disk(LevelEditorComponent editor, Vector2 pos) : base(editor)
    {
        Position = pos;
    }

    public Tag? Tag { get; set; }

    public override object Clone() => new Disk(Editor, Position) { DiskId = DiskId, Angle = Angle };

    public override bool ContainsPoint(Vector2 worldPoint)
    {
        return Vector2.DistanceSquared(worldPoint, Position) < (128 * 128);
    }

    public override Vector2 GetPosition() => Position;

    public override void ProcessInEditor(Scene scene, InputState input)
    {
        const int size = 40;

        ProcessDraggable(input);
        ProcessRotatable(input, ref Angle, Position);

        var tex = Texture.Value;

        var isSelected = Editor.SelectionManager.SelectedObject == this;
        var isHover = Editor.SelectionManager.HoveringObject == this;

        Draw.Colour = (isSelected || isHover ? Colors.Red.Saturation(0.2f).Brightness(4) : Colors.White);

        Draw.Order = RenderOrder;
        Draw.TransformMatrix = Matrix3x2.CreateRotation(-float.DegreesToRadians(Angle), Position);
        Draw.Image(tex, new Rect(Position, tex.Size).Translate(0, tex.Height), ImageContainmentMode.Stretch);

        Draw.ResetTransformation();
        Draw.Order = Draw.Order.OffsetOrder(1);
        Draw.Colour = isSelected ? Colors.Red : Colors.White;
        Draw.FontSize = 14;
        Draw.Font = Fonts.Inter;
        Draw.Text(DiskId, Position + new Vector2(0, -tex.Height / 2), Vector2.One, HorizontalTextAlign.Center, VerticalTextAlign.Top);
    }

    public override void ProcessPropertyUi()
    {
        Ui.Spacer(16);

        Ui.Label("Disk to unlock");
        Ui.Layout.FitWidth(false).Height(200).VerticalLayout();
        Ui.StartScrollView();
        {
            int i = 0;
            foreach (var d in ImprobabilityDisks.All)
            {
                Ui.Layout.FitWidth().Height(32).CenterHorizontal();
                if (DiskId == d.Key)
                    Ui.Decorate(new CrosshairDecorator());
                if (Ui.Button(d.Value.DisplayName, identity: i++))
                    DiskId = d.Key;
            }
        }
        Ui.End();

        Ui.Spacer(16);

        Ui.Layout.FitWidth(false).Height(80).HorizontalLayout();
        Ui.StartScrollView();
        {
            int i = 0;
            foreach (var t in DiskTextures)
            {
                Ui.Layout.FitHeight().CenterVertical().AspectRatio(1);
                if (Texture == t)
                    Ui.Decorate(new CrosshairDecorator());
                if (Ui.ImageButton(t.Value, ImageContainmentMode.Contain, identity: i++))
                    Texture = t;
            }
        }
        Ui.End();

        Ui.Layout.FitWidth(false).Height(32);
        Ui.FloatSlider(ref Angle, Direction.Horizontal, (-180, 180), 1, "Angle: {0:0.#} deg");

        Ui.Label("Layer");
        Ui.Layout.FitWidth(false).Height(32);
        Ui.IntStepper(ref RenderOrder.Layer, (int.MinValue, int.MaxValue), 1);

        Ui.Label("Order in layer");
        Ui.Layout.FitWidth(false).Height(32);
        Ui.IntStepper(ref RenderOrder.OrderInLayer, (int.MinValue, int.MaxValue), 1);
    }

    public override void SetPosition(Vector2 pos)
    {
        Position = pos;
    }

    public override void SpawnInGameScene(Scene scene)
    {
        if (string.IsNullOrWhiteSpace(DiskId))
            return;

        var e = scene.CreateEntity();

        scene.AttachComponent(e, new UnlockDiskComponent
        {
            Angle = float.DegreesToRadians(Angle),
            DiskId = DiskId,
            Position = Position,
            RenderOrder = RenderOrder,
            Texture = Texture
        });
    }
}
