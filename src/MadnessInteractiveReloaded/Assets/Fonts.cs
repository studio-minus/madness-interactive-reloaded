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
public static class Fonts
{
    public static readonly AssetRef<Font> Inter = Assets.Load<Font>("fonts/inter.wf");
    public static readonly AssetRef<Font> Impact = Assets.Load<Font>("fonts/impact.wf");
    public static readonly AssetRef<Font> Tipografia = Assets.Load<Font>("fonts/tipografia.wf");
    public static readonly AssetRef<Font> Toxigenesis = Assets.Load<Font>("fonts/toxigenesis.wf");
    public static readonly AssetRef<Font> Oxanium = Assets.Load<Font>("fonts/oxanium.wf");
    public static readonly AssetRef<Font> CascadiaMono = Assets.Load<Font>("fonts/cascadia-mono.wf");
}
