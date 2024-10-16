#pragma warning disable IDE1006 // Naming Styles
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Walgelijk;
using Walgelijk.Onion;
using Walgelijk.Physics;
using Walgelijk.SimpleDrawing;

namespace MIR;

/// <summary>
/// Static class containing console commands.
/// </summary>
public static class MadnessCommands
{
    private static Game game => MadnessInteractiveReloaded.Game ?? throw new Exception("Game may not be null");
    private static Scene scene => MadnessInteractiveReloaded.Game?.Scene ?? throw new Exception("Game may not be null");

    [Command(HelpString = "Writes all commands and their descriptions to \"cmd_list.md\" as a Markdown table. Intended for the documentation.")]
    public static CommandResult ExportCommandList()
    {
        var cmds = CommandProcessor.GetAllCommands();
        using var file = new StreamWriter("cmd_list.md", false);

        file.WriteLine("| Command | Description |");
        file.WriteLine("|:--------|-------------|");
        foreach (var item in cmds)
            file.WriteLine("| `{0}` | {1} |", item.Item1, item.Item2.CommandAttr.HelpString);

        file.Dispose();
        return CommandResult.Info($"Success! Written to \"{Path.GetFullPath("cmd_list.md")}\"");
    }

    [Command(HelpString = "Crash the game")]
    public static void Crash()
    {
        throw new ManualCrashException("Manual crash invoked :)");
    }

    [Command(HelpString = "Print the game version")]
    public static string GameVersion()
    {
        return MIR.GameVersion.Version.ToString();
    }

    [Command(HelpString = "Toggle physics debug")]
    public static CommandResult PhysicsDebug()
    {
        if (scene.HasSystem<PhysicsDebugSystem>())
        {
            scene.RemoveSystem<PhysicsDebugSystem>();
            return CommandResult.Info("Physics debug system removed!");
        }

        scene.AddSystem(new PhysicsDebugSystem());
        return CommandResult.Info("Physics debug system added");
    }

    [Command(HelpString = "Set the body count to the target body count")]
    public static CommandResult WinLevel()
    {
        bool isInLevel = scene.FindAnyComponent<GameModeComponent>(out var gameModeComponent) && gameModeComponent.Mode == GameMode.Campaign;

        if (!scene.FindAnyComponent<LevelProgressComponent>(out var lvlProgress) || Level.CurrentLevel == null || !isInLevel)
            return CommandResult.Error("This is not a story level so it can't be won");
        else
            scene.GetSystem<LevelProgressSystem>().ForceReachGoal();

        MadnessUtils.DelayPausable(1, () =>
        {
            scene.GetSystem<LevelProgressSystem>().TransitionToNextLevel();
        });

        return "Well done!";
    }

    [Command(HelpString = "Lists all levels")]
    public static CommandResult Levels()
    {
        StringBuilder str = new("All level keys:\n");
        foreach (var item in Registries.Levels.GetAllKeys())
            str.AppendFormat("\"{0}\"\n", item);

        return str.ToString();
    }

    [Command(HelpString = "Sets the level to the given level name. Enter \'SetLevel ??\' to see a list of available level names")]
    public static CommandResult SetLevel(string levelKey)
    {
        if (levelKey == "??")
            return Levels();

        bool isInLevel = scene.FindAnyComponent<GameModeComponent>(out var gameModeComponent) && gameModeComponent.Mode == GameMode.Campaign;
        LevelEntry? instance;
        if (string.IsNullOrWhiteSpace(levelKey))
            return CommandResult.Error("You can't give an empty string");
        else if (!Registries.Levels.TryGet(levelKey, out instance) || instance == null)
            return CommandResult.Error($"There is no level {levelKey}. Invoke \"{nameof(Levels)}\" to see your options");

        switch (instance.LevelType)
        {
            case LevelType.Unknown:
                break;
            case LevelType.Campaign:
                //MadnessUtils.StoreCurrentPlayerWeaponForNextLevel(game.Scene);
                MadnessUtils.TransitionScene(game => LevelLoadingScene.Create(game, instance.Level, SceneCacheSettings.NoCache));
                //game.Scene = CampaignScene.Create(game, Level.CurrentLevel, true);
                break;
            case LevelType.Experiment:
                MadnessUtils.TransitionScene(game => LevelLoadingScene.Create(game, instance.Level, SceneCacheSettings.NoCache));
                //game.Scene = ExperimentScene.Create(game, Level.CurrentLevel);
                break;
            default:
                break;
        }

        var stats = CampaignProgress.GetCurrentStats();
        if (stats != null)
        {
            var requestedProgress = CampaignProgress.GetCampaignLevelList().IndexOf(levelKey);
            if (requestedProgress != -1)
                stats.LevelIndex = requestedProgress;
        }

        return "Set level to " + levelKey;
    }

    [Command(HelpString = "Disables the enemy AI")]
    public static CommandResult DisableAI()
    {
        AiCharacterSystem.DisableAI = true;
        return "AI disabled";
    }

    [Command(HelpString = "Enables the enemy AI")]
    public static CommandResult EnableAI()
    {
        AiCharacterSystem.DisableAI = false;
        return "AI enabled";
    }

    [Command(HelpString = "Start the benchmark sequence")]
    public static CommandResult Benchmark()
    {
        game.Scene = BenchmarkScene.Create(game);
        return "Benchmark started";
    }

    [Command(HelpString = "Sets development mode")]
    public static CommandResult DevMode(bool value)
    {
        game.DevelopmentMode = value;
        return game.DevelopmentMode ? "Development mode enabled" : "Development mode disabled";
    }

    [Command(HelpString = "Set God mode for the player")]
    public static CommandResult God(bool enabled)
    {
        if (ImprobabilityDisks.All.TryGetValue("god", out var m))
            m.Enabled = enabled;
        else
            return CommandResult.Error("\"god\" modifier not found.");
        game.AudioRenderer.PlayOnce(Sounds.HigherPowers);
        return ImprobabilityDisks.IsEnabled("god") ? "Godmode enabled" : "Godmode disabled";
    }

    [Command(HelpString = "Stops all playing sounds")]
    public static void StopSound()
    {
        game.AudioRenderer.StopAll();
    }

    [Command(HelpString = "Sets autospawn")]
    public static CommandResult Autospawn(bool enabled)
    {
        AiCharacterSystem.AutoSpawn = enabled;
        return AiCharacterSystem.AutoSpawn ? "Autospawn enabled" : "Autospawn disabled";
    }

    [Command(HelpString = "Refill ammo, health, and dodge")]
    public static CommandResult RefillMotives()
    {
        if (scene.FindAnyComponent<PlayerComponent>(out var player))
        {
            var playerCharacter = scene.GetComponentFrom<CharacterComponent>(player.Entity);
            if (!playerCharacter.IsAlive)
                return CommandResult.Error("Failure to refill because you are dead. Invoke \"Revive\" to revive yourself.");
            game.AudioRenderer.PlayOnce(Sounds.HigherPowers);

            playerCharacter.DodgeMeter = playerCharacter.Stats.DodgeAbility;
            var head = scene.GetComponentFrom<BodyPartComponent>(playerCharacter.Positioning.Head.Entity);
            var body = scene.GetComponentFrom<BodyPartComponent>(playerCharacter.Positioning.Body.Entity);
            head.Health = head.MaxHealth;
            body.Health = body.MaxHealth;

            if (playerCharacter.EquippedWeapon.TryGet(scene, out var equipped))
                equipped.RemainingRounds = equipped.Data.RoundsPerMagazine;

            return "Ammo, health, and dodge replenished.";
        }
        else return CommandResult.Error("There is no player in the scene :(");
    }

    [Command(HelpString = "Sets or gets the current game master volume, ranging from 0.0 to 1.0")]
    public static string Volume(float set = -1)
    {
        if (set < 0)
            return "Current volume is " + game.AudioRenderer.Volume;

        set = Utilities.Clamp(set, 0, 1);
        game.AudioRenderer.Volume = set;
        return "Volume set to " + set;
    }

    //TODO move to utils
    [Command(HelpString = "Revives the player")]
    public static CommandResult Revive()
    {
        if (!scene.FindAnyComponent<GameModeComponent>(out var gameModeComponent))
            return CommandResult.Error("There is no game mode component");

        if (!scene.FindAnyComponent<CameraComponent>(out var camera) || !scene.TryGetComponentFrom<TransformComponent>(camera.Entity, out var cameraTransform))
            return CommandResult.Error("There is no camera in the scene");

        var spawnPos = cameraTransform.Position;

        if (scene.FindAnyComponent<PlayerComponent>(out var player))
        {
            var playerCharacter = scene.GetComponentFrom<CharacterComponent>(player.Entity);
            if (playerCharacter.IsAlive)
                return CommandResult.Error("You haven't died yet...");


            if (scene.TryGetComponentFrom<RagdollComponent>(player.Entity, out var ragdoll))
            {
                ragdoll.Delete(scene, false);
                spawnPos = default;
                int cc = 0;
                foreach (var c in ragdoll.Nodes)
                    if (c.TryGet(scene, out var node))
                    {
                        spawnPos += node.Position;
                        cc++;
                    }
                spawnPos /= cc;
            }

            game.AudioRenderer.Stop(Sounds.DeathMusic);

            playerCharacter.DropWeapon(scene);
            scene.RemoveEntity(player.Entity);
            //scene.RemoveEntity(phys.Empty);
            scene.RemoveEntity(playerCharacter.Positioning.Head.Entity);
            scene.RemoveEntity(playerCharacter.Positioning.Body.Entity);

            if (scene.TryGetEntityWithTag(Tags.PlayerDeathSequence, out var entity))
                scene.RemoveEntity(entity);

            foreach (var item in playerCharacter.Positioning.BodyDecorations)
                scene.RemoveEntity(item);

            foreach (var item in playerCharacter.Positioning.HeadDecorations)
                scene.RemoveEntity(item);

            foreach (var h in playerCharacter.Positioning.Hands)
                scene.RemoveEntity(h.Entity);

            foreach (var h in playerCharacter.Positioning.Feet)
                scene.RemoveEntity(h.Entity);
        }

        var p = Prefabs.CreatePlayer(scene, spawnPos);
        MadnessUtils.Flash(Colors.White, 0.5f);
        game.AudioRenderer.PlayOnce(Sounds.HigherPowers);

        return "Higher powers gave you another chance";
    }

    [Command(HelpString = "Set infinite ammo for the player")]
    public static CommandResult InfiniteAmmo(bool enabled)
    {
        if (ImprobabilityDisks.All.TryGetValue("infinite_ammo", out var m))
            m.Enabled = enabled;
        else
            return CommandResult.Error("\"infinite_ammo\" modifier not found.");
        game.AudioRenderer.PlayOnce(Sounds.HigherPowers);
        return ImprobabilityDisks.IsEnabled("infinite_ammo") ? "Higher powers granted you infinite ammo" : "Higher powers restored your munition normality";
    }

    [Command(HelpString = "Gives the player the weapon of their choice by codename. Enter \'Give ??\' to see a list of available weapon names")]
    public static CommandResult Give(string name)
    {
        if (name == "??")
            return string.Join("\n", Registries.Weapons.GetAllKeys());

        if (!scene.FindAnyComponent<PlayerComponent>(out var player))
            return CommandResult.Error("There is no player in the scene");

        var playerCharacter = scene.GetComponentFrom<CharacterComponent>(player.Entity);

        if (name == "all")
        {
            for (int i = 0; i < 100; i++)
                game.AudioRenderer.PlayOnce(Sounds.Conjure, 1, Utilities.RandomFloat(0.95f, 1.05f));

            foreach (var item in Registries.Weapons.GetAllValues())
                equip(Prefabs.CreateWeapon(scene, default, item).Entity);

            return "why";
        }

        if (Registries.Weapons.TryGet(name, out var instructions) && instructions != null)
            equip(Prefabs.CreateWeapon(scene, default, instructions).Entity);
        else
            return CommandResult.Error("There is no such weapon in the registry. Your options are:\n" + string.Join("\n", Registries.Weapons.GetAllKeys()));

        void equip(Entity weaponEntity)
        {
            playerCharacter.DropWeapon(scene);
            var weapon = scene.GetComponentFrom<WeaponComponent>(weaponEntity);
            playerCharacter.EquippedWeapon = new ComponentRef<WeaponComponent>(weaponEntity);
            weapon.Wielder = new ComponentRef<CharacterComponent>(player.Entity);
        }

        game.AudioRenderer.PlayOnce(Sounds.Conjure, 0.25f, 1, AudioTracks.UserInterface);
        return "Higher powers granted you a " + name;
    }

    [Command(HelpString = "Opens the level editor scene where developers can edit and create levels")]
    public static void LevelEditor()
    {
        game.Scene = LevelEditorScene.Load(game);
    }

    [Command(HelpString = "Opens the weapon editor scene where developers can edit and create weapons. Takes an optional filename argument to start editing an existing weapon.")]
    public static CommandResult WeaponEditor()
    {
        var weaponEditorScene = WeaponEditorScene.Load(game);
        game.Scene = weaponEditorScene;

        return "Opened WeaponEditor";
    }

    [Command(HelpString = "Opens the armour editor.")]
    public static CommandResult ArmourEditor()
    {
        var armourEditorScene = ArmourEditorScene.Load(game);
        game.Scene = armourEditorScene;
        return "Opened ArmourEditor";
    }

    [Command(HelpString = "Clear the immediate mode drawing cache")]
    public static void ClearDrawCache()
    {
        Draw.ClearPools();
        Draw.ClearTextMeshCache();
    }

    [Command(HelpString = "Opens the given debug scene. Enter '??' as a parameter to see a list of available debug scenes")]
    public static CommandResult Debug(string scene)
    {
        var dict = new Dictionary<string, Func<Game, Scene>>()
        {
            { "melee",  MeleeTestScene.Create },
            { "char",  CharacterCreationTestScene.Create },
            { "physics",  PhysicsTestScene.Create },
            { "text",  TextTestingScene.Create },
            { "cont",  ContinuousGameplayScene.Create },
            { "ragdoll",  RagdollTestScene.Create },
            { "gore",  GoreTestScene.Create },
            { "anim",  AnimationsScene.Create },
            { "camera",  CameraMovementTestScene.Create },
        };

        if (scene == "??")
            return string.Join("\n", dict.Keys);

        if (dict.TryGetValue(scene, out var func))
        {
            game.Scene = func(game);
            return $"Switch to {scene}";
        }

        return CommandResult.Error($"\"{scene}\" is not valid. The following scenes are available:\n" + string.Join("\n", dict.Keys));
    }

    [Command(HelpString = "Opens the given demo scene. Enter '??' as a parameter to see a list of available demo scenes")]
    public static CommandResult Demo(string scene)
    {
        var dict = new Dictionary<string, Func<Game, Scene>>()
        {
            { "char",  DemoScenes.LoadCharacterCustomisationDemo },
        };

        if (scene == "??")
            return string.Join("\n", dict.Keys);

        if (dict.TryGetValue(scene, out var func))
        {
            game.Scene = func(game);
            return $"Switch to {scene}";
        }

        return CommandResult.Error($"\"{scene}\" is not valid. The following scenes are available:\n" + string.Join("\n", dict.Keys));
    }

    [Command(HelpString = "Sets or gets the global time scale. This determines the speed of time-dependent operations")]
    public static CommandResult Timescale(float speed = float.NegativeInfinity)
    {
        if (float.IsNegativeInfinity(speed))
            return $"Current timescale is {game.State.Time.TimeScale}";

        if (speed < 0 || float.IsNaN(speed) || float.IsInfinity(speed))
            return CommandResult.Error($"{speed} is not a valid timescale. The value must be greater than zero and cannot be NaN or infinity");

        game.State.Time.TimeScale = speed;
        return $"Timescale set to {game.State.Time.TimeScale}";
    }

    [Command(HelpString = "Set the scene to the main menu")]
    public static void MainMenu()
    {
        game.AudioRenderer.StopAll();
        game.Scene = MainMenuScene.Load(game);
    }

    [Command(HelpString = "Set the Onion UI debug mode. E.g 'GuiDebug DrawBounds RaycastHit'")]
    private static CommandResult OnionDebug(string type)
    {
        int spanIndex = 0;
        var s = type.AsSpan();
        var final = (UiDebugOverlay)0;
        while (true)
        {
            var part = s[spanIndex..];
            var eaten = eatEnum(part, out var result);
            if (eaten > 0)
            {
                final |= result;
                spanIndex += eaten;
                if (spanIndex >= s.Length)
                    break;
            }
            else
                break;

            static int eatEnum(ReadOnlySpan<char> input, out UiDebugOverlay result)
            {
                for (int i = 0; i <= input.Length; i++)
                {
                    var ss = input[..i];
                    if (Enum.TryParse(ss, true, out result))
                        return i;
                }
                result = UiDebugOverlay.None;
                return 0;
            }
        }

        if (final >= 0)
        {
            scene.GetSystem<OnionSystem>().DebugOverlay = final;
            return $"Set GUI debug flags: {final}";
        }

        return CommandResult.Error($"Invalid debug flag value. Seperated by a space, you can only enter these values:\n{string.Join("\n", Enum.GetNames<UiDebugOverlay>())}");
    }
}
#pragma warning restore IDE1006 // Naming Styles
