using System;
using System.Numerics;
using Walgelijk;

namespace MIR;

public class ApparelSpriteComponent : QuadShapeComponent, IDisposable
{
    /// <summary>
    /// Max number of holes.
    /// </summary>
    public const int HoleMaxCount = 32;

    /// <summary>
    /// The distance for which a new hole would be considered overlapping with
    /// an existing hole and thus not created.
    /// </summary>
    public const float Threshold = 1;

    /// <summary>
    /// How many holes we currently have.
    /// Used for setting a shader uniform.
    /// </summary>
    public int HolesCount = 0;

    /// <summary>
    /// The list of holes.
    /// Used for setting a shader uniform.
    /// </summary>
    public Vector3[] Holes = new Vector3[HoleMaxCount];

    /// <summary>
    /// If we need to re-apply the shader uniforms.
    /// </summary>
    public bool NeedsUpdate = true;

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    /// <param name="centered"></param>
    public ApparelSpriteComponent(bool centered) : base(centered)
    {
        ClearHoles();
    }

    /// <summary>
    /// Modify a hole
    /// </summary>
    public void SetHole(int i, float x, float y, float depth)
    {
        Holes[i] = new Vector3(x, y, depth);
        NeedsUpdate = true;
    }

    /// <summary>
    /// Erase all the holes.
    /// </summary>
    public void ClearHoles()
    {
        HolesCount = 0;
        for (int i = 0; i < HoleMaxCount; i++)
            SetHole(i, 0, 0, -10000);
        NeedsUpdate = true;
    }

    /// <summary>
    /// Get an overlapping hole at (x,y).
    /// </summary>
    /// <param name="x">X position.</param>
    /// <param name="y">Y position.</param>
    /// <returns>The index of the hole which is overlapping. Returns -1 if no hole was found.</returns>
    public int GetOverlappingHole(float x, float y)
    {
        var p = new Vector2(x, y);
        for (int i = 0; i < HolesCount; i++)
        {
            var h = Holes[i];
            var d = Vector2.Distance(new Vector2(h.X, h.Y), p) - h.Z + Threshold;
            if (d < Threshold)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// Try to add a hole.
    /// </summary>
    /// <param name="x">X position.</param>
    /// <param name="y">Y position.</param>
    /// <param name="depth">The depth.</param>
    /// <returns>The depth of the hole.</returns>
    public float TryAddHole(float x, float y, float depth)
    {
        int overlappingHole = GetOverlappingHole(x, y);
        if (overlappingHole != -1 && depth >= Holes[overlappingHole].Z)
        {
            var hole = Holes[overlappingHole];
            SetHole(overlappingHole, hole.X, hole.Y, depth);
        }
        else if (HolesCount < HoleMaxCount)
        {
            SetHole(HolesCount, x, y, depth);
            HolesCount++;
        }

        return depth;
    }

    public void SetPiece(IReadableTexture texture)
    {
        Material.SetUniform("mainTex", texture);
    }

    public void Dispose()
    {
        ApparelMaterialPool.Instance.ReturnToPool(Material);
    }
}