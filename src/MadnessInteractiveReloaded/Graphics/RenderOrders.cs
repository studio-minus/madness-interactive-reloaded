using Walgelijk;

namespace MIR;

/// <summary>
/// Defines render layer ranges for MRE. 
/// If unsure just use <see cref="RenderOrders.Default"/>.
/// </summary>
public static class RenderOrders
{
    /// <summary> -100, 0 </summary>
    public static readonly RenderOrder BackgroundBehind = new(-100);
    /// <summary> -100, 9 </summary>
    public static readonly RenderOrder BackgroundDecals = new(-100, 9);
    /// <summary> -100, 10 </summary>
    public static readonly RenderOrder BulletEjections = new(-100, 10);

    /// <summary> 0, 0 </summary>
    public static readonly RenderOrder Default = new(0);

    /// <summary> 1, 0 </summary>
    public static readonly RenderOrder RagdollsLower = new(1);
    /// <summary> 19, 0 </summary>
    public static readonly RenderOrder RagdollsUpper = new(20);
    /// <summary> 2, 0 </summary>
    public static readonly RenderOrder CharacterLower = new(20);
    /// <summary> 199, 0 </summary>
    public static readonly RenderOrder CharacterUpper = new(199);

    /// <summary> 200, 0 </summary>
    public static readonly RenderOrder Effects = new(200);

    /// <summary> 250, 0 </summary>
    public static readonly RenderOrder HighlightedItemsLower = new(250, 0);
    /// <summary> 250, 100 </summary>
    public static readonly RenderOrder HighlightedItemsUpper = new(250, 100);

    /// <summary> 300, 0 </summary>
    public static readonly RenderOrder BackgroundInFront = new(300);

    /// <summary> 300, 50 </summary>
    public static readonly RenderOrder ForegroundDecals = new(300, 50);

    /// <summary> 302, 0 </summary>
    public static readonly RenderOrder PlayerRagdoll = new(302);

    /// <summary> 350, 0 </summary>
    public static readonly RenderOrder UserInterface = new(350);
    /// <summary> 350, 1000 </summary>
    public static readonly RenderOrder Imgui = new(350, 1000);
    /// <summary> 350, int.MaxValue </summary>
    public static readonly RenderOrder UserInterfaceTop = new(350, int.MaxValue);
}
