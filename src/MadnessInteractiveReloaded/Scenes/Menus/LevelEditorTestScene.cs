using MIR.LevelEditor;
using System;
using System.Numerics;
using Walgelijk;
using Walgelijk.Physics;
using Walgelijk.SimpleDrawing;

namespace MIR;

/// <summary>
/// The scene for testing the level you are creating in the level editor.<br></br>
/// See: <see cref="LevelEditorSystem"/>.
/// </summary>
public static class LevelEditorTestScene
{
    public static Scene Create(Game game, Level lvl)
    {
        // reset level script cache to prevent a memory leak
        LevelScriptCache.Instance.UnloadAll();
        CampaignProgress.SetCampaign(null);
        Level.CurrentLevel = lvl;

        game.State.Time.TimeScale = 1;
        var scene = SceneUtils.PrepareGameScene(game, (GameMode)lvl.LevelType, true, lvl);

        //if (game.Scene != null && game.Scene.FindAnyComponent<PlayerComponent>(out var playerComponent, out var playerEntity))
        //{
        //    var playerCharacter = game.Scene.GetComponentFrom<CharacterComponent>(playerEntity);
        //    var weapon = Prefabs.CreateWeapon(scene, default, Registry.Weapons.GetRandomValue());
        //    playerCharacter.EquipWeapon(scene, playerEntity, weapon.Entity, weapon.Component);
        //}

        scene.AddSystem(new RaycastTestSystem());

        scene.AddSystem(new EnemySpawningSystem());
        scene.AddSystem(new LevelEditorTestSystem());

        if (lvl.LevelType == LevelType.Experiment)
        {
            scene.AttachComponent(scene.CreateEntity(), new ExperimentModeComponent());
            scene.AddSystem(new ExperimentModeSystem());
        }

        AiCharacterSystem.AutoSpawn = true;

        Prefabs.CreateSceneTransition(scene, Transition.Entry);

        return scene;
    }

    public class RaycastTestSystem : Walgelijk.System
    {
        public override void Update()
        {
            if (Input.IsKeyHeld(Key.R) && Input.IsKeyHeld(Key.LeftControl))
            {
                Draw.Reset();
                Draw.Colour = Colors.Magenta;
                var phys = Scene.GetSystem<PhysicsSystem>();
                for (int i = 0; i < 64; i++)
                {
                    float th = (float)i / 63 * MathF.Tau;
                    var dir = new Vector2(MathF.Cos(th), MathF.Sin(th));

                    if (phys.Raycast(Input.WorldMousePosition, dir, out var result))
                        Draw.Line(Input.WorldMousePosition, result.Position, 5);
                    else
                        Draw.Line(Input.WorldMousePosition, Input.WorldMousePosition + dir * 10000, 5);
                }
            }
        }
    }
}
