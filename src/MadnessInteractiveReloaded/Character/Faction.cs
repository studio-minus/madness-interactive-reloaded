using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.AssetManager.Deserialisers;

namespace MIR;

/// <summary>
/// Represents a character "team"
/// </summary>
public class Faction
{
    public string Name;
    public readonly string Id;
    public readonly uint CollisionLayer;

    public uint AttackHitLayerComposite
    {
        get
        {
            uint enemy = CollisionLayers.AllCharacters;

            foreach (var item in FactionRelationships.GetAllies(Id))
                enemy &= ~Registries.Factions.Get(item).CollisionLayer;

            if (!IsEnemiesWith(this)) // if we are not our own enemy, make sure we can't hit ourselves
                enemy &= ~CollisionLayer;
            return enemy;
        }
    }

    private static uint lastAssignedLayer = CollisionLayers.CharacterStart;

    public Faction(string id, string name)
    {
        Id = id;
        Name = name;

        lastAssignedLayer = CollisionLayer = lastAssignedLayer << 1;
        Logger.Debug($"Assigned layer {CollisionLayer:b} to faction '{name}'");
    }

    public void AddAlly(Faction f)
    {
        if (f == this)
            throw new System.Exception("Faction can't ally itself");

        if (IsEnemiesWith(f))
            throw new System.Exception("Faction can't ally an enemy");

        FactionRelationships.Allies.Ensure(Id).Add(f.Id);
    }

    public void AddEnemy(Faction f)
    {
        if (f == this)
            throw new System.Exception("Faction can't be an enemy with itself");

        if (IsAlliedTo(f))
            throw new System.Exception("Faction can't be an enemy with an ally");

        FactionRelationships.Enemies.Ensure(Id).Add(f.Id);
    }

    public void ClearAllies()
    {
        if (FactionRelationships.Allies.TryGetValue(Id, out var set))
            set.Clear();
    }

    public void ClearEnemies()
    {
        if (FactionRelationships.Enemies.TryGetValue(Id, out var set))
            set.Clear();
    }

    public void RemoveAlly(Faction f)
    {
        FactionRelationships.Allies.Ensure(Id).Remove(f.Id);
    }

    public void RemoveEnemy(Faction f)
    {
        FactionRelationships.Enemies.Ensure(Id).Remove(f.Id);
    }

    public bool IsAlliedTo(Faction other)
    {
        if (ImprobabilityDisks.IsEnabled("everyone_evil"))
            return false;

        return FactionRelationships.AreAllies(Id, other.Id);
    }

    public bool IsEnemiesWith(Faction other)
    {
        if (ImprobabilityDisks.IsEnabled("everyone_evil"))
            return true;

        return FactionRelationships.AreEnemies(Id, other.Id);
    }

    public class FactionDeserialiser : IAssetDeserialiser<Faction>
    {
        public Faction Deserialise(Func<Stream> stream, in AssetMetadata assetMetadata)
        {
            using var input = stream();
            using var json = new StreamReader(input, leaveOpen: false);
            var v = JsonConvert.DeserializeObject<IntermediateFaction>(json.ReadToEnd());
            var faction = new Faction(Path.GetFileNameWithoutExtension(assetMetadata.Path), v.Name);

            foreach (var other in v.Enemies ?? [])
                FactionRelationships.Enemies.Ensure(faction.Id).Add(other);

            foreach (var other in v.Allies ?? [])
                FactionRelationships.Allies.Ensure(faction.Id).Add(other);

            return faction;
        }

        public bool IsCandidate(in AssetMetadata assetMetadata)
        {
            return assetMetadata.Path.EndsWith(".json", StringComparison.InvariantCultureIgnoreCase);
        }

        private struct IntermediateFaction
        {
            public string Name;
            public string[] Enemies, Allies;
        }
    }
}

public static class FactionRelationships
{
    public static Dictionary<string, HashSet<string>> Allies = [];
    public static Dictionary<string, HashSet<string>> Enemies = [];

    public static void Clear()
    {
        Allies.Clear();
        Enemies.Clear();
    }

    public static IEnumerable<string> GetEnemies(string a)
    {
        if (Enemies.TryGetValue(a, out var set))
            foreach (var other in set)
                if (other != a)
                    yield return a;
    }

    public static IEnumerable<string> GetAllies(string a)
    {
        if (Allies.TryGetValue(a, out var set))
            foreach (var other in set)
                if (other != a)
                    yield return a;
    }

    public static IEnumerable<Faction> GetEnemies(Faction a)
    {
        if (Enemies.TryGetValue(a.Id, out var set))
            foreach (var other in set)
                if (other != a.Id)
                    yield return a;
    }

    public static IEnumerable<Faction> GetAllies(Faction a)
    {
        if (Allies.TryGetValue(a.Id, out var set))
            foreach (var other in set)
                if (other != a.Id)
                    yield return a;
    }

    public static bool AreAllies(Faction a, Faction b) => AreAllies(a.Id, b.Id);

    public static bool AreAllies(string a, string b)
    {
        if (ImprobabilityDisks.IsEnabled("everyone_evil"))
            return false;

        if (Allies.TryGetValue(a, out var set))
            if (set.Contains(b))
                return true;

        return false;
    }

    public static bool AreEnemies(Faction a, Faction b) => AreEnemies(a.Id, b.Id);

    public static bool AreEnemies(string a, string b)
    {
        if (ImprobabilityDisks.IsEnabled("everyone_evil"))
            return true;

        if (Enemies.TryGetValue(a, out var set))
            if (set.Contains(b))
                return true;

        return false;
    }
}
