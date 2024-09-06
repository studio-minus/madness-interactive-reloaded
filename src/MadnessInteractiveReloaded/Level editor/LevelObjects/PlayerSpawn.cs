using System;
using System.Numerics;
using Walgelijk;
using Walgelijk.Onion;
using Walgelijk.SimpleDrawing;

namespace MIR.LevelEditor.Objects;

/// <summary>
/// Where to spawn the player in the level.
/// </summary>
public class PlayerSpawn : LevelObject
{
    public Vector2 Position;
    public string? SpawnWeapon;

    public PlayerSpawn(LevelEditor.LevelEditorComponent editor) : base(editor)
    {
    }

    public override bool ContainsPoint(Vector2 worldPoint)
    {
        return Vector2.Distance(Position, worldPoint) < 64;
    }

    public override void SpawnInGameScene(Scene scene)
    {
        //No need
    }

    public override object Clone()
    {
        return new PlayerSpawn(Editor) { Position = Position };
    }

    public override void ProcessInEditor(Scene scene, InputState input)
    {
        if (ProcessDraggable(input))
            Editor.UpdateFloorLine();

        var isSelected = Editor.SelectionManager.SelectedObject == this;
        var isHover = Editor.SelectionManager.HoveringObject == this;

        Draw.Colour = (isSelected || isHover ? Colors.White : Colors.Green).WithAlpha(0.2f);

        Draw.ResetTexture();
        Draw.Circle(Position, new Vector2(64));

        Draw.Colour = isSelected || isHover ? Colors.Green : Colors.White;
        Draw.FontSize = 18;
        Draw.Font = Fonts.Inter;
        Draw.Text("Player\nSpawn", Position, Vector2.One, HorizontalTextAlign.Center, VerticalTextAlign.Middle);

        if (!string.IsNullOrWhiteSpace(SpawnWeapon))
        {
            Draw.Font = Fonts.CascadiaMono;
            Draw.Colour = Colors.Magenta;
            Draw.Text(SpawnWeapon, Position - new Vector2(0, 32), Vector2.One, HorizontalTextAlign.Center, VerticalTextAlign.Middle);

            if (Registries.Weapons.TryGet(SpawnWeapon, out var wpn))
            {
                var tex = wpn.BaseTexture.Value;
                Draw.Colour = new Color(0.8f, 1, 0.8f, 0.5f);
                Draw.Image(tex, new Rect(Vector2.Zero, tex.Size).Translate(Position).Translate(0, tex.Size.Y), ImageContainmentMode.Stretch);
            }
        }
    }

    public override void ProcessPropertyUi()
    {
        bool hasWeapon = SpawnWeapon != null;
        Ui.Layout.Height(32).FitWidth(false);
        if (Ui.Checkbox(ref hasWeapon, "Has weapon?"))
        {
            SpawnWeapon = !hasWeapon ? Registries.Weapons.GetRandomKey() : null;
            Editor.Dirty = true;
        }

        if (SpawnWeapon != null)
        {
            if (!Registries.Weapons.Has(SpawnWeapon))
                SpawnWeapon = Registries.Weapons.GetRandomKey();

            Ui.Label("Weapon");
            var selectedIndex = Array.IndexOf(Editor.Weapons, SpawnWeapon);
            Ui.Layout.Height(32).FitWidth(false);
            if (Ui.Dropdown(Editor.Weapons, ref selectedIndex))
            {
                SpawnWeapon = Editor.Weapons[selectedIndex];
                Editor.Dirty = true;
            }
        }
    }

    public override Vector2 GetPosition() => Position;
    public override void SetPosition(Vector2 pos) => Position = pos;
}
