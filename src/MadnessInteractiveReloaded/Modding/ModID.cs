using System;

namespace MIR;

public readonly struct ModID(string id) : IEquatable<ModID>
{
    public readonly string Value = id;

    public override bool Equals(object? obj)
    {
        return obj is ModID iD && Equals(iD);
    }

    public bool Equals(ModID other)
    {
        return Value == other.Value;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Value);
    }

    public static bool operator ==(ModID left, ModID right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ModID left, ModID right)
    {
        return !(left == right);
    }

    public override string ToString() => this;

    public static implicit operator string(ModID s) => s.Value;
    public static implicit operator ModID(string s) => new(s);
}
