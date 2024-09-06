using System;
using System.Collections.Generic;
using System.Linq;
using Walgelijk;
using Walgelijk.Onion;
using Walgelijk.Onion.Controls;
using static MIR.CameraMovementComponent;

namespace MIR;

/// <summary>
/// The scene for testing the weapon out that you're editing in the <see cref="WeaponEditorSystem"/>.
/// </summary>
public static class WeaponEditorTestScene
{
    public static Scene Create(Game game, WeaponInstructions instr)
    {
        Level.CurrentLevel = Registries.Levels.Get("dbg_room").Level.Value;

        game.State.Time.TimeScale = 1;
        var scene = SceneUtils.PrepareGameScene(game, GameMode.Experiment, true, Level.CurrentLevel);

        scene.UpdateSystems();
        MadnessUtils.Delay(0.1f, () =>
        {
            if (game.Scene.FindAnyComponent<PlayerComponent>(out var player))
            {
                var playerCharacter = game.Scene.GetComponentFrom<CharacterComponent>(player.Entity);
                var weapon = Prefabs.CreateWeapon(scene, default, instr);
                playerCharacter.EquipWeapon(scene, weapon);
                playerCharacter.Flags |= CharacterFlags.Invincible;
            }
        });

        scene.AddSystem(new EnemySpawningSystem());
        scene.AddSystem(new WeaponEditorTestSystem());

        ImprobabilityDisks.AutoSpawn = true;

        game.AudioRenderer.StopAll(AudioTracks.Music);

        Prefabs.CreateSceneTransition(scene, Transition.Entry);

        return scene;
    }
}

/// <summary>
/// The scene for testing the weapon out that you're editing in the <see cref="WeaponEditorSystem"/>.
/// </summary>
public static class CameraMovementTestScene
{
    public static Scene Create(Game game)
    {
        Level.CurrentLevel = Registries.Levels.Get("dbg_two_stairs").Level.Value;

        game.State.Time.TimeScale = 1;
        var scene = SceneUtils.PrepareGameScene(game, GameMode.Experiment, true, Level.CurrentLevel);

        scene.UpdateSystems();

        scene.AddSystem(new EnemySpawningSystem());
        scene.AddSystem(new CameraMovementTestSystem());

        ImprobabilityDisks.AutoSpawn = false;

        game.AudioRenderer.StopAll();

        return scene;
    }

    public class CameraMovementTestSystem : Walgelijk.System
    {
        private TypeWrapper[] choices;
        private int selectedToAdd;

        private class TypeWrapper
        {
            public Type Type;

            public override string ToString()
            {
                return Type.Name;
            }
        }

        public override void Update()
        {
            if (!Scene.FindAnyComponent<CameraMovementComponent>(out var m))
                return;

            if (choices == null)
                choices = [.. typeof(CameraMovementComponent.ITarget)
                    .Assembly.GetTypes()
                    .Where(t => t.IsAssignableTo(typeof(CameraMovementComponent.ITarget)))
                    .Where(t => t.IsClass)
                    .Select(d => new TypeWrapper { Type = d })];

            Ui.Layout.Size(200, 512).VerticalLayout();
            Ui.Theme.OutlineWidth(1).Once();
            Ui.StartScrollView(true);
            {
                int i = 0;
                foreach (var b in m.States)
                {
                    var r = b.Value;
                    Ui.Layout.FitWidth().StickLeft().StickTop().Height(32);
                    Ui.FloatSlider(ref r.Weight, Direction.Horizontal, (0, 1), label: b.Key.GetType().Name, identity: i++);
                    Ui.Layout.FitWidth().StickLeft().StickTop().Height(32);
                    if (Ui.Button("Transition", identity: i++))
                        m.TransitionTo(b.Key);

                    Ui.Spacer(16);
                }

                Ui.Layout.FitWidth().StickLeft().StickTop().Height(32);
                Ui.StartGroup();
                {
                    Ui.Layout.FitContainer(1, 1, false).Scale(-32, 0);
                    Ui.Dropdown(choices, ref selectedToAdd, true);

                    Ui.Layout.FitHeight(false).Width(32).StickRight(false);
                    if (Ui.Button("+"))
                    {
                        m.Targets.Add(Activator.CreateInstance(choices[selectedToAdd].Type) as ITarget);
                    }
                }
                Ui.End();
            }
            Ui.End();
        }
    }
}
