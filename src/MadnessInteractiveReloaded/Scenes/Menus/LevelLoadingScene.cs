using MIR.LevelEditor.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Walgelijk;

namespace MIR;

/// <summary>   
/// The scene for loading between levels.<br></br>
/// The game is pretty fast so you probably will only barely see this.
/// </summary>
public static class LevelLoadingScene
{
    public static Scene Create(Game game, Lazy<Level> levelToLoad, SceneCacheSettings sceneCache)
    {
        var scene = new Scene(game);
        var camera = scene.CreateEntity();
        scene.AttachComponent(camera, new TransformComponent());
        scene.AttachComponent(camera, new CameraComponent());
        scene.AddSystem(new TransformSystem());
        scene.AddSystem(new CameraSystem());
        scene.AddSystem(new LevelLoadingSystem());

        RoutineScheduler.Start(LoadLevelRoutine(levelToLoad, scene, sceneCache));

        return scene;
    }

    private static IEnumerator<IRoutineCommand> LoadLevelRoutine(Lazy<Level> levelToLoad, Scene scene, SceneCacheSettings sceneCache)
    {
        Level? level = null;

        if (levelToLoad.IsValueCreated)
            level = levelToLoad.Value;
        else
        {
            yield return AsyncTask(() =>
            {
                try
                {
                    level = levelToLoad.Value;
                }
#if DEBUG
                catch (Exception)
                {
                    throw;
#else
                catch (Exception e)
                {
                    //TODO deal with level loading failure (go back to main menu & show error?)
                    Logger.Error("Failed to load level: " + e.Message);
#endif
                }
            });
        }

        //TODO deal with level loading failure (go back to main menu & show error?)
        if (level == null)
        {
            Logger.Error("Attempt to load null level");
            yield break;
        }

        // compile scripts
        foreach (var item in level.Objects.OfType<LevelScript>())
        {
            var s = LevelScriptCache.Instance.Load(item.Code);
            if (s.IsOccupied)
                yield return new RoutineWaitUntil(() => !s.IsOccupied);
            if (s.Script == null)
                yield return AsyncTask(async () => await s.Build());
        }

        switch (level.LevelType)
        {
            case LevelType.Experiment:
                MadnessUtils.TransitionScene(g => ExperimentScene.Create(g, level, sceneCache));
                break;
            default:
            case LevelType.Unknown:
            case LevelType.Campaign:
                MadnessUtils.TransitionScene(g => CampaignScene.Create(g, level));
                break;
        }
        MadnessUtils.Delay(1f, scene.Dispose);
    }

    private static IRoutineCommand AsyncTask(Action action)
    {
        var t = Task.WhenAll(Task.Run(action), Task.Delay(16));
        return new RoutineWaitUntil(() => t.IsCompleted);
    }
}
