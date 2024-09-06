using Walgelijk;

namespace MIR;

/// <summary>
/// Render a shape with a specific mesh and material.
/// </summary>
public class CustomShapeComponent : ShapeComponent
{
    public CustomShapeComponent(VertexBuffer vertexBuffer, Material material)
    {
        this.VertexBuffer = vertexBuffer;
        this.RenderTask = new ShapeRenderTask(this.VertexBuffer, material: material);
    }
}