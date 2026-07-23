using Walgelijk;

namespace MIR;

/// <summary>
/// Marks a firearm whose shots detonate into an explosion at their impact point.
/// Attach via a weapon's <see cref="WeaponData.AdditionalComponents"/> together with
/// <see cref="ExplosiveWeaponSystem"/> in <see cref="WeaponData.EnsureSystems"/>.
/// </summary>
public class ExplosiveWeaponComponent : Component
{
    /// <summary>
    /// Blast radius of each shot.
    /// </summary>
    public float Radius = 650;

    /// <summary>
    /// Damage at the epicentre, falling off to zero at the edge.
    /// </summary>
    public float Damage = 12;

    /// <summary>
    /// Ragdoll launch force at the centre.
    /// </summary>
    public float Knockback = 3000;

    /// <summary>
    /// How far the shot travels before detonating if it hits nothing.
    /// </summary>
    public float MaxTravel = 6000;

    /// <summary>
    /// Remaining rounds seen last frame, used to detect a shot being fired.
    /// </summary>
    public int PreviousRounds = int.MinValue;
}
