using System;
using Walgelijk;

namespace MIR;

/// <summary>
/// For the audio visualiser screen.
/// </summary>
public class DjScreenComponent : Component, IDisposable
{
    /// <summary>
    /// The viewport of the screen.
    /// </summary>
    public Rect WorldRect;

    /// <summary>
    /// The render texture to display the visualiser on.
    /// </summary>
    public readonly RenderTexture Target;

    /// <summary>
    /// The visualiser data. 
    /// </summary>
    public AudioVisualiser? Visualiser = null;

    /// <summary>
    /// The material used to draw the bar graph bars
    /// </summary>
    public readonly Material BarMaterial;

    public DjScreenComponent(Rect worldRect)
    {
        Target = new RenderTexture(256, 256, flags: RenderTargetFlags.None);
        WorldRect = worldRect;
        BarMaterial = new Material();
        BarMaterial.SetUniform("mainTex", Texture.White);
        BarMaterial.SetUniform("tint", Colors.Red);
    }

    public void Dispose()
    {
        Target.Dispose();
        BarMaterial.Dispose();
    }
}
//✨