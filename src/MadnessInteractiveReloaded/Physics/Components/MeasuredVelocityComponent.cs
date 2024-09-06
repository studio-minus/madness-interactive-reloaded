using System.Numerics;
using Walgelijk;

namespace MIR;

/// <summary>
/// Measures velocity at a fixed rate.
/// Used in <see cref="MeasuredVelocitySystem"/>.
/// </summary>
[RequiresComponents(typeof(TransformComponent))]
public class MeasuredVelocityComponent : Component
{
    public Vector2 PreviousTranslation;
    public float PreviousRotation;

    public Vector2 DeltaTranslation;
    public float DeltaRotation;

    public bool ValidState = false;
}