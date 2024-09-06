using MIR.LevelEditor;
using MIR.LevelEditor.Objects;
using System;
using Walgelijk;
using Walgelijk.AssetManager;
using static MIR.CameraMovementComponent;

namespace MIR;

/// <summary>
/// The scene for the level editor.<br></br>
/// See: <see cref="LevelEditorSystem"/>.
/// </summary>
public static class LevelEditorScene
{
    private static readonly SceneId Id = nameof(LevelEditorScene);

    public static Scene Load(Game game)
    {
        game.State.Time.TimeScale = 1;
        game.AudioRenderer.StopAll(AudioTracks.Music);

        if (game.SceneCache.TryGet(Id, out var scene))
            return scene ?? throw new Exception("Null scene has somehow been registered");

        scene = SceneUtils.PrepareMenuScene(game, Id, false);

        var editor = scene.AttachComponent(scene.CreateEntity(), new LevelEditorComponent
        {
            Level = new Level() { BackgroundMusic = AssetRef<StreamAudioData>.None },
            Dirty = true
        });

        scene.AddSystem(new LevelEditorSystem());
        scene.AddSystem(new LevelEditorGuiSystem());

        scene.AddSystem(new CameraMovementSystem());
        if (scene.FindAnyComponent<CameraComponent>(out var camera))
            scene.AttachComponent(camera.Entity, new CameraMovementComponent()).Targets.Add(new FreeMoveTarget());

        if (editor.Level != null)
            editor.Level.Objects.Add(new PlayerSpawn(editor));

        return scene;
    }

}
