using SkiaSharp;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Numerics;
using Walgelijk;
using Walgelijk.SimpleDrawing;

namespace MIR;

/// <summary>
/// Used for stamping sprites into the background layer.
/// </summary>
public static class Stamper
{
    /// <summary>
    /// Stamp a character at their curent position.
    /// </summary>
    public static void Stamp(Scene scene, CharacterPositioning pos)
    {
        if (scene.FindAnyComponent<StampCanvasComponent>(out var canvas))
        {
            int c = 6 + pos.BodyDecorations.Length + pos.HeadDecorations.Length;
            var stamps = ArrayPool<Stamped>.Shared.Rent(c);

            stamps[0] = new(scene.GetComponentFrom<QuadShapeComponent>(pos.Feet.First.Entity));
            stamps[1] = new(scene.GetComponentFrom<QuadShapeComponent>(pos.Feet.Second.Entity));
            stamps[2] = new(scene.GetComponentFrom<QuadShapeComponent>(pos.Hands.First.Entity));
            stamps[3] = new(scene.GetComponentFrom<QuadShapeComponent>(pos.Hands.Second.Entity));
            stamps[4] = new(scene.GetComponentFrom<BodyPartShapeComponent>(pos.Body.Entity));
            stamps[5] = new(scene.GetComponentFrom<BodyPartShapeComponent>(pos.Head.Entity));

            for (int i = 0; i < pos.BodyDecorations.Length; i++)
                stamps[6 + i] = new(scene.GetComponentFrom<QuadShapeComponent>(pos.BodyDecorations[i]));

            for (int i = 0; i < pos.HeadDecorations.Length; i++)
                stamps[6 + pos.BodyDecorations.Length + i] = new(scene.GetComponentFrom<QuadShapeComponent>(
                    pos.HeadDecorations[i]));

            Array.Sort(stamps, 0, c, new StampComparer());

            for (int i = 0; i < c; i++)
            {
                var stamp = stamps[i];
                scene.Game.RenderQueue.Add(canvas.CreateStampTask(stamp.Task), stamp.RenderOrder);
            }
        }
    }

    /// <summary>
    /// Stamp a shape at its curent position.
    /// </summary>
    public static void Stamp(Scene scene, ShapeComponent shape)
    {
        if (scene.FindAnyComponent<StampCanvasComponent>(out var canvas))
        {
            Stamped stamp = new(shape);
            scene.Game.RenderQueue.Add(canvas.CreateStampTask(stamp.Task), stamp.RenderOrder);
        }
    }

    /// <summary>
    /// Stamp a sprite at its current position
    /// </summary>
    public static void Stamp(Scene scene, SpriteComponent shape)
    {
        if (scene.FindAnyComponent<StampCanvasComponent>(out var canvas))
        {
            Stamped stamp = new(shape);
            scene.Game.RenderQueue.Add(canvas.CreateStampTask(stamp.Task), stamp.RenderOrder);
        }
    }

    public static void Stamp(Scene scene, RichCharacterComponent.SubSprite subsprite)
    {
        if (scene.FindAnyComponent<StampCanvasComponent>(out var canvas))
        {
            var transform = Matrix3x2.CreateScale(subsprite.Rectangle.GetSize()) * Matrix3x2.CreateTranslation(subsprite.Rectangle.GetCenter()) * subsprite.Transform;
            var subspriteTask = new ShapeRenderTask(PrimitiveMeshes.CenteredQuad, transform, DrawingMaterialCreator.Cache.Load(subsprite.Texture.Value));
            scene.Game.RenderQueue.Add(canvas.CreateStampTask(subspriteTask), subsprite.Order);
        }
    }

    private readonly struct Stamped
    {
        public readonly IRenderTask Task;
        public readonly RenderOrder RenderOrder;

        public Stamped(IRenderTask task, RenderOrder renderOrder)
        {
            Task = task;
            RenderOrder = renderOrder;
        }

        public Stamped(ShapeComponent shape)
        {
            Task = shape.RenderTask;
            RenderOrder = shape.RenderOrder;
        }

        public Stamped(SpriteComponent sprite)
        {
            Task = sprite.RenderTask;
            RenderOrder = sprite.RenderOrder;
        }
    }

    private struct StampComparer : IComparer<Stamped>
    {
        public int Compare(Stamped a, Stamped b)
        {
            if (a.RenderOrder > b.RenderOrder)
                return 1;
            if (a.RenderOrder < b.RenderOrder)
                return -1;
            return 0;
        }
    }
}