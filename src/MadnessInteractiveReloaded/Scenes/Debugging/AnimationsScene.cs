using System.Numerics;
using Walgelijk;
using static MIR.CameraMovementComponent;

namespace MIR;

/// <summary>
/// The animation debug testing Scene.
/// </summary>
public static class AnimationsScene
{
    public static Scene Create(Game game)
    {
        game.State.Time.TimeScale = 1;
        game.AudioRenderer.StopAll();

        var scene = SceneUtils.PrepareGameScene(game, GameMode.Unknown, true, null);

        scene.UpdateSystems();

        scene.AttachComponent(scene.CreateEntity(), new AnimationTestingComponent());

        scene.AddSystem(new AnimationTestingSystem());
        scene.RemoveSystem<PlayerUISystem>();

        scene.UpdateSystems();

        if (scene.FindAnyComponent<CameraMovementComponent>(out var cam))
        {
            cam.Targets = [new FreeMoveTarget()];
        }

        return scene;
    }
}
