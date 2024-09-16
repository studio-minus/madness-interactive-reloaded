using Walgelijk;

namespace MIR;

/// <summary>
/// The scene for the <see cref="ExperimentModeSystem"/>.
/// </summary>
public static class ExperimentScene
{
    public static Scene Create(Game game, Level lvl, SceneCacheSettings sceneCache)
    {
        Level.CurrentLevel = lvl;
        CampaignProgress.SetCampaign(null);

        if (PersistentSoundHandles.MainMenuMusic != null)
            game.AudioRenderer.Stop(PersistentSoundHandles.MainMenuMusic);
        if (PersistentSoundHandles.PauseMusic != null)
            game.AudioRenderer.Stop(PersistentSoundHandles.PauseMusic);
        if (PersistentSoundHandles.LevelMusic != null)
            game.AudioRenderer.Stop(PersistentSoundHandles.LevelMusic);

        PersistentSoundHandles.LevelMusic = null;

        if (sceneCache.ShouldCache && game.SceneCache.TryGet(sceneCache.CacheId.Value, out var cached))
        {
            Logger.Log("Loaded level from cache: " + sceneCache.CacheId);
            if (MadnessUtils.FindPlayer(cached, out _, out var character))
            {
                character.DeleteHeldWeapon(cached);
                MadnessUtils.EquipStoredWeapon(lvl, cached, character);
            }
            return cached;
        }

        game.State.Time.TimeScale = 1;

        var scene = SceneUtils.PrepareGameScene(game, GameMode.Experiment, sceneCache.CacheId, true, lvl);

        scene.AddSystem(new EnemySpawningSystem());
        scene.AddSystem(new ExperimentModeSystem());
        scene.AddSystem(new DjSystem());
        scene.AttachComponent(scene.CreateEntity(), new ExperimentModeComponent());

        AiCharacterSystem.AutoSpawn = false;
        Prefabs.CreateSceneTransition(scene, Transition.Entry);

        return scene;
    }
}
