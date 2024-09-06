using System.Numerics;
using Walgelijk;

namespace MIR;

/// <summary>
/// Something gets knocked when it gets hit.
/// </summary>
public class ImpactOffsetComponent : Component
{
    public Vector2 TranslationOffset;
    public float RotationOffset;

    public float DecaySpeed = 12;
}