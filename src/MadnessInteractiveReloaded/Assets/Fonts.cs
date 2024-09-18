using Walgelijk;
using Walgelijk.AssetManager;

namespace MIR;

/// <summary>
/// A bunch of fonts we use for MIR.
/// <example>
/// <code>
/// // Example:
/// Draw.FontSize = 18;
/// Draw.Font = Fonts.Inter;
/// </code>
/// </example>
/// </summary>
public class Fonts
{
    public AssetRef<Font> Inter { get; set; } = Assets.Load<Font>("fonts/inter.wf");
    public AssetRef<Font> Impact { get; set; } = Assets.Load<Font>("fonts/impact.wf");
    public AssetRef<Font> Tipografia { get; set; } = Assets.Load<Font>("fonts/tipografia.wf");
    public AssetRef<Font> Toxigenesis { get; set; } = Assets.Load<Font>("fonts/toxigenesis.wf");
    public AssetRef<Font> Oxanium { get; set; } = Assets.Load<Font>("fonts/oxanium.wf");
    public AssetRef<Font> CascadiaMono { get; set; } = Assets.Load<Font>("fonts/cascadia-mono.wf");
}
