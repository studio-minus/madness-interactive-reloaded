using System;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.Onion;
using Walgelijk.Onion.Controls;
using Walgelijk.SimpleDrawing;
using static MIR.CameraMovementComponent;

namespace MIR;

/// <summary>
/// Debug test scene for ragdolls.
/// </summary>
public static class RagdollTestScene
{
    public static Scene Create(Game game)
    {
        game.State.Time.TimeScale = 1;
        game.AudioRenderer.StopAll();

        var scene = SceneUtils.PrepareGameScene(game, GameMode.Unknown, true, null);

        scene.UpdateSystems();

        //scene.AttachComponent(scene.CreateEntity(), new AnimationTestingComponent());

        scene.AddSystem(new RagdollTestSystem());
        scene.RemoveSystem<PlayerUISystem>();

        scene.UpdateSystems();

        if (scene.FindAnyComponent<CameraMovementComponent>(out var cam))
        {
            cam.Targets = [new FreeMoveTarget()];
        }

        return scene;
    }

    public class RagdollTestSystem : Walgelijk.System
    {
        private const Key RagdollKey = Key.V;

        public override void Update()
        {
            if (Input.IsKeyPressed(RagdollKey) && MadnessUtils.FindPlayer(Scene, out var pl, out var ch))
            {
                ch.Kill();
                MadnessUtils.TurnIntoRagdoll(Scene, ch);
            }

            if (Input.IsKeyPressed(Key.R))
                MadnessCommands.Revive();
        }

        public override void Render()
        {
            Draw.Reset();
            Draw.Order = RenderOrder.Top;

            //UI
            float f = Time.TimeScale;
            Ui.Layout.Size(48, 128).StickLeft().StickTop();
            if (Ui.FloatSlider(ref f, Direction.Horizontal, (0, 1), 0.01f, "{0:P0}"))
                Time.TimeScale = f;

            Ui.Layout.PreferredSize().StickRight().StickTop();
            Ui.Label($"Press '{RagdollKey}' to turn yourself into a ragdoll");
        }
    }
}
