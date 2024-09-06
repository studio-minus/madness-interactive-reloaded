namespace MIR;

using System;
using Walgelijk;

/// <summary>
/// The game information and credits scene.
/// </summary>
public static class InformationScene
{
    private static readonly SceneId Id = nameof(InformationScene);

    public static Scene Load(Game game)
    {
        if (game.SceneCache.TryGet(Id, out var scene))
            return scene ?? throw new Exception("Null scene has somehow been registered");

        scene = SceneUtils.PrepareMenuScene(game, Id);
        scene.AddSystem(new InformationGuiSystem());

        return scene;
    }
}
