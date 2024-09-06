using System;
using System.Collections.Generic;
using System.Numerics;
using Walgelijk;

namespace MIR;

/// <summary>
/// Handles spawning enemies for a level.
/// </summary>
[SingleInstance]
public class EnemySpawningComponent : Component, ICloneable
{
    /// <summary>
    /// The list of ways to spawn enemies.
    /// See:
    /// <see cref="ISpawnInstructions"/>
    /// <seealso cref="EnemySpawnInstructions"/>
    /// <seealso cref="InstancedSpawnInstructions"/>
    /// </summary>
    public List<ISpawnInstructions>? SpawnInstructions;

    /// <summary>
    /// List of weapons from <see cref="Registries.Weapons"/>
    /// for enemies to spawn with.
    /// </summary>
    public List<string>? WeaponsToSpawnWith = [];

    /// <summary>
    /// How many enemies will spawn on this level.
    /// </summary>
    public int MaxEnemyCount = 10;

    /// <summary>
    /// The chance that an enemy will spawn with a weapon.
    /// <see cref="WeaponsToSpawnWith"/>
    /// </summary>
    public float WeaponChance = 0.05f;

    /// <summary>
    /// The rate to spawn enemies. (in seconds)
    /// </summary>
    public float Interval = 1;

    /// <summary>
    /// The positions we can spawn enemies from.
    /// </summary>
    public IList<Vector2>? SpawnPoints;

    /// <summary>
    /// The doors enemies can spawn out of.
    /// </summary>
    public IList<LevelEditor.Objects.Door>? Doors;

    /// <summary>
    /// Should any enemies be spawned
    /// </summary>
    public bool Enabled = true;

    /// <summary>
    /// Minimum amount of dropped weapons on the floor to stop spawning enemies with weapons
    /// </summary>
    public int DroppedWeaponAmountThreshold = 8;

    /// <summary>
    /// The spawn clock
    /// </summary>
    public float SpawnTimer = 0;

    public object Clone()
    {
        return MemberwiseClone();
    }
}
