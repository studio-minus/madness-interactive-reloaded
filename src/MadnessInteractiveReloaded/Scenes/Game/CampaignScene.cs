namespace MIR;

using Walgelijk;
using Walgelijk.AssetManager;

/// <summary>
/// The generic Scene used for each level in the campaign.
/// </summary>
public static class CampaignScene
{
    public static Scene Create(Game game, Level lvl)
    {
        Level.CurrentLevel = lvl;

        game.State.Time.TimeScale = 1;
        var scene = SceneUtils.PrepareGameScene(game, GameMode.Campaign, true, lvl);

        scene.AddSystem(new EnemySpawningSystem());
        scene.AddSystem(new CampaignStatsTimerSystem());

        if (PersistentSoundHandles.MainMenuMusic != null)
            game.AudioRenderer.Stop(PersistentSoundHandles.MainMenuMusic);
        if (PersistentSoundHandles.PauseMusic != null)
            game.AudioRenderer.Stop(PersistentSoundHandles.PauseMusic);

        if (lvl.BackgroundMusic != AssetRef<StreamAudioData>.None)
        {
            var l = SoundCache.Instance.LoadMusic(lvl.BackgroundMusic.Value);
            if (PersistentSoundHandles.LevelMusic != l)
            {
                if (PersistentSoundHandles.LevelMusic != null)
                    game.AudioRenderer.Stop(PersistentSoundHandles.LevelMusic);
                PersistentSoundHandles.LevelMusic = l;
            }
            game.AudioRenderer.Play(l);
        }

        AiCharacterSystem.DisableAI = false;
        AiCharacterSystem.AutoSpawn = true;

        if (lvl.OpeningTransition)
            Prefabs.CreateSceneTransition(scene, Transition.Entry);

        // increment attempts stat
        if (CampaignProgress.TryGetCurrentStats(out var stats))
            stats.IncrementAttempts(lvl);

        return scene;
    }
}
