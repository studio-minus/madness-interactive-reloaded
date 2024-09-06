using Walgelijk;

namespace MIR;

/// <summary>
/// This component draws a texture onto the <see cref="BackgroundBufferStorageComponent"/>
/// </summary>
public class BackgroundBufferDrawerComponent : Component
{
    public Rect WorldRect;
    public readonly Texture Texture;

    public BackgroundBufferDrawerComponent(Texture texture, Rect worldRect)
    {
        Texture = texture;
        WorldRect = worldRect;
    }
}
