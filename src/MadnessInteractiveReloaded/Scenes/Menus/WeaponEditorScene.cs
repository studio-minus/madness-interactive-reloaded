using System;
using Walgelijk;
using static MIR.CameraMovementComponent;

namespace MIR;

/// <summary>
/// The scene for the weapon editor.
/// </summary>
public static class WeaponEditorScene
{
    private static readonly SceneId Id = nameof(WeaponEditorScene);

    public static Scene Load(Game game)
    {
        game.AudioRenderer.StopAll(AudioTracks.Music);

        if (game.SceneCache.TryGet(Id, out var scene))
            return scene ?? throw new Exception("Null scene has somehow been registered");

        scene = SceneUtils.PrepareMenuScene(game, Id);
        game.State.Time.TimeScale = 1;

        if (scene.FindAnyComponent<GameModeComponent>(out var gm))
            gm.Mode = GameMode.Editor;

        scene.AttachComponent(scene.CreateEntity(), new WeaponEditorComponent());

        scene.AddSystem(new WeaponEditorSystem());
        scene.AddSystem(new CameraMovementSystem());
        if (scene.FindAnyComponent<CameraComponent>(out var camera))
            scene.AttachComponent(camera.Entity, new CameraMovementComponent()).Targets.Add(new FreeMoveTarget());

        return scene;
    }
}
