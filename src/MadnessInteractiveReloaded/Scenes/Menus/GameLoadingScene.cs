using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.Onion;

namespace MIR;

/// <summary>
/// The scene for loading the game.
/// </summary>
public static class GameLoadingScene
{
    public static event Action? OnFinishedLoading;

    public static readonly List<LoadingStep> LoadingSteps =
    [
        // these are the loading steps. i make them sound cool because its fun.
        // its in a list because im not sure if mods can contribute to game loading?

        //("Detecting host specifications", GameLoadingSteps.DetectHardware), // no need for this one as of now
        new("Loading mods", ModLoader.LoadModsFromSources),
        new("Reading localisations", Registries.LoadLanguages),
        new("Uploading frame sheets", GameLoadingSteps.LoadFlipbooks),
        new("Registering arsenal", Registries.LoadWeapons),
        new("Loading character animations", Registries.LoadAnimations),
        new("Preparing common animations", GameLoadingSteps.PreloadAnimations),
        new("Loading melee sequences", Registries.LoadMeleeSequences),
        new("Registering character skillsets", Registries.LoadStats),
        new("Reading wardrobe assets", Registries.LoadArmour),
        new("Registering character appearances", Registries.LoadLooks),
        new("Manifesting ideological hostility", Registries.LoadFactions),
        new("Detecting experiment presets", Registries.LoadCharacterPresets),
        new("Reading cutscenes", Registries.LoadCutscenes, false), // loading videos has to be done on the main thread for... some reason
        new("Assembling reality", Registries.LoadLevels),
        new("Initiating Club N", Registries.LoadDjTracks),
        new("Incident BGM", Registries.LoadIncidentMusicSet),
        new("Preloading audio", GameLoadingSteps.PreloadAudio),
        new("Warming up material pool", GameLoadingSteps.PrefillMaterialPool),
        new("Ensuring player appearance", GameLoadingSteps.LoadPlayerLook),
        new("Initialising convars", ConVars.Initialise),
        new("Preparing textures", GameLoadingSteps.PrepareTextures, false),
        new("Preparing fonts", GameLoadingSteps.PrepareFonts),
        //new("Constructing the arena", () => UserData.Instances.ArenaMode = ArenaModeSaves.Load()),
        new("Loading timeline", Registries.LoadCampaigns),
        new("Reading progression", Registries.LoadCampaignsStats),
        new("Finalising", () =>
        {
            Materials.TrainMovingBackground.SetUniform(ShaderDefaults.MainTextureUniform, Textures.MovingView.Value);
            if (Registries.Campaigns.Count > 0)
                CampaignProgress.SetCampaign(Registries.Campaigns["employee_of_the_month"]);

            Onion.HoverSound = Sounds.UiHover;
            Onion.ActiveSound = Sounds.UiPress;

            Settings.TryReadFromFile(UserData.Paths.Settings, ref UserData.Instances.Settings);
            UserData.Instances.Settings.Apply(Game.Main);
            UserData.Instances.UnlockedImprobabilityDisks = [..ImprobabilityDisks.LoadUnlocked()];
        }, false),
    ];

    /// <param name="reloadOnly">
    /// When true, this is a post-mod-toggle content reload rather than a cold start: the asset registry is
    /// rebuilt for the current set of active mods and the content registries are re-run, but mods themselves
    /// are NOT reloaded (that would re-extract them and throw on duplicate ids).
    /// </param>
    public static Scene Create(Game game, bool reloadOnly = false)
    {
        var scene = new Scene(game);
        game.AudioRenderer.StopAll();
        var camera = scene.CreateEntity();
        scene.AttachComponent(camera, new TransformComponent());
        scene.AttachComponent(camera, new CameraComponent());
        var gameLoadingComponent = scene.AttachComponent(scene.CreateEntity(), new GameLoadingComponent());

        scene.AddSystem(new TransformSystem());
        scene.AddSystem(new CameraSystem());
        scene.AddSystem(new GameLoadingSystem());

        var tickSound = SoundCache.Instance.LoadUISoundEffect(Assets.Load<FixedAudioData>("sounds/ui/tick.wav"));

        Assets.AssignLifetime("textures/loading_gear.png", new SceneLifetimeOperator());

        IEnumerator<IRoutineCommand> loadResourcesAndContinue()
        {
            gameLoadingComponent.Progress = 0;

            // on a reload, rebuild the package registry for the current active-mod set before re-running content
            if (reloadOnly)
            {
                yield return show("Rebuilding asset registry");
                ModLoader.RefreshAssetRegistry();
            }

            foreach (var item in LoadingSteps)
            {
                // re-running the mod load would re-extract mods and throw on duplicate ids; skip it on reload
                if (reloadOnly && item.Task == ModLoader.LoadModsFromSources)
                {
                    gameLoadingComponent.Progress += 1f / LoadingSteps.Count;
                    continue;
                }

                yield return show(item.Title);

                if (item.Async)
                    yield return asyncTask(item.Task);
                else
                    item.Task();

                gameLoadingComponent.Progress += 1f / LoadingSteps.Count;
            }

            yield return show("Welcome, " + Environment.UserName);
            game.AudioRenderer.Play(Sounds.UiConfirm);

            yield return new RoutineDelay(0.5f);

            OnFinishedLoading?.Invoke();
            game.Scene = MainMenuScene.Load(game);
            MadnessUtils.Delay(0.5f, scene.Dispose);
        }

        IRoutineCommand show(string a)
        {
            gameLoadingComponent.DisplayedText.Add(a.ToUpper());
            tickSound.Pitch = Utilities.MapRange(0, 1, 0.9f, 1, gameLoadingComponent.Progress);
            tickSound.ForceUpdate();
            game.AudioRenderer.Play(tickSound);
            return new RoutineDelay(0.05f);
        }

        static IRoutineCommand asyncTask(Action action)
        {
            var t = Task.Run(action);
            return new RoutineWaitUntil(() => t.IsCompleted);
        }

        RoutineScheduler.Start(loadResourcesAndContinue());
        return scene;
    }
}

public record struct LoadingStep(string Title, Action Task, bool Async = true);