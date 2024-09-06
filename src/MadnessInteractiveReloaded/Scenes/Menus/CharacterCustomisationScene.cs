using System;
using System.Numerics;
using Walgelijk;

namespace MIR;

/// <summary>
/// The scene for customizing the player character.
/// </summary>
public static class CharacterCreationScene
{
    private static readonly SceneId Id = nameof(CharacterCreationScene);

    public static Scene Load(Game game)
    {
        const string id = "character creation scene";
        if (game.SceneCache.TryGet(id, out var scene))
            return scene ?? throw new Exception("Null scene has somehow been registered");

        scene = SceneUtils.PrepareMenuScene(game, id);
        scene.UpdateSystems();
        game.State.Time.TimeScale = 1;

        scene.AddSystem(new CharacterCreationSystem());
        scene.AttachComponent(scene.CreateEntity(), new CharacterCreationComponent());

        {
            Level.CurrentLevel = null;

            var player = Prefabs.CreatePlayer(scene, default);

            var character = scene.GetComponentFrom<CharacterComponent>(player.Entity);
            character.AimTargetPosition = new Vector2(1000, 0);
            character.Positioning.IsFlipped = false;
            player.RespondToUserInput = false;


            character.Look = UserData.Instances.PlayerLook;
        }

        return scene;
    }
}
