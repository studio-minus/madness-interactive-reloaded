using Walgelijk;

namespace MIR;

/// <summary>
/// The scene for the mod menu.
/// </summary>
public static class ModMenuScene
{
    public static Scene Load(Game game) => SceneUtils.GetMenuScene(game, new ModMenuSystem(), nameof(ModMenuScene));
}
