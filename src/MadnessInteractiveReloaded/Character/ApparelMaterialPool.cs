using System;
using System.Numerics;
using Walgelijk;

namespace MIR;

/// <summary>
/// A re-usable pool for apparel materials.
/// </summary>
public class ApparelMaterialPool : Pool<Material, ApparelMaterialParams>
{
    /// <summary>
    /// The singleton instance of <see cref="ApparelMaterialPool"/>.
    /// </summary>
    public static readonly ApparelMaterialPool Instance = new();

    private static readonly Material fallback = new(CharacterConstants.ApparelShader);

    protected override Material CreateFresh()
    {
        var mat = new Material(CharacterConstants.ApparelShader);
        return mat;
    }

    protected override Material GetOverCapacityFallback()
    {
        return fallback;
    }

    protected override void ResetObjectForNextUse(Material c, ApparelMaterialParams initialiser)
    {
        ResetMaterial(c);

        c.SetUniform(ShaderDefaults.MainTextureUniform, initialiser.Texture);
        c.SetUniform("scale", initialiser.Scale);
        c.SetUniform("seed", Utilities.RandomFloat());
    }

    public static void ResetMaterial(Material material)
    {
        material.SetUniform("holesCount", 0);
        material.SetUniform("holes", Array.Empty<Vector3>());
    }
}
