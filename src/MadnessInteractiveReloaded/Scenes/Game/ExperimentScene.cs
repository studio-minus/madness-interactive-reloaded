using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.AssetManager.Deserialisers;

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
        var c = scene.AttachComponent(scene.CreateEntity(), new ArenaModeComponent());

        var waves = c.Waves.Value;

        AiCharacterSystem.AutoSpawn = false;
        Prefabs.CreateSceneTransition(scene, Transition.Entry);

        return scene;
    }
}

public class ArenaModeSystem : Walgelijk.System
{

}

public class ArenaModeComponent : Component
{
    public AssetRef<ArenaModeWave[]> Waves = Assets.Load<ArenaModeWave[]>("data/arena/waves.json");
}

public class ArenaModeWave
{
    public EnemySpawnInstructions[] Enemies = [];
    public string[] Weapons = [];
    public float WeaponChance = 0.5f;
    public int EnemyCount;

    public class AssetDeserialiser : IAssetDeserialiser<ArenaModeWave[]>
    {
        public ArenaModeWave[] Deserialise(Func<Stream> stream, in AssetMetadata assetMetadata)
        {
            using var s = stream();
            using var reader = new StreamReader(s);
            var json = reader.ReadToEnd() ?? throw new Exception("arena mode wave file is empty");
            return JsonConvert.DeserializeObject<ArenaModeWave[]>(json) ?? [];
        }

        public bool IsCandidate(in AssetMetadata assetMetadata)
        {
            return assetMetadata.Tags?.Contains("arena_waves") ?? false;
        }
    }
}