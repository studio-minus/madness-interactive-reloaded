using System.Numerics;
using static MIR.BulletEmitter;

namespace MIR;

/// <summary>
/// Emitted when something is shot or hit.
/// </summary>
public struct HitEvent
{
    public BulletParameters Params;
    public Vector2 Point, Normal;
}
