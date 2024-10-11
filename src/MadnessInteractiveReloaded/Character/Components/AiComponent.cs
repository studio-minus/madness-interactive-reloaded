namespace MIR;

using System.Numerics;
using Walgelijk;

/// <summary>
/// For enemy character AI.
/// </summary>
public class AiComponent : Component
{
    /// <summary>
    /// The seed for random values for this AI component.
    /// E.g., adding noise to their aim.
    /// Gets seeded in <see cref="AiComponent.AiComponent"/>
    /// </summary>
    public readonly float Seed;
    
    /// <summary>
    /// If this AI won't try to attack anyone.
    /// </summary>
    public bool IsDocile = false;
    
    /// <summary>
    /// Does the AI have a target?
    /// </summary>
    public bool HasKillTarget;

    /// <summary>
    /// The Entity the AI is trying to kill.
    /// </summary>
    public ComponentRef<CharacterComponent> KillTarget;

    /// <summary>
    /// If the player is being attacked by too many enemies,
    /// some enemies will have this flag set as to not overwhelm the player.
    /// </summary>
    public bool TooBusyToAttack = false;

    /// <summary>
    /// Is the AI trying to pick up an item?
    /// </summary>
    public bool HasItemTarget;

    /// <summary>
    /// The item Entity that the AI is trying to pickup.
    /// </summary>
    public ComponentRef<WeaponComponent> ItemTarget;

    /// <summary>
    /// The AI will prioritise picking up a weapon.
    /// </summary>
    public bool WantsToPickupWeapon = true;

    /// <summary>
    /// Whether or not this AI respects the MaxSimultaneousAttackingEnemies property
    /// </summary>
    public bool WaitsForAttackTurn = true;

    /// <summary>
    /// Where the AI is aiming. This is separate from <see cref="CharacterComponent.AimTargetPosition"/> in that <see cref="AimingPosition"/> is where the AI <i>wants</i> to aim, and <see cref="CharacterComponent.AimTargetPosition"/> is where it is actually aiming.
    /// </summary>
    public Vector2 AimingPosition;

    public BoolPrev WantsToIronSight = new(false);
    public BoolPrev WantsToInteract = new(false);
    public BoolPrev WantsToShoot = new(false);
    public BoolPrev WantsToDoAccurateShot = new(false);
    public BoolPrev WantsToReload = new(false);
    public BoolPrev WantsToWalkLeft = new(false);
    public BoolPrev WantsToWalkRight = new(false);

    public float PanicLevel = 0;
    public float AttackModeDuration;
    public float WanderTargetPosition;
    public float WanderTargetRemainingTime;

    /// <summary>
    /// If the AI has had an <see cref="AccurateShotComponent"/> attached and
    /// is performing an accurate shot.
    /// </summary>
    public bool IsDoingAccurateShot = false;

    public void SyncVariables()
    {
        WantsToIronSight.Update();
        WantsToInteract.Update();
        WantsToShoot.Update();
        WantsToDoAccurateShot.Update();
        WantsToReload.Update();
        WantsToWalkLeft.Update();
        WantsToWalkRight.Update();
    }

    public AiComponent()
    {
        Seed = Utilities.RandomFloat(-500, 500);
    }

    public static float LastAccurateShotTime = 10000;
}
