using System;

namespace MIR;

/// <summary>
/// Animations can specify constraints that prevent the character from performing certain actions
/// </summary>
[Flags]
public enum AnimationConstraint : uint
{
    /// <summary>
    /// Character can do anything
    /// </summary>
    AllowAll = 0,

    /// <summary>
    /// Character can't do anything
    /// </summary>
    PreventAll = uint.MaxValue,

    /// <summary>
    /// Makes the character immortal
    /// </summary>
    PreventDying = 1 << 0,

    /// <summary>
    /// Prevents the character from walking in any direction
    /// </summary>
    PreventWalking = 1 << 1,

    /// <summary>
    /// Prevents the character from aiming their weapon around. The aiming direction will stay as it was before the constraint was set.
    /// </summary>
    PreventAiming = 1 << 2,

    /// <summary>
    /// Prevents the character from dodging
    /// </summary>
    PreventDodge = 1 << 3,

    /// <summary>
    /// Prevents the character from shooting their weapon
    /// </summary>
    PreventShooting = 1 << 4,

    /// <summary>
    /// Prevents the character from performing a melee attack
    /// </summary>
    PreventMelee = 1 << 5,

    /// <summary>
    /// Prevents the character from being shot
    /// </summary>
    PreventBeingShot = 1 << 6,

    /// <summary>
    /// Prevents the character from being hit with a melee attack
    /// </summary>
    PreventBeingMeleed = 1 << 7,

    /// <summary>
    /// If this character was about to ragdoll, it'll simply not do so
    /// </summary>
    PreventRagdoll = 1 << 8,

    /// <summary>
    /// Character cannot pick anything up or interact with the environment
    /// </summary>
    PreventWorldInteraction = 1 << 9,

    /// <summary>
    /// Character can't throw
    /// </summary>
    PreventThrowing = 1 << 10,

    /// <summary>
    /// Character can't look through their iron sights / block. Identical to <see cref="PreventBlock"/>
    /// </summary>
    PreventIronSight = 1 << 11,

    /// <summary>
    /// Character can't block / look through their iron sights. Identical to <see cref="PreventIronSight"/>
    /// </summary>
    PreventBlock = PreventIronSight,

    /// <summary>
    /// The animation should blend between ANIMATED and NON-ANIMATED states immediately, without a transition.
    /// Used for looping animations, entrance animations, and exit animations
    /// </summary>
    PreventMixTransition = 1 << 12,

    /// <summary>
    /// Prevent the character from flipping around. flipping it!!
    /// </summary>
    PreventFlip = 1 << 13,

    /// <summary>
    /// Force the character to look forwards. This is different from <see cref="PreventAiming"/> in that it doesn't "freeze" the aiming direction to what it was prior to setting the constraint, it simply forces a certain aiming direction
    /// </summary>
    FaceForwards = 1 << 14,

    /// <summary>
    /// This character can die but they will not perform a death animation
    /// </summary>
    PreventDeathAnimation = 1 << 15,

    // COMBO TIME

    PreventAllAttacking = PreventMelee | PreventShooting | PreventThrowing,
    PreventAllMovement = PreventWalking | PreventAiming | PreventFlip,
    PreventAllDamage = PreventBeingShot | PreventBeingMeleed,
}