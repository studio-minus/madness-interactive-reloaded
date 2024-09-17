using System.Numerics;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.Localisation;
using Walgelijk.SimpleDrawing;

namespace MIR.Tutorials;

public class TeachMoveSystem : TeachSystem
{
    public override void Update()
    {
        if (MadnessUtils.IsPaused(Scene) || MadnessUtils.IsCutscenePlaying(Scene))
            return;

        var left = ControlScheme.ActiveControlScheme.InputMap[GameAction.Left];
        var right = ControlScheme.ActiveControlScheme.InputMap[GameAction.Right];

        Instructions(new Instruction
        {
            Text = string.Format(Localisation.Get("tut-move"), left, right),
            Target = Target.Player
        });

        Instructions(new Instruction
        {
            Text = Localisation.Get("tut-proceed"),
            Target = Target.Right
        });
    }
}

public class TeachPickupShootSystem : TeachSystem
{
    public override void Update()
    {
        if (MadnessUtils.IsPaused(Scene) || MadnessUtils.IsCutscenePlaying(Scene))
            return;

        if (!MadnessUtils.FindPlayer(Scene, out var _, out var playerChar))
            return;

        var interact = ControlScheme.ActiveControlScheme.InputMap[GameAction.Interact];
        var aim = ControlScheme.ActiveControlScheme.InputMap[GameAction.BlockAim];
        var attack = ControlScheme.ActiveControlScheme.InputMap[GameAction.Attack];

        if (!playerChar.EquippedWeapon.IsValid(Scene))
        {
            Instructions(new Instruction
            {
                Text = string.Format(Localisation.Get("tut-pickup"), interact),
                Target = Target.Weapon
            });
        }
        else
        {
            Instructions(new Instruction
            {
                Text = string.Format(Localisation.Get("tut-aim-shoot"), aim, attack),
                Target = Target.NPC
            });
        }

        foreach (var wpn in Scene.GetAllComponentsOfType<WeaponComponent>())
        {
            wpn.InfiniteAmmo = true;
        }
    }
}

public enum Target
{
    Right,
    Left,
    Top,
    Player,
    Weapon,
    NPC
}

public struct Instruction
{
    public string Text;
    public Target Target;
}

public abstract class TeachSystem : Walgelijk.System
{
    public void Instructions(Instruction instr)
    {
        Draw.Reset();
        Draw.ScreenSpace = true;
        Draw.Order = RenderOrders.UserInterface;

        Draw.Font = Fonts.Oxanium;
        Draw.FontSize = 20;

        const float pad = 10;

        var m = Localisation.Get(instr.Text);

        float w = float.Clamp(Draw.CalculateTextWidth(m) + 10, 100, int.Min(Window.Width, 300));
        float h = Draw.CalculateTextHeight(m, w);
        var r = new Rect(0, 0, w, h);

        switch (instr.Target)
        {
            case Target.Right:
                r = r.Translate(Window.Width - w - pad, Window.Height * 0.5f - h * 0.5f);
                break;
            case Target.Left:
                r = r.Translate(pad, Window.Height * 0.5f - h * 0.5f);
                break;
            case Target.Player:
                {
                    if (MadnessUtils.FindPlayer(Scene, out var _, out var playerChar))
                    {
                        var p = playerChar.Positioning.Body.ComputedVisualCenter;
                        p.Y += CharacterConstants.HalfHeight * 2 * playerChar.Positioning.Scale;

                        var c = Window.WorldToWindowPoint(p) - r.GetSize() * 0.5f;
                        r = r.Translate(c);
                    }
                    else return;
                }
                break;
            case Target.NPC:
                {
                    if (Scene.FindAnyComponent<AiComponent>(out var ai))
                    {
                        var character = Scene.GetComponentFrom<CharacterComponent>(ai.Entity);
                        var p = character.Positioning.Body.ComputedVisualCenter;
                        p.Y += CharacterConstants.HalfHeight * 2 * character.Positioning.Scale;

                        var c = Window.WorldToWindowPoint(p) - r.GetSize() * 0.5f;
                        r = r.Translate(c);
                    }
                    else return;
                }
                break;
            case Target.Weapon:
                {
                    Vector2? nearest = null;
                    float minDist = float.MaxValue;
                    var center = Window.Size * 0.5f;

                    if (MadnessUtils.FindPlayer(Scene, out var _, out var playerChar))
                        center = Window.WorldToWindowPoint(playerChar.Positioning.Body.ComputedVisualCenter);

                    foreach (var wpn in Scene.GetAllComponentsOfType<WeaponComponent>())
                    {
                        if (wpn.HasRoundsLeft)
                            if (Scene.TryGetComponentFrom<TransformComponent>(wpn.Entity, out var transform))
                            {
                                var p = transform.Position;
                                if (wpn.Texture.HasValue)
                                    p.Y += wpn.Texture.Value.Value.Height;
                                var c = Window.WorldToWindowPoint(p) - r.GetSize() * 0.5f;

                                var dist = Vector2.DistanceSquared(c, center);
                                if (dist < minDist)
                                {
                                    minDist = dist;
                                    nearest = c;
                                }
                            }
                    }

                    if (nearest.HasValue)
                        r = r.Translate(nearest.Value);
                    else return;
                }
                break;

            default:
            case Target.Top:
                r = r.Translate(Window.Width * 0.5f - w * 0.5f, Window.Height * 0.2f);
                break;
        }

        var rr = r.Expand(pad);
        Draw.Colour = Colors.Black.WithAlpha(0.8f);
        Draw.Quad(rr);

        Draw.Colour = Colors.Red;
        const int width = 2;
        Draw.Line(rr.Expand(-width).TopLeft, rr.Expand(-width).TopRight, width, 0);
        Draw.Text(m, r.BottomLeft, Vector2.One, HorizontalTextAlign.Left, VerticalTextAlign.Top, w);
    }
}