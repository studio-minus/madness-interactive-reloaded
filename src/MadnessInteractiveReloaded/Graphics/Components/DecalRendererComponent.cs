using System;
using System.Numerics;
using Walgelijk;

namespace MIR;

/// <summary>
/// This component isn't created for every <see cref="DecalComponent"/> but instead is usually used
/// for rendering many of one type of decal.
/// See <see cref="DecalSystem"/>.
/// <seealso cref="DecalSystem.Render"/>
/// </summary>
public class DecalRendererComponent : Component, IDisposable
{
    /// <summary>
    /// The max number of decals.
    /// </summary>
    public readonly int MaxDecalCount = 512;

    /// <summary>
    /// Decal index for the <see cref="DecalSystem"/>.
    /// </summary>
    public int CurrentDecalCount = 0;

    /// <summary>
    /// Same as the corresponding <see cref="DecalComponent"/>.
    /// </summary>
    public DecalType DecalType;

    /// <summary>
    /// Whether or not to use the "decalMask" shader uniform.
    /// <see cref="DecalMaterialCreator.DecalMaskUniform"/>
    /// </summary>
    public bool UseDecalMask = false;

    public int ProcessingIndex;

    public Material Material;

    public readonly InstancedShapeRenderTask RenderTask;
    public readonly VertexBuffer VertexBuffer;
    public RenderOrder RenderOrder = RenderOrders.BackgroundInFront.WithOrder(50);

    public DecalRendererComponent(DecalType decalType, int maxCount = 512)
    {
        MaxDecalCount = maxCount;
        DecalType = decalType;
        Material = DecalMaterialCreator.Instance.Load(DecalComponent.GetTextureForType(DecalType));

        VertexBuffer = new VertexBuffer(PrimitiveMeshes.CenteredQuad.Vertices, PrimitiveMeshes.CenteredQuad.Indices, new VertexAttributeArray[]{
                new Matrix4x4AttributeArray(new Matrix4x4[MaxDecalCount]), // transform
                new Vector4AttributeArray(new Vector4[MaxDecalCount]), // color
                new FloatAttributeArray(new float[MaxDecalCount]), // frame
            });

        RenderTask = new InstancedShapeRenderTask(VertexBuffer, Matrix3x2.Identity, Material);
    }

    public void Dispose()
    {
        VertexBuffer.Dispose();
    }

    public void Clear()
    {
        CurrentDecalCount = 0;
        ProcessingIndex = 0;
        VertexBuffer.ExtraDataHasChanged = true;
    }
}
