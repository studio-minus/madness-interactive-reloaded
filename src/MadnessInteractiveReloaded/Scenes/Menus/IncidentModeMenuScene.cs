using Walgelijk;

namespace MIR;

/// <summary>
/// The scene for the incident mode menu.
/// </summary>
public static class IncidentModeMenuScene
{
    public static Scene Load(Game game) => SceneUtils.GetMenuScene(game, new IncidentModeMenuSystem(), nameof(IncidentModeMenuScene));
}
