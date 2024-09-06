using System;

namespace MIR;

/// <summary>
/// Flags for certain character responses/abilities
/// </summary>
[Flags] // μ
public enum CharacterFlags
{
    None = 0,

    /// <summary>
    /// Undamagable, unkillable
    /// </summary>
    Invincible = 1,

    /// <summary>
    /// This character can never be ragdolled (death animation on shot will also be disabled)
    /// </summary>
    NoRagdoll = 2,

    /// <summary>
    /// Will respond to thrown weapons and such
    /// </summary>
    AttackResponseThrownProjectile = 4,
    /// <summary>
    /// Will respond to melee combat hits
    /// </summary>
    AttackResponseMelee = 8,
    /// <summary>
    /// Will respond to bullet impact
    /// </summary>
    AttackResponseBullet = 16,

    /// <summary>
    /// Will flinch when hit with a non-fatal shot
    /// </summary>
    StunAnimationOnNonFatalShot = 32,

    /// <summary>
    /// The dead ragdoll will despawn on its own
    /// </summary>
    DeleteRagdoll = 64,

    // presets -----------------------------------

    /// <summary>
    /// The default flag mask
    /// </summary>
    Default = 
        AttackResponseThrownProjectile | 
        AttackResponseMelee | 
        DeleteRagdoll |
        AttackResponseBullet | 
        StunAnimationOnNonFatalShot
}
