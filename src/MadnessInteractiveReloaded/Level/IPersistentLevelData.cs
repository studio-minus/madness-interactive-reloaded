using System.Collections.Generic;
using System.Numerics;
using Walgelijk;

namespace MIR;

/// <summary>
/// Provides getters for information that has to be read on level load to ensure persistence across levels
/// </summary>
public interface IPersistentLevelData
{
    PersistentEquippedWeapon? EquippedWeapon { get; }
    IEnumerable<SerialisedBuddy>? Buddies { get; }

    /// <summary>
    /// Erase existing buddies and spawn new ones according to the given list
    /// </summary>
    // TODO not sure if this should be here or at MadnessUtils or wherever
    public static void SpawnBuddies(Scene scene, Vector2 position, List<SerialisedBuddy> buddies)
    {
        // first, delete existing buddies
        foreach (var buddy in scene.GetAllComponentsOfType<PersistentBuddyComponent>())
        {
            var c = scene.GetComponentFrom<CharacterComponent>(buddy.Entity);
            c.DeleteHeldWeapon(scene);
            c.Delete(scene);
        }

        // then we spawn new ones!
        foreach (var buddy in buddies)
        {
            var buddyChar = Prefabs.CreateCharacter(scene, new CharacterPrefabParams
            {
                Bottom = position,
                Faction = Registries.Factions[buddy.Faction],
                Look = Registries.Looks[buddy.Look],
                Stats = Registries.Stats[buddy.Stats],
                Name = buddy.Name,
                Tag = buddy.Tag,
            });

            scene.AttachComponent(buddyChar.Entity, new PersistentBuddyComponent());
            scene.AttachComponent(buddyChar.Entity, new AiComponent());

            if (buddy.Weapon.HasValue && Registries.Weapons.TryGet(buddy.Weapon.Value.Key, out var wi))
            {
                var wpn = buddy.Weapon.Value;

                var weaponComponent = Prefabs.CreateWeapon(scene, position, wi);
                weaponComponent.RemainingRounds = wpn.Ammo;

                buddyChar.EquipWeapon(scene, weaponComponent);
            }
        }
    }

    /// <summary>
    /// Clear and set the given list to contain all buddies
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="buddies"></param>
    // TODO not sure if this should be here or at MadnessUtils or wherever
    public static void SetBuddies(Scene scene, List<SerialisedBuddy> buddies)
    {
        buddies.Clear();
        foreach (var buddy in scene.GetAllComponentsOfType<PersistentBuddyComponent>())
        {
            var buddyChar = scene.GetComponentFrom<CharacterComponent>(buddy.Entity);
            if (!buddyChar.IsAlive)
                continue;

            var serialised = new SerialisedBuddy
            {
                Name = buddyChar.Name,
            };

            scene.TryGetTag(buddyChar.Entity, out serialised.Tag);

            if (!Registries.Factions.TryGetKeyFor(buddyChar.Faction, out serialised.Faction!))
            {
                Logger.Error($"Failed to save buddy {buddyChar.Name}: faction does not exist");
                continue;
            }

            if (!Registries.Stats.TryGetKeyFor(buddyChar.Stats, out serialised.Stats!))
            {
                Logger.Error($"Failed to save buddy {buddyChar.Name}: stats do not exist");
                continue;
            }

            if (!Registries.Looks.TryGetKeyFor(buddyChar.Look, out serialised.Look!))
            {
                Logger.Error($"Failed to save buddy {buddyChar.Name}: look does not exist");
                continue;
            }

            if (buddyChar.EquippedWeapon.TryGet(scene, out var eq) && eq.RegistryKey != null)
            {
                if (!Registries.Weapons.Has(eq.RegistryKey))
                    Logger.Error($"Failed to save buddy's ({buddyChar.Name}) equipped weapon: key does not exist");
                else
                    serialised.Weapon = new PersistentEquippedWeapon
                    {
                        Ammo = eq.RemainingRounds,
                        Key = eq.RegistryKey,
                        InfiniteAmmo = eq.InfiniteAmmo
                    };
            }

            buddies.Add(serialised);
        }
    }
}