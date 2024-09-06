using MIR.LevelEditor.Objects;
using System.Numerics;
using Walgelijk;

namespace MIR;

/// <summary>
/// Debug test scene for melee combat.
/// </summary>
public static class MeleeTestScene
{
    public static Scene Create(Game game)
    {
        game.AudioRenderer.StopAll();

        var level = new Level();

        level.LevelBounds = new Rect(-512, 0, 512, 512);

        level.FloorLine = new()
        {
            new Vector2(-512, 0),
            new Vector2(512, 0)
        };

        level.Objects.Add(new CameraSection(null, new Rect(-2000, 0, 2000, 1024)));
        level.Objects.Add(new PlayerSpawn(null) { Position = new Vector2(0, 128) });
        level.Objects.Add(new MovementBlocker(null, new Rect(-1000, 0, -512, 1000)));
        level.Objects.Add(new MovementBlocker(null, new Rect(512, 0, 1000, 1000)));

        var scene = SceneUtils.PrepareGameScene(game, GameMode.Unknown, true, level);
        scene.RemoveSystem<PlayerUISystem>();
        return scene;
    }
}
