using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Walgelijk;

namespace MIR;

/// <summary>
/// Handles <see cref="DecalComponent"/> <seealso cref=DecalRendererComponent"/>.
/// </summary>
public class DecalSystem : Walgelijk.System
{
    public bool IsDirty = false;

    private static DecalRendererComponent? GetRendererFor(IEnumerable<DecalRendererComponent> coll, DecalComponent comp)
    {
        return coll.FirstOrDefault(b =>
        {
            return b.DecalType == comp.DecalType && b.RenderOrder == comp.RenderOrder;
        });
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void RenderBasicDecal(DecalRendererComponent renderer, DecalComponent decal, int index)
    {
        if (renderer.CurrentDecalCount <= index)
            renderer.CurrentDecalCount++;
        if (renderer.CurrentDecalCount >= renderer.MaxDecalCount)
            return;

        var positions = renderer.VertexBuffer.GetAttribute(0) as Matrix4x4AttributeArray ?? throw new Exception("Vertex attributes at 0 is null");
        var colors = renderer.VertexBuffer.GetAttribute(1) as Vector4AttributeArray ?? throw new Exception("Vertex attributes at 1 is null");
        var indices = renderer.VertexBuffer.GetAttribute(2) as FloatAttributeArray ?? throw new Exception("Vertex attributes at 2 is null");

        positions.Data[index] = new Matrix4x4(
            Matrix3x2.CreateScale(decal.Scale.X, decal.Scale.Y) *
            Matrix3x2.CreateRotation(decal.RotationDegrees * Utilities.DegToRad) *
            Matrix3x2.CreateTranslation(decal.Position.X, decal.Position.Y));
        colors.Data[index] = (Vector4)decal.Color;
        indices.Data[index] = (float)decal.FlipbookIndex;

        renderer.ProcessingIndex++;
    }

    public override void Render()
    {
#if DEBUG
        if (Input.IsKeyPressed(Key.F4))
            Prefabs.CreateBloodSplat(Scene, Input.WorldMousePosition, Utilities.RandomFloat(0, 360), Colors.Purple, 340);
#endif
        var allDecals = Scene.GetAllComponentsOfType<DecalComponent>();
        if (!allDecals.Any())
            return;

        bool hasMask = Scene.FindAnyComponent<BackgroundBufferStorageComponent>(out var mask);
        var decalRenderers = Scene.GetAllComponentsOfType<DecalRendererComponent>();

#if DEBUG
        if (Input.IsKeyPressed(Key.F9))
        {
            RemoveAllDecals(allDecals, decalRenderers);
            return;
        }
#endif

        if (IsDirty)
        {
            foreach (var item in allDecals)
            {
                var renderer = GetRendererFor(decalRenderers, item);
                if (renderer == null)
                    continue;

                var index = renderer.ProcessingIndex;
                var decal = item;

                if (index < renderer.MaxDecalCount)
                    switch (item.DecalType)
                    {
                        case DecalType.Blood:
                        case DecalType.BulletHole:
                            RenderBasicDecal(renderer, decal, index);
                            break;
                    }
            }
        }

        IsDirty = false;

        foreach (var renderer in decalRenderers)
        {
            if (hasMask && renderer.UseDecalMask)
                renderer.Material.SetUniform(DecalMaterialCreator.DecalMaskUniform, mask!.Buffer);

            var task = renderer.RenderTask;
            task.InstanceCount = Math.Min(renderer.CurrentDecalCount, renderer.MaxDecalCount);
            task.ModelMatrix = Matrix3x2.Identity;
            renderer.VertexBuffer.ExtraDataHasChanged = true;
            RenderQueue.Add(renderer.RenderTask, renderer.RenderOrder);

            renderer.ProcessingIndex = 0;
        }
    }

    /// <summary>
    /// Clear all decals.<br></br>The parameters are only used if you already have cached lists of <see cref="DecalComponent"/>s and <see cref="DecalRendererComponent"/>.
    /// <br></br>
    /// Otherwise, this method will perform a lookup anyway.
    /// </summary>
    /// <param name="allDecals"></param>
    /// <param name="decalRenderers"></param>
    public void RemoveAllDecals(IEnumerable<DecalComponent>? allDecals = null, IEnumerable<DecalRendererComponent>? decalRenderers = null)
    {
        allDecals ??= Scene.GetAllComponentsOfType<DecalComponent>();
        decalRenderers ??= Scene.GetAllComponentsOfType<DecalRendererComponent>();

        foreach (var item in allDecals)
            Scene.RemoveEntity(item.Entity);
        foreach (var renderer in decalRenderers)
            renderer.Clear();
    }
}
