namespace MIR;

using Walgelijk;

/// <summary>
/// Describes a rotational <see cref="Curve{T}"/>
/// </summary>
public class AngleCurve : Curve<float>
{
    public AngleCurve(params Key[] keys) : base(keys) { }

    protected override float Lerp(float a, float b, float t)
    {
        //return Utilities.LerpAngle(a, b, t);
        return Utilities.Lerp(a, b, t);
    }
}
