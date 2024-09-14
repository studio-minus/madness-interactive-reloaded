using System.Numerics;
using Walgelijk;
using Walgelijk.Onion;
using Walgelijk.Onion.Controls;
using Walgelijk.SimpleDrawing;
using static MIR.CameraMovementComponent;

namespace MIR; //💋

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

        var body = Prefabs.CreateBodypartSprite(scene, new BodyPartMaterialParams
        {
            SkinTexture = Registries.Armour.Body["default_body"].Left,
            FleshTexture = Textures.Character.FleshBody,
            GoreTexture = Textures.Character.GoreBody,
            BloodColour = Colors.Red,
            Scale = 1
        });
        var head = Prefabs.CreateBodypartSprite(scene, new BodyPartMaterialParams
        {
            SkinTexture = Registries.Armour.Head["default_head"].Left,
            FleshTexture = Textures.Character.FleshHead,
            GoreTexture = Textures.Character.GoreHead,
            BloodColour = Colors.Red,
            Scale = 1
        });

        scene.GetComponentFrom<TransformComponent>(head).Position = new Vector2(50, 200);

        return scene;
    }

    public class GoreTestSystem : Walgelijk.System
    {
        public float Damage = 1;

        public override void Update()
        {
            if (!Ui.IsBeingUsed)
                foreach (var body in Scene.GetAllComponentsOfType<BodyPartShapeComponent>())
                {
                    var transform = Scene.GetComponentFrom<TransformComponent>(body.Entity);
                    Vector2 ToLocal(Vector2 global) => Vector2.Transform(global, transform.WorldToLocalMatrix);
                    var localMouse = ToLocal(Input.WorldMousePosition);

                    if (Input.IsButtonPressed(MouseButton.Left))
                    {
                        body.TryAddHole(localMouse.X, localMouse.Y, Damage);

                        if (Damage > 0.2f)
                        {
                            localMouse += Utilities.RandomPointInCircle() * 0.4f;
                            body.TryAddInnerCutoutHole(localMouse.X, localMouse.Y, Damage + 1);
                        }
                    }

                    if (Input.IsButtonPressed(MouseButton.Right))
                    {
                        body.AddSlash(localMouse, Utilities.RandomFloat() * float.Tau);
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

            Ui.Layout.Size(100, 32).StickTop().StickLeft().Move(0, 32);
            Ui.FloatSlider(ref Damage, Direction.Horizontal, (0, 1), label: "{0:0.##}");

            Draw.Reset();
            Draw.ScreenSpace = true;
            Draw.FontSize = 17;
            Draw.Font = Fonts.Oxanium.Value;
            Draw.Colour = Colors.White;
            Draw.Text("LMB to add bullet wound\nRMB to add slash", new Vector2(Window.Width - 15, 15), Vector2.One, HorizontalTextAlign.Right, VerticalTextAlign.Top);
        }
    }
}
