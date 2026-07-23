using System.Numerics;
using Walgelijk;
using Walgelijk.Physics;

namespace MIR;

/// <summary>
/// Detonates <see cref="ExplosiveWeaponComponent"/> shots into explosions at their impact point.
/// Detects a shot by watching the weapon's remaining rounds drop, then raycasts from the barrel
/// to find where the round lands and spawns an explosion there.
/// </summary>
public class ExplosiveWeaponSystem : Walgelijk.System
{
    public override void Update()
    {
        if (MadnessUtils.IsPaused(Scene) || MadnessUtils.EditingInExperimentMode(Scene))
            return;

        foreach (var explosive in Scene.GetAllComponentsOfType<ExplosiveWeaponComponent>())
        {
            if (!Scene.TryGetComponentFrom<WeaponComponent>(explosive.Entity, out var weapon))
                continue;

            // only react while wielded; reset the counter otherwise so re-pickup doesn't misfire
            if (!weapon.Wielder.TryGet(Scene, out var wielder))
            {
                explosive.PreviousRounds = int.MinValue;
                continue;
            }

            int rounds = weapon.RemainingRounds;
            if (explosive.PreviousRounds != int.MinValue && rounds < explosive.PreviousRounds)
            {
                var transform = Scene.GetComponentFrom<TransformComponent>(weapon.Entity);
                var barrel = WeaponSystem.GetBarrel(weapon, transform);

                uint mask = wielder.EnemyCollisionLayer | CollisionLayers.BlockPhysics;
                var pos = Scene.GetSystem<PhysicsSystem>().Raycast(
                    barrel.position, barrel.direction, out var hit, explosive.MaxTravel, mask,
                    ignore: wielder.AttackIgnoreCollision)
                    ? hit.Position
                    : barrel.position + barrel.direction * explosive.MaxTravel;

                Prefabs.CreateExplosion(Scene, pos, explosive.Radius, explosive.Damage, explosive.Knockback, wielder);
            }

            explosive.PreviousRounds = rounds;
        }
    }
}
