using System;
using System.Collections.Generic;
using Walgelijk;

namespace MIR;

public delegate PoolablePrefabComponent PrefabFunction(Scene scene);

/// <summary>
/// <see cref="Walgelijk.System"/> for managing prefab pools. 
/// <br></br>
/// Used for things that are spawned frequently, like muzzle flashes.
/// </summary>
public class PrefabPoolSystem : Walgelijk.System
{
    private readonly Dictionary<PrefabPoolID, EntityPool> pools = new();

    public bool TryRequest(PoolablePrefab instructions, out Entity prefab)
    {
        if (!pools.TryGetValue(instructions.Identity, out var pool))
        {
            pool = new EntityPool(instructions.Identity, Scene, instructions, 64);
            pools.Add(instructions.Identity, pool);
        }

        if (pool.TryRequest(out prefab))
            return true;

        prefab = default;
        return false;
    }

    public void ReturnToPool(Entity entity)
    {
        if (pools.TryGetValue(Scene.GetComponentFrom<PoolablePrefabComponent>(entity).OriginPool, out var pool))
            pool.ReturnToPool(entity);
    }

    public override void Update()
    {
        foreach (var item in Scene.GetAllComponentsOfType<ReturnToPrefabPoolAfterTimeComponent>())
        {
            if (Scene.GetComponentFrom<PoolablePrefabComponent>(item.Entity).IsInUse)
            {
                item.CurrentLifetime += Time.DeltaTime;
                if (item.CurrentLifetime >= item.TimeInSeconds)
                    ReturnToPool(item.Entity);
            }
        }
    }

    private class EntityPool
    {
        public PrefabPoolID Identity;

        private readonly Scene scene;
        private readonly PoolablePrefab instructions;
        private readonly int Capacity = 256;

        /// <summary>
        /// all entities ever created
        /// </summary>
        private readonly HashSet<Entity> created;
        /// <summary>
        /// all entities currently available to use
        /// </summary>
        private readonly List<Entity> available;

        public int AmountAvailable => available.Count;
        public int AmountCurrentlyInUse => created.Count - available.Count;

        public EntityPool(PrefabPoolID id, Scene scene, PoolablePrefab instructions, int capacity = 256)
        {
            Identity = id;

            this.scene = scene;
            this.instructions = instructions;
            Capacity = capacity;

            available = new(capacity);
            created = new(capacity);
        }

        public bool TryRequest(out Entity entity)
        {
            if (IsSomethingAvailable)
                return TryGetExistingFromPool(out entity);

            if (TryCreateNew(out _))
                return TryGetExistingFromPool(out entity);
            else
            {
                entity = default;
                return false;
            }
        }

        public void Clear()
        {
            foreach (var item in created)
                if (scene.HasEntity(item))
                    scene.RemoveEntity(item);

            available.Clear();
            created.Clear();
        }

        public void ReturnToPool(Entity entity)
        {
            var comp = scene.GetComponentFrom<PoolablePrefabComponent>(entity);

            if (created.Contains(entity))
            {
                //TODO dit kan sneller. je kan de PoolablePrefabComponent checken of hij niet meer in use is
                if (!comp.IsInUse || available.Contains(entity))
                    Logger.Warn($"Prefab {entity} was already in the pool...");
                else
                {
                    available.Add(entity);
                    comp.IsInUse = false;
                    //Logger.Debug($"Prefab {entity} returned to pool! ({AmountCurrentlyInUse}/{Capacity})");
                }

                instructions.OnReturn(scene, entity);
            }
            else
                throw new Exception($"Invalid entity {entity} returned to EntityPool. This entity didn't come from this place.");
        }

        private bool TryCreateNew(out PoolablePrefabComponent? prefab)
        {
            if (created.Count >= Capacity)
            {
                prefab = default;
                return false;
            }

            prefab = instructions.OnCreateNew(scene);
            prefab.OriginPool = Identity;
            created.Add(prefab.Entity);
            available.Add(prefab.Entity);
            return true;
        }

        private bool TryGetExistingFromPool(out Entity entity)
        {
            if (TryGetRandomAvailable(out entity))
            {
                available.Remove(entity);
                if (scene.HasEntity(entity))
                {
                    //Logger.Debug($"Prefab {entity} taken from pool! ({AmountCurrentlyInUse}/{Capacity})");
                    if (scene.TryGetComponentFrom<ReturnToPrefabPoolAfterTimeComponent>(entity, out var timer))
                        timer.CurrentLifetime = 0;
                    scene.GetComponentFrom<PoolablePrefabComponent>(entity).IsInUse = true;
                    return true;
                }
                else
                {
                    throw new Exception($"Pooled entity {entity} has been removed from the scene! This is not allowed and could cause issues.");
                    created.Remove(entity);
                    return false;
                }
            }

            return false;
        }

        private bool TryGetRandomAvailable(out Entity entity)
        {
            entity = default;

            if (available.Count == 0)
                return false;

            entity = Utilities.PickRandom(available);

            return true;
        }

        public bool IsResponsibleForEntity(Entity ent) => created.Contains(ent);
        public bool IsSomethingAvailable => available.Count > 0;
    }
}
