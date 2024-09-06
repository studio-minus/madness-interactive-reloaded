using MIR.LevelEditor.Objects;
using System;
using System.Numerics;
using Walgelijk;
using Walgelijk.ParticleSystem;
using Walgelijk.SimpleDrawing; 

namespace MIR;

/// <summary>
/// <see cref="Walgelijk.System"/> for things like debug visuals, debug commands, etc.
/// </summary>
public class GameDebugSystem : Walgelijk.System
{
    static GameDebugSystem()
    {
        Game.Main.OnSceneChange.AddListener(e =>
        {
            flags = GameDebugFlag.None;
            AutoExec.Run();
        });
    }

    [Flags]
    private enum GameDebugFlag
    {
        None = 0b_0000,
        DrawFloorline = 0b_0001,
        DrawWalls = 0b_0010,
        DrawAnimationInfo = 0b_0100,
    }

    private static GameDebugFlag flags;

    private static string FlagStatus(GameDebugFlag f) => flags.HasFlag(f) ? "enabled" : "disabled";

    public override void PostRender()
    {
        if (Level.CurrentLevel == null)
            return;

        if (flags.HasFlag(GameDebugFlag.DrawFloorline))
        {
            var index = 0;
            var line = Level.CurrentLevel.FloorLine.ToArray();
            foreach (var p1 in line)
            {
                var p2 = p1;
                if (index > 0 && index < line.Length - 1)
                    p2 = line[index + 1];

                DebugDraw.Line(p1, p2, Color.Red, renderOrder: RenderOrder.DebugUI);
                DebugDraw.Circle(p1, 4, Color.Blue, renderOrder: RenderOrder.DebugUI);
                index++;
            }
        }

        if (flags.HasFlag(GameDebugFlag.DrawAnimationInfo))
        {
            Draw.Reset();
            Draw.Order = RenderOrder.DebugUI;
            foreach (var ch in Scene.GetAllComponentsOfType<CharacterComponent>())
            {
                var pos = ch.Positioning.Head.GlobalPosition;

                var baseRect = new Rect(Vector2.Zero, new Vector2(256, 48)).Translate(pos.X + 64, pos.Y + 256);
                float h = 0;
                foreach (var item in ch.Animations)
                {
                    var r = baseRect.Translate(0, -h);

                    Draw.Colour = Colors.Black.WithAlpha(0.8f);
                    Draw.Quad(r);

                    Draw.Colour = Colors.White;
                    Draw.Colour = Colors.Red;
                    Draw.Quad(r.Expand(-5) with { Width = (r.Width - 5) * item.ScaledTimer / item.ScaledDuration });

                    Draw.Text(item.Animation.Name, r.BottomLeft, Vector2.One * 3, HorizontalTextAlign.Right, VerticalTextAlign.Bottom);

                    h -= 55;
                }
            }
        }

        if (flags.HasFlag(GameDebugFlag.DrawWalls))
            foreach (var item in Level.CurrentLevel.Objects)
                switch (item)
                {
                    case RectWall wall:
                        DebugDraw.Rectangle(wall.Rectangle, 0, wall.BlockerType.GetColour(), renderOrder: RenderOrder.DebugUI);
                        break;
                    case LineWall wall:
                        DebugDraw.Line(wall.A, wall.B, wall.BlockerType.GetColour(), renderOrder: RenderOrder.DebugUI);
                        break;

                    // the obsolete ones 
                    case AllBlocker b:
                        DebugDraw.Rectangle(b.Rectangle, 0, BlockerType.All.GetColour(), renderOrder: RenderOrder.DebugUI);
                        break;
                    case MovementBlocker b:
                        DebugDraw.Rectangle(b.Rectangle, 0, BlockerType.Characters.GetColour(), renderOrder: RenderOrder.DebugUI);
                        break;
                }
    }

    [Command(HelpString = "Draws the floorline")]
    public static CommandResult DebugDrawFloor(bool enabled)
    {
        flags = enabled ? (flags | GameDebugFlag.DrawFloorline) : (flags & ~GameDebugFlag.DrawFloorline);
        return $"Debug draw floor: {FlagStatus(GameDebugFlag.DrawFloorline)}";
    }

    [Command(HelpString = "Draws all walls")]
    public static CommandResult DebugDrawWalls(bool enabled)
    {
        flags = enabled ? (flags | GameDebugFlag.DrawWalls) : (flags & ~GameDebugFlag.DrawWalls);
        return $"Debug draw walls: {FlagStatus(GameDebugFlag.DrawWalls)}";
    }

    [Command(HelpString = "Draws animation information")]
    public static CommandResult DrawAnimationInfo(bool enabled)
    {
        flags = enabled ? (flags | GameDebugFlag.DrawAnimationInfo) : (flags & ~GameDebugFlag.DrawAnimationInfo);
        return $"{nameof(DrawAnimationInfo)}: {FlagStatus(GameDebugFlag.DrawAnimationInfo)}";
    }

    [Command(Alias = "GameMode", HelpString = "Force the game mode of the current level. If no input is given, the current gamemode will be returned")]
    public static CommandResult SetGameMode(string mode = "")
    {
        var scene = Game.Main.Scene;

        if (!scene.FindAnyComponent<GameModeComponent>(out var gm))
            return CommandResult.Error("Current level has no game mode component. Are you even in a level?");

        if (string.IsNullOrEmpty(mode))
            return $"Current game mode is {gm.Mode}";

        if (!Enum.TryParse<GameMode>(mode, true, out var m))
            return CommandResult.Error($"There is no game mode that matches \"{mode}\". The following are available: {string.Join(", ", Enum.GetNames<GameMode>())}");

        if (gm.Mode == m)
            return CommandResult.Warn("Target game mode matches current game mode: nothing changed");

        gm.Mode = m;

        switch (m)
        {
            case GameMode.Experiment:
                if (!scene.HasSystem<ExperimentModeSystem>())
                    scene.AddSystem(new ExperimentModeSystem());
                if (!scene.FindAnyComponent<ExperimentModeComponent>(out _))
                    scene.AttachComponent(scene.CreateEntity(), new ExperimentModeComponent());
                break;
            default:
                if (scene.HasSystem<ExperimentModeSystem>())
                    scene.RemoveSystem<ExperimentModeSystem>();
                if (scene.FindAnyComponent<ExperimentModeComponent>(out var ex))
                    scene.RemoveEntity(ex.Entity);
                break;
        }

        return $"Game mode set to {m}";
    }
}
