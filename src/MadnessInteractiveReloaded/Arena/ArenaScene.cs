using Walgelijk;

namespace MIR;

public static class ArenaScene
{
    public static Scene Create(Game game)
    {
        Level.CurrentLevel = null;
        CampaignProgress.SetCampaign(null);

        if (PersistentSoundHandles.MainMenuMusic != null)
            game.AudioRenderer.Stop(PersistentSoundHandles.MainMenuMusic);
        if (PersistentSoundHandles.PauseMusic != null)
            game.AudioRenderer.Stop(PersistentSoundHandles.PauseMusic);
        if (PersistentSoundHandles.LevelMusic != null)
            game.AudioRenderer.Stop(PersistentSoundHandles.LevelMusic);

        game.State.Time.TimeScale = 1;

        var scene = SceneUtils.PrepareGameScene(game, GameMode.Experiment, null, true, null);

        scene.AddSystem(new EnemySpawningSystem());
        scene.AddSystem(new ArenaModeSystem());
        var c = scene.AttachComponent(scene.CreateEntity(), new ArenaModeComponent());//🎈

        var waves = c.Waves.Value;

        AiCharacterSystem.AutoSpawn = false;
        Prefabs.CreateSceneTransition(scene, Transition.Entry);

        return scene;
    }
}
