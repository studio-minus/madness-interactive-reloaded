using System;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.OpenTK;

namespace MIR.Test.GameTests;

public struct TestGameUtil
{
    public static Game Create()
    {
        Game game = new Game(new FakeWindow());

        if (!Assets.TryGetPackage("base", out _))
            MadnessInteractiveReloaded.PrepareResourceInitialise();
        Resources.RegisterType(typeof(AudioData), static p => new FixedAudioData([], 256, 2, 512));
        Resources.RegisterType(typeof(FixedAudioData), static p => new FixedAudioData([], 256, 2, 512));
        //Resources.RegisterType(typeof(StreamAudioData), static p => new StreamAudioData(() => new OggAudioStream(p), 256, 2, 512));
        Registries.ClearAll();
        Registries.LoadWeapons();
        Registries.LoadArmour();
        Registries.LoadStats();
        Registries.LoadLooks();
        Registries.LoadAnimations();
        Registries.LoadLevels();
        Registries.LoadMeleeSequences();
        Registries.LoadCutscenes();
        Registries.LoadCampaigns();
        game.Window.Initialise();

        return game;
    }

    public static void StepGame(TimeSpan span, Game game)
    {
        if (game.Window is FakeWindow fake)
        {
            var state = game.State;
            var dt = 1 / 60f;
            var fakeTime = TimeSpan.Zero;
            var window = fake;

            double accumulator = 0;
            double fixedUpdateClock = 0;

            while (fakeTime < span)
            {
                var unscaledDt = (float)dt;
                var scaledDt = (float)dt * state.Time.TimeScale;

                state.Time.DeltaTimeUnscaled = unscaledDt;
                state.Time.DeltaTime = scaledDt;

                state.Time.SecondsSinceSceneChange += unscaledDt;
                state.Time.SecondsSinceSceneChangeUnscaled += scaledDt;

                state.Time.SecondsSinceLoad += scaledDt;
                state.Time.SecondsSinceLoadUnscaled += unscaledDt;

                game.AudioRenderer.UpdateTracks();
                game.Console.Update();
                game.AudioRenderer.Process(dt);

                double fixedUpdateInterval = 1d / game.FixedUpdateRate;
                if (!game.Console.IsActive)
                {
                    fixedUpdateClock += dt * state.Time.TimeScale;
                    accumulator += scaledDt;
                    while (accumulator > fixedUpdateInterval)
                    {
                        game.Scene?.FixedUpdateSystems();
                        fixedUpdateClock = 0;
                        accumulator -= fixedUpdateInterval;
                    }

                    state.Time.FixedInterval = (float)fixedUpdateInterval;
                    state.Time.Interpolation = (float)((fixedUpdateClock % fixedUpdateInterval) / fixedUpdateInterval);

                    game.Scene?.UpdateSystems();
                }

                game.Profiling.Tick();

                window.LoopCycle();

                if (!window.IsOpen)
                    break;

                fakeTime += TimeSpan.FromSeconds(dt);
            }

        }
        else throw new Exception("game is not a fake game...");
    }
}
