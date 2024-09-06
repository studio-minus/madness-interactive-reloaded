using System.Numerics;

namespace MIR;

/// <summary>
/// Emitted when something is shot or hit.
/// </summary>
/// <param name="Weapon"></param>
/// <param name="Point"></param>
/// <param name="Normal"></param>
/// <param name="Incoming"></param>
public record struct HitEvent(WeaponComponent? Weapon, Vector2 Point, Vector2 Normal, Vector2 Incoming)
{
    public static implicit operator (WeaponComponent? weapon, Vector2 point, Vector2 normal, Vector2 incoming)(HitEvent value)
        => (value.Weapon, value.Point, value.Normal, value.Incoming);

    public static implicit operator HitEvent((WeaponComponent? weapon, Vector2 point, Vector2 normal, Vector2 incoming) value)
        => new HitEvent(value.weapon, value.point, value.normal, value.incoming);
}
