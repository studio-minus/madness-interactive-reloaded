namespace MIR;

using System;
using Walgelijk;

/// <summary>
/// The performance benchmark test scene.
/// </summary>
public static class BenchmarkScene
{
    public static Scene Create(Game game)
    {
        var lvl = Registries.Levels.Get("benchmark");
        var scene = SceneUtils.PrepareGameScene(game, GameMode.Experiment, true, lvl.Level.Value);
        scene.ScenePersistence = ScenePersistence.Dispose;

        game.State.Time.TimeScale = 1;
        game.UpdateRate = 0;
        ImprobabilityDisks.DisableAI = false;
        //GameModifiers.InfiniteAmmoPlayer = true;
        ImprobabilityDisks.AutoSpawn = true;
        //GameModifiers.GodPlayer = true;
        game.DevelopmentMode = false;

        scene.AddSystem(new EnemySpawningSystem());
        var benchmark = scene.AddSystem(new BenchmarkSystem());

        if (scene.FindAnyComponent<PlayerComponent>(out var player))
        {
            var character = scene.GetComponentFrom<CharacterComponent>(player.Entity);
            var weapon = Prefabs.CreateWeapon(scene, default, Registries.Weapons.Get("aug"));
            player.RespondToUserInput = false;
            character.EquipWeapon(scene, weapon);
        }
        else throw new Exception("No player found in benchmark scene");

        benchmark.ResetRecording();
        benchmark.IsRecording = true;

        return scene;
    }
}
