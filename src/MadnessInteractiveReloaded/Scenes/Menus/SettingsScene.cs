using System;
using Walgelijk;

namespace MIR;

/// <summary>
/// The scene for the settings menu.
/// </summary>
public static class SettingsScene
{
    public static Scene Load(Game game) => SceneUtils.GetMenuScene(game, new SettingsSceneSystem(), nameof(SettingsScene));
}
