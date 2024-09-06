using Walgelijk;

namespace MIR;

/// <summary>
/// Automatically run through a spritesheet via the <see cref="FlipbookSystem"/>.
/// </summary>
public class FlipbookComponent : Component
{
    /// <summary>
    /// How far into the animation we are.
    /// </summary>
    public float CurrentTime = 0;

    /// <summary>
    /// How long is the animation?
    /// </summary>
    public float Duration = .5f;

    /// <summary>
    /// The material to render.
    /// </summary>
    public Material Material;

    /// <summary>
    /// If the entity should be deleted when the flipbook animation has run its course.
    /// </summary>
    public bool DeleteWhenDone = false;

    /// <summary>
    /// If the animation should repeat forever.
    /// </summary>
    public bool Loop = true;

    public FlipbookComponent(Material material)
    {
        Material = material;
    }
}
