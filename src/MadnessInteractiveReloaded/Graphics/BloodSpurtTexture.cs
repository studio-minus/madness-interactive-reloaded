using System;
using Walgelijk;
using Walgelijk.AssetManager;

namespace MIR;

/// <summary>
/// See <see cref="Textures.BloodSpurts"/>.
/// </summary>
public struct BloodSpurtTexture
{
    public AssetRef<Texture> Asset;
    public int Rows;
    public int Columns;
    public float Size;
    public bool ShouldPointTowardsBulletDirection;

    public BloodSpurtTexture(AssetRef<Texture> path, int rows, int columns, float size, bool shouldPointTowardsBulletDirection)
    {
        this.Asset = path;
        this.Rows = rows;
        this.Columns = columns;
        this.Size = size;
        this.ShouldPointTowardsBulletDirection = shouldPointTowardsBulletDirection;
    }

    public override bool Equals(object? obj)
    {
        return obj is BloodSpurtTexture other &&
               Asset == other.Asset &&
               Rows == other.Rows &&
               Columns == other.Columns &&
               Size == other.Size &&
               ShouldPointTowardsBulletDirection == other.ShouldPointTowardsBulletDirection;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Asset, Rows, Columns, Size, ShouldPointTowardsBulletDirection);
    }

    public void Deconstruct(out AssetRef<Texture> asset, out int rows, out int columns, out float size, out bool shouldPointTowardsBulletDirection)
    {
        asset = this.Asset;
        rows = this.Rows;
        columns = this.Columns;
        size = this.Size;
        shouldPointTowardsBulletDirection = this.ShouldPointTowardsBulletDirection;
    }

    public static implicit operator (AssetRef<Texture> asset, int rows, int columns, float size, bool shouldPointTowardsBulletDirection)(BloodSpurtTexture value)
    {
        return (value.Asset, value.Rows, value.Columns, value.Size, value.ShouldPointTowardsBulletDirection);
    }

    public static implicit operator BloodSpurtTexture((AssetRef<Texture> asset, int rows, int columns, float size, bool shouldPointTowardsBulletDirection) value)
    {
        return new BloodSpurtTexture(value.asset, value.rows, value.columns, value.size, value.shouldPointTowardsBulletDirection);
    }

    public static bool operator ==(BloodSpurtTexture left, BloodSpurtTexture right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(BloodSpurtTexture left, BloodSpurtTexture right)
    {
        return !(left == right);
    }
}
