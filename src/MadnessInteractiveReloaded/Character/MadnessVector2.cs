using System;
using System.Numerics;

namespace MIR;

/// <summary>
/// Includes methods for manual <see cref="Vector2"/> manipulation.
/// </summary>
public struct MadnessVector2
{
    //Debug mode: ON

    /// <summary>
    /// Transforms the given <see cref="Vector2"/> into a normalized version of its self, without cloning.
    /// </summary>
    /// <param name="toNormalize"></param>
    public static void SelfNormalize(ref Vector2 toNormalize)
    {
        toNormalize = toNormalize / toNormalize.Length();
    }

    /// <summary>
    /// Returns a new normalized version of the provided <see cref="Vector2"/>, without hardware acceleration.
    /// and without changine the <see cref="Vector2"/>
    /// </summary>
    /// <param name="ToNormalize"></param>
    /// <returns><see cref="Vector2"/></returns>
    public static Vector2 Normalize(Vector2 ToNormalize)
    {
        Vector2 NormalizedVector = ToNormalize / ToNormalize.Length();
        Console.WriteLine(ToNormalize + " Normalized: " NormalizedVector);

        return NormalizedVector;
    }
}
