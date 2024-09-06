using MIR.LevelEditor.Objects;
using System.Numerics;
using Walgelijk;
using static MIR.CameraMovementComponent;

namespace MIR;

public static class ArmourEditorScene
{
    private static readonly SceneId sceneId = nameof(ArmourEditorScene);

    public static Scene Load(Game game)
    {
        if (game.SceneCache.TryGet(sceneId, out var scene))
            return scene;

        game.AudioRenderer.StopAll(AudioTracks.Music);
        game.State.Time.TimeScale = 1;

        scene = SceneUtils.PrepareGameScene(game, GameMode.Editor, sceneId, withPlayer: false, null);
        scene.RemoveSystem<PlayerUISystem>();
        scene.AddSystem(new ArmourEditorSystem());

        // (duston): we need a new instance of a CharacterLook so the game (Entry.cs) doesn't
        // overwrite the player's look instance when we close the game.
        {
            CharacterLook playerLook = new();
            if (Registries.Looks.TryGet("grunt", out var grunt))
                grunt.CopyTo(playerLook);
            else
                throw new System.Exception("Couldn't find \"grunt\" CharacterLook in the Look Registry!");

            var character = Prefabs.CreateCharacter(scene, new CharacterPrefabParams
            {
                // "Player", new Vector2(0, 128), Registries.Factions.Get("player"), playerLook, Registries.Stats.Get("player"),Tags.Player
                Name = "Player",
                Bottom = new Vector2(0,128),
                Faction = Registries.Factions["player"],
                Look = playerLook,
                Stats  = Registries.Stats["player"],
                Tag = Tags.Player
            });
            scene.AttachComponent(character.Entity, new PlayerComponent());
        }

        var entity = scene.CreateEntity();
        scene.AttachComponent(entity, new ArmourEditorComponent());

        if (scene.FindAnyComponent<CameraMovementComponent>(out var cam))
        {
            cam.Targets = [new FreeMoveTarget()];
        }

        game.SceneCache.Add(scene);

        return scene;
    }
}