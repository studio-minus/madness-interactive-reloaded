using Walgelijk;

namespace MIR;

/// <summary>
/// Level selection scene 🍕
/// </summary>
public static class LevelSelectionMenuScene
{
    public static Scene Load(Game game) => SceneUtils.GetMenuScene(game, new LevelSelectMenuSystem(), nameof(LevelSelectionMenuScene));
}
