using System;
using System.Numerics;
using Walgelijk;
using Walgelijk.Physics;

namespace MIR;

/// <summary>
/// Used for drawing damage onto body parts.
/// </summary>
[RequiresComponents(typeof(PhysicsBodyComponent), typeof(TransformComponent))]
public class BodyPartShapeComponent : QuadShapeComponent, IDisposable
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
    /// How many slashes we currently have.
    /// Used for setting a shader uniform.
    /// </summary>
    public int SlashesCount = 0;

    /// <summary>
    /// The list of slashes.
    /// Used for setting a shader uniform.
    /// </summary>
    public Vector3[] Slashes = new Vector3[HoleMaxCount];

    /// <summary>
    /// How many inner holes we currently have.
    /// Used for setting a shader uniform.
    /// </summary>
    public int InnerCutoutHolesCount = 0;

    /// <summary>
    /// The list of inner holes.
    /// Used for setting a shader uniform.
    /// </summary>
    public Vector3[] InnerCutoutHoles = new Vector3[HoleMaxCount];

    /// <summary>
    /// If we need to re-apply the shader uniforms.
    /// </summary>
    public bool NeedsUpdate = true;

    /// <summary>
    /// Used for determining the depth of holes.
    /// </summary>
    public float ShotHeat = 0;

    /// <summary>
    /// What color will the blood spurts and stuff be?
    /// See <see cref="Prefabs.CreateBloodSpurt(Scene, Vector2, float, Color, float)"/>
    /// </summary>
    public Color BloodColour;

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    /// <param name="centered"></param>
    public BodyPartShapeComponent(bool centered) : base(centered)
    {
        ClearHoles();
    }

    /// <summary>
    /// Modify a hole
    /// </summary>
    /// <param name="i">The hole index in the hole array.</param>
    /// <param name="x">The new x position.</param>
    /// <param name="y">The new y position.</param>
    /// <param name="depth">The new depth.</param>
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
        InnerCutoutHolesCount = 0;
        SlashesCount = 0;

        for (int i = 0; i < HoleMaxCount; i++)
        {
            SetHole(i, 0, 0, -10000);
            InnerCutoutHoles[i] = new Vector3(0, 0, -10000);
        }
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
    /// For stuff like exit wounds.
    /// </summary>
    /// <param name="x">X position.</param>
    /// <param name="y">Y position.</param>
    /// <param name="depth">The depth.</param>
    public void TryAddInnerCutoutHole(float x, float y, float depth)
    {
        if (InnerCutoutHolesCount >= HoleMaxCount)
            return;

        InnerCutoutHoles[InnerCutoutHolesCount] = new Vector3(x, y, depth);
        InnerCutoutHolesCount++;
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
        //depth += ShotHeat;

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

        ShotHeat += 0.05f;

        return depth;
    }

    public void AddSlash(Vector2 localPos, float directionRad)
    {
        if (SlashesCount >= HoleMaxCount)
            return;

        Slashes[SlashesCount] = new(localPos, directionRad);
        SlashesCount++;
        NeedsUpdate = true;
    }

    public void Dispose()
    {
        BodyPartMaterialPool.Instance.ReturnToPool(Material);
    }
}
