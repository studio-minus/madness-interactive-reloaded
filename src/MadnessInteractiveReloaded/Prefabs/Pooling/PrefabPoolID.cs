using System;

namespace MIR;

/// <summary>
/// Used for distinguishing prefab pools.
/// </summary>
public struct PrefabPoolID : IEquatable<PrefabPoolID>
{
    public int Identity;

    public PrefabPoolID(int identity)
    {
        Identity = identity;
    }

    public override bool Equals(object? obj)
    {
        return obj is PrefabPoolID iD &&
               Identity == iD.Identity;
    }

    public bool Equals(PrefabPoolID other) => other.Identity == Identity;

    public override int GetHashCode()
    {
        return HashCode.Combine(Identity);
    }

    public static bool operator ==(PrefabPoolID left, PrefabPoolID right)
    {
        return left.Identity == right.Identity;
    }

    public static bool operator !=(PrefabPoolID left, PrefabPoolID right)
    {
        return !(left == right);
    }
}