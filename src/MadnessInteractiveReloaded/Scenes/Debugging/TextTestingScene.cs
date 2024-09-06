using System;
using System.Numerics;
using Walgelijk;
using Walgelijk.Onion;
using Walgelijk.Onion.Controls;
using Walgelijk.SimpleDrawing;
using static MIR.CameraMovementComponent;

namespace MIR;

/// <summary>
/// Debug scene for text rendering.
/// </summary>
public static class TextTestingScene
{
    public static Scene Create(Game game)
    {
        var scene = new Scene(game);
        game.AudioRenderer.StopAll();

        var camera = scene.CreateEntity();
        scene.AttachComponent(camera, new TransformComponent());
        scene.AttachComponent(camera, new CameraMovementComponent()).Targets.Add(new FreeMoveTarget());
        scene.AttachComponent(camera, new CameraComponent
        {
            OrthographicSize = 1,
            PixelsPerUnit = 1,
            Clear = true,
            ClearColour = Colors.WhiteSmoke
        });

        scene.AttachComponent(scene.CreateEntity(), new TextTestingComponent(new TextMeshGenerator
        {
            Color = Colors.Black,
            Font = Fonts.Inter,
            HorizontalAlign = HorizontalTextAlign.Left,
            VerticalAlign = VerticalTextAlign.Bottom
        }, "The quick brown fox jumps over the lazy dog")
        { Position = new Vector2(0, 0) });

        scene.AddSystem(new TransformSystem());
        scene.AddSystem(new TextTestingSystem());
        scene.AddSystem(new OnionSystem());
        scene.AddSystem(new CameraSystem());
        scene.AddSystem(new CameraMovementSystem());

        return scene;
    }

    public class TextTestingComponent : Component, IDisposable
    {
        public Vector2 Position;
        public readonly VertexBuffer TextMesh;
        public readonly TextMeshGenerator TextMeshGenerator;
        public readonly string Text;
        public readonly ActionRenderTask RenderTask;

        public TextTestingComponent(TextMeshGenerator textMeshGenerator, string text)
        {
            Text = text;
            TextMesh = new VertexBuffer(new Vertex[text.Length * 4], new uint[text.Length * 6]);
            TextMeshGenerator = textMeshGenerator;

            RenderTask = new ActionRenderTask((graphics) =>
            {
                graphics.CurrentTarget.ModelMatrix = Matrix4x4.CreateTranslation(new Vector3(Position, 0));
                graphics.Draw(TextMesh, TextMeshGenerator.Font.Material);
            });
        }

        public void Dispose()
        {
            TextMesh.Dispose();
        }
    }

    public class TextTestingSystem : Walgelijk.System
    {
        private static readonly Font[] fontList =
        {
            Fonts.Inter,
            Fonts.Oxanium,
            Fonts.Impact,
            Fonts.Tipografia,
            Fonts.Toxigenesis,
            Font.Default,
        };

        public override void Update()
        {
            foreach (var t in Scene.GetAllComponentsOfType<TextTestingComponent>())
            {
                Ui.Layout.Size(128, 32).Move(8);
                if (Ui.ClickButton("VAlign bottom"))
                    t.TextMeshGenerator.VerticalAlign = VerticalTextAlign.Bottom;

                Ui.Layout.Size(128, 32).Move(8 + 128, 8);
                if (Ui.ClickButton("VAlign middle"))
                    t.TextMeshGenerator.VerticalAlign = VerticalTextAlign.Middle;

                Ui.Layout.Size(128, 32).Move(8 + 128 * 2, 8);
                if (Ui.ClickButton("VAlign top"))
                    t.TextMeshGenerator.VerticalAlign = VerticalTextAlign.Top;

                Ui.Layout.Size(128, 32).Move(8, 8 + 32);
                if (Ui.ClickButton("HAlign left"))
                    t.TextMeshGenerator.HorizontalAlign = HorizontalTextAlign.Left;

                Ui.Layout.Size(128, 32).Move(8 + 128, 8 + 32);
                if (Ui.ClickButton("HAlign center"))
                    t.TextMeshGenerator.HorizontalAlign = HorizontalTextAlign.Center;

                Ui.Layout.Size(128, 32).Move(8 + 128 * 2, 8 + 32);
                if (Ui.ClickButton("HAlign right"))
                    t.TextMeshGenerator.HorizontalAlign = HorizontalTextAlign.Right;

                for (int i = 0; i < fontList.Length; i++)
                {
                    Ui.Layout.Size(128, 32).Move(8 + 128 * i, 8 + 64);
                    if (Ui.ClickButton(fontList[i].Name, identity: i))
                        t.TextMeshGenerator.Font = fontList[i];
                }
            }
        }

        public override void Render()
        {
            if (!Scene.FindAnyComponent<CameraComponent>(out var cam))
                return;

            Draw.Reset();

            foreach (var t in Scene.GetAllComponentsOfType<TextTestingComponent>())
            {
                var results = t.TextMeshGenerator.Generate(t.Text, t.TextMesh.Vertices, t.TextMesh.Indices);
                t.TextMesh.ForceUpdate();

                RenderQueue.Add(t.RenderTask);

                Draw.Colour = Colors.Transparent;

                Draw.OutlineColour = Colors.Orange;
                Draw.OutlineWidth = 4 * cam.OrthographicSize;
                Draw.Quad(results.LocalBounds.TopLeft + t.Position, results.LocalBounds.GetSize());

                Draw.OutlineColour = Colors.Blue;
                Draw.OutlineWidth = 4 * cam.OrthographicSize;
                Draw.Quad(results.LocalTextBounds.TopLeft + t.Position, results.LocalTextBounds.GetSize());

                Draw.OutlineWidth = 0;
                Draw.Colour = Colors.Green.WithAlpha(0.5f);
                Draw.Line(t.Position, t.Position + new Vector2(results.LocalBounds.Width, 0), 6 * cam.OrthographicSize);
            }
        }
    }
}