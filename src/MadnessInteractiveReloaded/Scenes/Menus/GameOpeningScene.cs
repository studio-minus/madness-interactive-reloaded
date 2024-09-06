using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.SimpleDrawing;

namespace MIR;

/// <summary>
/// The splash screen when the game is opening.
/// </summary>
public static class GameOpeningScene
{
    public static Scene Create(Game game)
    {
        var scene = new Scene(game);
        var camera = scene.CreateEntity();

        scene.AttachComponent(camera, new TransformComponent());
        scene.AttachComponent(camera, new CameraComponent());
        scene.AddSystem(new TransformSystem());
        scene.AddSystem(new CameraSystem());
        scene.AddSystem(new GameOpeningSystem());

        // TODO scene persistence
        //scene.ShouldBeDisposedOnSceneChange = true;

        return scene;
    }

    public class GameOpeningSystem : Walgelijk.System
    {
        private const float AnimationDuration = 0.5f;
        private const float TransitionDelay = 0.5f;

        private float t = 0; // TODO stateful system is technically not allowed, but i doubt it matters here lmao

        public override void Update()
        {
            t += Time.DeltaTime;

            if (t > TransitionDelay)
                Game.Scene = GameLoadingScene.Create(Game);
        }

        public override void Render()
        {
            var r = new Rect(0, 0, Window.Width, Window.Height);

            Draw.Reset();
            Draw.ScreenSpace = true;

            Draw.Colour = Colors.Black;
            Draw.Quad(r);

            Draw.Colour = Utilities.Lerp(Colors.Red, Colors.Black, t / AnimationDuration);
            Draw.Image(Assets.Load<Texture>("textures/ui/studiominus.png").Value, r, ImageContainmentMode.Center);
        }
    }
}
