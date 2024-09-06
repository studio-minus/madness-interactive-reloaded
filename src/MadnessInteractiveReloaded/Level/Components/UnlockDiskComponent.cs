using System.Numerics;
using Walgelijk;
using Walgelijk.AssetManager;

namespace MIR; // 🎈

public class UnlockDiskComponent : Component
{
    public string DiskId = string.Empty;

    public RenderOrder RenderOrder;
    public AssetRef<Texture> Texture;
    public Vector2 Position;

    /// <summary>
    /// Radians
    /// </summary>
    public float Angle;

    public static float Size = 40;
}
