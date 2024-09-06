using System;
using System.Linq;
using System.Numerics;
using Walgelijk;
using Walgelijk.Onion;
using Walgelijk.SimpleDrawing;

namespace MIR.LevelEditor.Objects;

/// <summary>
/// A level representation of a weapon.
/// </summary>
public class LevelWeapon : LevelObject
{
    /// <summary>
    /// What weapon is this?
    /// </summary>
    public string WeaponKey;

    /// <summary>
    /// Rotation angle.
    /// </summary>
    public float Angle = 0;

    /// <summary>
    /// Starting ammo.
    /// </summary>
    public int Ammo = int.MaxValue;

    /// <summary>
    /// If this weapon is hung on a wall.
    /// </summary>
    public bool AttachedToWall = true;
    
    /// <summary>
    /// Where is the weapon in the world?
    /// </summary>
    public Vector2 Position;

    private static string[] ids = null!;

    public LevelWeapon(LevelEditor.LevelEditorComponent editor, string weaponKey) : base(editor)
    {
        ids ??= Registries.Weapons.GetAllKeys().ToArray();
        WeaponKey = weaponKey;
    }

    public override object Clone()
    {
        var clone = new LevelWeapon(Editor, WeaponKey)
        {
            Position = Position,
            Angle = Angle,
            Ammo = Ammo
        };
        return clone;
    }

    public override bool ContainsPoint(Vector2 worldPoint)
    {
        if (Matrix3x2.Invert(Matrix3x2.CreateRotation(-Angle * Utilities.DegToRad, Position), out var inverted))
        {
            var invertedMousePoint = Vector2.Transform(worldPoint, inverted);
            var o = GetTextureFor(WeaponKey);
            return new Rect(Position, o.Size).ContainsPoint(invertedMousePoint);
        }
        return false;
    }

    public override Vector2 GetPosition() => Position;
    public override void SetPosition(Vector2 pos) => Position = pos;

    public Texture GetTextureFor(string str)
    {
        var tex = Texture.ErrorTexture;
        if (Registries.Weapons.TryGet(str, out var wpnInstructions))
            tex = wpnInstructions.BaseTexture.Value;
        return tex;
    }

    public override void ProcessInEditor(Scene scene, InputState input)
    {
        var o = Draw.Order;
        ProcessDraggable(input);
        ProcessRotatable(input, ref Angle, Position);
        Draw.Order = o;

        var th = Utilities.DegToRad * Angle;
        var tex = GetTextureFor(WeaponKey);

        var topLeft = new Vector2(-tex.Width * 0.5f, tex.Height * 0.5f);

        var cos = MathF.Cos(-th);
        var sin = MathF.Sin(-th);
        var rotatedTopLeft = new Vector2(
            topLeft.X * cos - topLeft.Y * sin,
            topLeft.X * sin + topLeft.Y * cos
            );

        rotatedTopLeft += Position;

        Draw.Colour = Colors.White;
        Draw.Texture = tex;
        Draw.Quad(rotatedTopLeft, tex.Size, Angle);

        if (Editor.SelectionManager.IsSelected(this))
        {
            Draw.OutlineColour = Colors.Red;
            Draw.OutlineWidth = Editor.PixelSize;
            Draw.Colour = Colors.Transparent;
            Draw.ResetTexture();
            Draw.Quad(rotatedTopLeft, tex.Size, Angle);
        }
        else if (Editor.SelectionManager.IsHovering(this))
        {
            Draw.OutlineColour = Colors.White;
            Draw.OutlineWidth = Editor.PixelSize;
            Draw.Colour = Colors.Transparent;
            Draw.ResetTexture();
            Draw.Quad(rotatedTopLeft, tex.Size, Angle);
        }
    }

    public override void ProcessPropertyUi()
    {
        Ui.Label("Weapon ID");
        int index = Array.IndexOf(ids, WeaponKey);
        if (index < 0)
            index = 0;

        Ui.Layout.FitWidth(false).Height(32);
        Ui.Checkbox(ref AttachedToWall, "Attached to wall");

        Ui.Layout.FitWidth(false).Height(32);
        if (Ui.Dropdown(ids, ref index))
        {
            WeaponKey = ids[index];
            if (Registries.Weapons.TryGet(WeaponKey, out var w))
                Ammo = w.WeaponData.RoundsPerMagazine;
        }

        if (Registries.Weapons.TryGet(WeaponKey, out var wpn))
        {
            if (wpn.WeaponData.WeaponType == WeaponType.Firearm)
            {
                Ui.Label("Ammo");
                Ui.Layout.FitWidth(false).Height(32);
                Ui.IntStepper(ref Ammo, (0, wpn.WeaponData.RoundsPerMagazine));
            }
        }
    }

    public override void SpawnInGameScene(Scene scene)
    {
        if (!Registries.Weapons.TryGet(WeaponKey, out var wpn))
        {
            Logger.Error($"Attempt to spawn {nameof(LevelWeapon)} with key \"{WeaponKey}\", which is not present in the weapon registry");
            return;
        }
        var c = Prefabs.CreateWeapon(scene, Position, wpn);
        var transform = scene.GetComponentFrom<TransformComponent>(c.Entity);
        transform.Rotation = -Angle;
        c.IsAttachedToWall = AttachedToWall;
        c.RemainingRounds = (Ammo < 0 || int.MaxValue == Ammo) ? c.Data.RoundsPerMagazine : Utilities.Clamp(Ammo, 0, c.Data.RoundsPerMagazine);
    }
}
