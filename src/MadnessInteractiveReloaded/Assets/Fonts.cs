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
    public static AssetRef<Font> Inter => Assets.Load<Font>("fonts/inter.wf");
    public static AssetRef<Font> Impact => Assets.Load<Font>("fonts/impact.wf");
    public static AssetRef<Font> Tipografia => Assets.Load<Font>("fonts/tipografia.wf");
    public static AssetRef<Font> Toxigenesis => Assets.Load<Font>("fonts/toxigenesis.wf");
    public static AssetRef<Font> Oxanium => Assets.Load<Font>("fonts/oxanium.wf");
    public static AssetRef<Font> CascadiaMono => Assets.Load<Font>("fonts/cascadia-mono.wf");
}
