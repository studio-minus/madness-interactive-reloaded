using System.Linq;
using System.Numerics;
using Walgelijk;
using Walgelijk.Localisation;
using Walgelijk.SimpleDrawing;

namespace MIR.Tutorials;

public class TeachMoveSystem : TeachSystem
{
    public override void Update()
    {
        if (MadnessUtils.IsPaused(Scene) || MadnessUtils.IsCutscenePlaying(Scene))
            return;

        if (!MadnessUtils.FindPlayer(Scene, out var _, out var playerChar) || !playerChar.IsAlive)
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

        if (!MadnessUtils.FindPlayer(Scene, out var _, out var playerChar) || !playerChar.IsAlive)
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

public class TeachDodgeSystem : TeachSystem
{
    private int jumpsDodged = 0;
    private const int jumpsNeeded = 2;

    public override void Update()
    {
        if (MadnessUtils.IsPaused(Scene) || MadnessUtils.IsCutscenePlaying(Scene))
            return;

        if (!MadnessUtils.FindPlayer(Scene, out var player, out var playerChar) || !playerChar.IsAlive)
            return;

        if (!Scene.TryGetSystem<LevelProgressSystem>(out var lvlProgressSys)
            || !Scene.FindAnyComponent<LevelProgressComponent>(out var lvlProgress))
            return;

        var jumpDodge = ControlScheme.ActiveControlScheme.InputMap[GameAction.JumpDodge];
        var block = ControlScheme.ActiveControlScheme.InputMap[GameAction.BlockAim];

        if (lvlProgress.GoalReached)
        {
            foreach (var item in Scene.GetEntitiesWithTag(new Tag(9005)))
                Scene.RemoveEntity(item);

            foreach (var wpn in Scene.GetAllComponentsOfType<WeaponComponent>())
            {
                if (wpn.Data.WeaponType is not WeaponType.Melee)
                {
                    wpn.InfiniteAmmo = false;
                    wpn.RemainingRounds = 0;
                }
            }
        }
        else if (jumpsDodged >= jumpsNeeded)
        {
            if (!playerChar.EquippedWeapon.IsValid(Scene))
                playerChar.EquipWeapon(Scene,
                    Prefabs.CreateWeapon(Scene, default, Registries.Weapons["baseball_bat"]));

            Instructions(new Instruction
            {
                Text = string.Format(Localisation.Get("tut-perfect-deflect"), block),
                Target = Target.Top
            });

            foreach (var ai in Scene.GetAllComponentsOfType<AiComponent>())
            {
                var ch = Scene.GetComponentFrom<CharacterComponent>(ai.Entity);
                ch.AllowWalking = false;
                if (Time.SecondsSinceLoad - AiComponent.LastAccurateShotTime > 10)
                {
                    ai.WantsToDoAccurateShot.PreviousValue = false;
                    ai.WantsToDoAccurateShot.Value = true;
                }

                if (!ch.IsAlive)
                    lvlProgressSys.ForceReachGoal();
            }
        }
        else
        {
            if (playerChar.EquippedWeapon.IsValid(Scene))
                playerChar.DeleteHeldWeapon(Scene);

            Instructions(new Instruction
            {
                Text = Localisation.Get("tut-perfect-shot"),
                Target = Target.Top
            });

            if (jumpsDodged == 0)
                Instructions(new Instruction
                {
                    Text = string.Format(Localisation.Get("tut-jump-dodge"), jumpDodge),
                    Target = Target.Player
                });

            foreach (var wpn in Scene.GetAllComponentsOfType<WeaponComponent>())
            {
                wpn.RemainingRounds = 2000;
                wpn.InfiniteAmmo = true;
            }

            {
                if (Scene.HasComponent<JumpDodgeComponent>(playerChar.Entity)
                    && Scene.FindAnyComponent<AccurateShotComponent>(out var s))
                    if (playerChar.IsAlive && !player.IsDoingDyingSequence && s.Finished)
                        jumpsDodged++;
            }

            bool timePassed = Time.SecondsSinceSceneChange > 5;

            foreach (var ai in Scene.GetAllComponentsOfType<AiComponent>())
            {
                var ch = Scene.GetComponentFrom<CharacterComponent>(ai.Entity);

                ch.AllowWalking = false;

                if (ch.Stats.AccurateShotChance < 100)
                    ch.Stats = new(ch.Stats)
                    {
                        AccurateShotChance = 200,
                    };

                if (Time.SecondsSinceLoad - AiComponent.LastAccurateShotTime > 5)
                {
                    ai.WantsToDoAccurateShot.PreviousValue = false;
                    ai.WantsToDoAccurateShot.Value = timePassed;
                }
            }

            Draw.Reset();
            Draw.ScreenSpace = true;
            Draw.Font = Fonts.Toxigenesis;
            Draw.Colour = Colors.Red.WithAlpha(0.2f);
            Draw.BlendMode = BlendMode.Addition;
            Draw.FontSize = Utilities.MapRange(0, jumpsNeeded, 48, 100, jumpsDodged);
            Draw.Text($"{jumpsDodged} / {jumpsNeeded}", (Window.Size * 0.5f), Vector2.One, HorizontalTextAlign.Center, VerticalTextAlign.Middle);
        }
    }
}

public class TeachMeleeSystem : TeachSystem
{
    private float timeMeleeBattle = 0;
    private Entity lastParried;

    public override void Update()
    {
        if (MadnessUtils.IsPaused(Scene) || MadnessUtils.IsCutscenePlaying(Scene))
            return;

        if (!MadnessUtils.FindPlayer(Scene, out var player, out var playerChar) || !playerChar.IsAlive)
            return;

        if (!Scene.TryGetSystem<LevelProgressSystem>(out var lvlProgressSys)
            || !Scene.FindAnyComponent<LevelProgressComponent>(out var lvlProgress))
            return;

        var @throw = ControlScheme.ActiveControlScheme.InputMap[GameAction.Throw];
        var attack = ControlScheme.ActiveControlScheme.InputMap[GameAction.Attack];
        var block = ControlScheme.ActiveControlScheme.InputMap[GameAction.BlockAim];

        if (lvlProgress.GoalReached)
        {
            Instructions(new Instruction
            {
                Text = Localisation.Get("tut-finish"),
                Target = Target.Top
            });
        }
        else if (lvlProgress.BodyCount.Current == 0)
        {
            foreach (var ai in Scene.GetAllComponentsOfType<AiComponent>())
            {
                var ch = Scene.GetComponentFrom<CharacterComponent>(ai.Entity);

                if (ch.Name == "victim")
                {
                    ai.IsDocile = true;
                    ch.AllowWalking = false;
                    if (Scene.TryGetComponentFrom<BodyPartComponent>(ch.Positioning.Head.Entity, out var head))
                        head.Health = 0.01f;
                    if (Scene.TryGetComponentFrom<BodyPartComponent>(ch.Positioning.Body.Entity, out var body))
                        body.Health = 0.01f;
                }
            }

            // the Infinite Weapon
            if (!Scene.GetAllComponentsOfType<WeaponComponent>().Any(d => d.IsAttachedToWall))
            {
                var wpn = Prefabs.CreateWeapon(Scene, new Vector2(-1450, -282), Registries.Weapons.Get("baseball_bat"));
                wpn.IsAttachedToWall = true;
                if (Scene.TryGetComponentFrom<TransformComponent>(wpn.Entity, out var wpnT))
                {
                    wpnT.Rotation = -11;
                }
            }

            Instructions(new Instruction
            {
                Text = Localisation.Get("tut-throw-1"),
                Target = Target.Top
            });

            Instructions(new Instruction
            {
                Text = string.Format(Localisation.Get("tut-throw-2"), @throw),
                Target = Target.NPC
            });
        }
        else
        {
            timeMeleeBattle += Time.DeltaTime;

            if (Scene.TryGetSystem<EnemySpawningSystem>(out var s))
                s.Enabled = timeMeleeBattle > 5;

            Instructions(new Instruction
            {
                Text = string.Format(Localisation.Get("tut-melee"), attack, block),
                Target = Target.Top
            });

            foreach (var ai in Scene.GetAllComponentsOfType<AiComponent>())
            {
                var ch = Scene.GetComponentFrom<CharacterComponent>(ai.Entity);

                if (ch.Animations.Any(d => d.Animation.Name.Contains("melee_stun_sword"))) // yes, it's fucked up. 
                    lastParried = ch.Entity;
                else if (ch.Entity == lastParried)
                    lastParried = Entity.None;

                if (ch.Entity == lastParried && !ch.IsAlive)
                    lvlProgressSys.ForceReachGoal();
            }
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
        Draw.FontSize = instr.Target is Target.Top ? 20 : 20;

        const float pad = 10;

        var m = Localisation.Get(instr.Text);

        float w = float.Clamp(Draw.CalculateTextWidth(m) + 10, 100, int.Min(Window.Width, 600));
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
        Draw.Text(m, r.GetCenter() + new Vector2(0, -2), Vector2.One, HorizontalTextAlign.Center, VerticalTextAlign.Middle, w);
    }
}