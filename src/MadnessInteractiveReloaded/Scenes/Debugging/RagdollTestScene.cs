using System;
using System.Numerics;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.Onion;
using Walgelijk.Onion.Controls;
using Walgelijk.SimpleDrawing;
using static MIR.CameraMovementComponent;
using static MIR.RagdollTestScene;

namespace MIR;

public static class GoreTestScene
{
    public static Scene Create(Game game)
    {
        game.State.Time.TimeScale = 1;
        game.AudioRenderer.StopAll();

        var scene = SceneUtils.PrepareGameScene(game, GameMode.Unknown, false, null);

        scene.UpdateSystems();

        scene.AddSystem(new GoreTestSystem());
        scene.RemoveSystem<PlayerUISystem>();

        scene.UpdateSystems();

        if (scene.FindAnyComponent<CameraMovementComponent>(out var cam))
            cam.Targets = [new FreeMoveTarget()];

        var body = Prefabs.CreateBodypartSprite(scene, Registries.Armour.Body["default_body"].Left.Value, Textures.Character.FleshBody, Colors.Red);
        var head = Prefabs.CreateBodypartSprite(scene, Registries.Armour.Head["default_head"].Left.Value, Textures.Character.FleshHead, Colors.Red);

        scene.GetComponentFrom<TransformComponent>(head).Position = new Vector2(50, 200);

        return scene;
    }

    public class GoreTestSystem : Walgelijk.System
    {
        public override void Update()
        {
            foreach (var body in Scene.GetAllComponentsOfType<BodyPartShapeComponent>())
            {
                var transform = Scene.GetComponentFrom<TransformComponent>(body.Entity);
                Vector2 ToLocal(Vector2 global) => Vector2.Transform(global, transform.WorldToLocalMatrix);
                var localMouse = ToLocal(Input.WorldMousePosition);

                if (Input.IsButtonPressed(MouseButton.Left))
                {
                    //body.TryAddInnerCutoutHole(localMouse.X, localMouse.Y, 22.211f);
                  //  body.AddSlash(localMouse, 0);
                    body.AddSlash(localMouse, Utilities.RandomFloat() * float.Tau);
                    //body.TryAddHole(localMouse.X, localMouse.Y, 0.11f);
                }
            }

            DrawUi();
        }

        private void DrawUi()
        {
            Ui.Layout.Size(100, 32).StickTop().StickLeft();
            if (Ui.Button("Clear"))
            {
                foreach (var body in Scene.GetAllComponentsOfType<BodyPartShapeComponent>())
                    body.ClearHoles();
            }
        }
    }
}

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
