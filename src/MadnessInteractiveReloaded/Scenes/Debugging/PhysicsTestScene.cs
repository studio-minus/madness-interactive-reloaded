using MIR.LevelEditor.Objects;
using System;
using System.Numerics;
using Walgelijk;
using Walgelijk.SimpleDrawing;
using static MIR.CameraMovementComponent;

namespace MIR;

/// <summary>
/// Debug test scene for physics simulation.
/// </summary>
public static class PhysicsTestScene
{
    public static Scene Create(Game game)
    {
        game.AudioRenderer.StopAll();
        var level = Registries.Levels.Get("dbg_room").Level.Value;
        var scene = SceneUtils.PrepareGameScene(game, GameMode.Unknown, false, level);

        scene.AddSystem(new PhysTestSystem());
        scene.AttachComponent(scene.CreateEntity(), new PhysTestComponent());

        scene.GetSystem<VerletPhysicsSystem>().DrawDebug = true;
        scene.AddSystem(new Walgelijk.Physics.PhysicsDebugSystem());

        if (scene.FindAnyComponent<CameraMovementComponent>(out var camera))
            camera.Targets = [new FreeMoveTarget()];

        return scene;
    }

    private static void AddBox(Scene scene, Vector2 point, Vector2 size)
    {
        size *= 0.5f;
        float radius = 16;

        var tl = scene.AttachComponent(scene.CreateEntity(), new VerletNodeComponent(point + new Vector2(-size.X, size.Y), radius));
        var tr = scene.AttachComponent(scene.CreateEntity(), new VerletNodeComponent(point + new Vector2(size.X, size.Y), radius));
        var bl = scene.AttachComponent(scene.CreateEntity(), new VerletNodeComponent(point + new Vector2(-size.X, -size.Y), radius));
        var br = scene.AttachComponent(scene.CreateEntity(), new VerletNodeComponent(point + new Vector2(size.X, -size.Y), radius));

        scene.AttachComponent(scene.CreateEntity(), new VerletLinkComponent(tl, tr));
        scene.AttachComponent(scene.CreateEntity(), new VerletLinkComponent(bl, br));
        scene.AttachComponent(scene.CreateEntity(), new VerletLinkComponent(tl, bl));
        scene.AttachComponent(scene.CreateEntity(), new VerletLinkComponent(tr, br));

        scene.AttachComponent(scene.CreateEntity(), new VerletLinkComponent(tl, br));
        scene.AttachComponent(scene.CreateEntity(), new VerletLinkComponent(tr, bl));
    }

    public class PhysTestComponent : Component
    {

    }

    public class PhysTestSystem : Walgelijk.System
    {
        public override void Update()
        {
            if (Input.IsKeyReleased(Key.Space))
                AddBox(Scene, Input.WorldMousePosition, new Vector2(Utilities.RandomFloat(128, 256)));
        }

        public override void Render()
        {
            var level = Level.CurrentLevel ?? throw new Exception("where is the level?"); //TODO better message
            Draw.Reset();
            Draw.Order = RenderOrders.BackgroundInFront.OffsetOrder(100);

            foreach (var item in level.Objects)
            {
                if (item is AllBlocker a)
                {
                    Draw.Colour = Colors.Purple.WithAlpha(0.2f);
                    Draw.Quad(a.Rectangle);
                }   
                if (item is RectWall w)
                {
                    Draw.Colour = Colors.Purple.WithAlpha(0.2f);
                    Draw.Quad(w.Rectangle);
                }
            }
        }
    }
}
