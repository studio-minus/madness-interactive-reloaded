using System;
using Walgelijk;

namespace MIR;

//TODO ®€¤
[Obsolete]
public struct ContinuousGameplayScene
{
    public static Scene Create(Game game)
    {
        var lvl = new Level()
        {
            LevelBounds = new Rect(-2000, 0, 100_000, 1080),
        };
        Level.CurrentLevel = lvl;

        //lvl.EnemySpawnInstructions.Add(new EnemySpawnInstructions("agent", "agent"));
        //lvl.EnemySpawnInstructions.Add(new EnemySpawnInstructions("agent", "agent"));
        //lvl.EnemySpawnInstructions.Add(new EnemySpawnInstructions("grunt", "grunt"));

        game.State.Time.TimeScale = 1;
        var scene = SceneUtils.PrepareGameScene(game, GameMode.Campaign, true, lvl);

        scene.AddSystem(new EnemySpawningSystem());
        //scene.AddSystem(new ExperimentModeSystem());
        //scene.AttachComponent(scene.CreateEntity(), new ExperimentModeComponent());

        ImprobabilityDisks.AutoSpawn = false;

        //var splitmek = SoundCache.Instance.LoadMusic(Resources.Load<AudioData>("music/apimadness.ogg"));
        //if (!game.AudioRenderer.IsPlaying(splitmek))
        //{
        //    game.AudioRenderer.Play(splitmek);
        //}
        game.AudioRenderer.StopAll(AudioTracks.Music);

        Prefabs.CreateSceneTransition(scene, Transition.Entry);

        return scene;
    }
}
