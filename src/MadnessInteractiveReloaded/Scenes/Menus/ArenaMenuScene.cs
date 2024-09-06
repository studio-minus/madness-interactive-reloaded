using Walgelijk;

namespace MIR;

public static class ArenaMenuScene
{
    public static Scene Load(Game game) => SceneUtils.GetMenuScene(game, new ArenaMenuSystem(), nameof(ArenaMenuScene));
} 