using Walgelijk;

namespace MIR;

/// <summary>
/// The scene for the main menu.
/// </summary>
public static class MainMenuScene
{
    public static Scene Load(Game game) => SceneUtils.GetMenuScene(game, new MainMenuSystem(), null);
}
