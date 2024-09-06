using Walgelijk;

namespace MIR;

/// <summary>
/// Improbabiliy disk selection scene  🎈
/// </summary>
public static class ImprobabilityDiskSelectionMenuScene
{
    public static Scene Load(Game game) => SceneUtils.GetMenuScene(game, new ImprobabilityDiskSelectMenuSystem(), nameof(ImprobabilityDiskSelectionMenuScene));
}
