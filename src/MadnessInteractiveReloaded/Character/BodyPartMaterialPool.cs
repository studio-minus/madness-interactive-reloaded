using Walgelijk;
using Walgelijk.AssetManager;

namespace MIR;

/// <summary>
/// A re-usable pool for body part materials instead of creating more and more.
/// </summary>
public class BodyPartMaterialPool : Pool<Material, BodyPartMaterialParams>
{
    private static readonly GlobalAssetId[] slashTextures = [.. Assets.EnumerateFolder("textures/blade_impact")];

    /// <summary>
    /// The singleton instance of <see cref="BodyPartMaterialPool"/>.
    /// </summary>
    public static readonly BodyPartMaterialPool Instance = new();

    private static readonly Material fallback = new(CharacterConstants.BodyPartShader);

    protected override Material CreateFresh()
    {
        var mat = new Material(CharacterConstants.BodyPartShader);
        return mat;
    }

    protected override Material GetOverCapacityFallback()
    {
        return fallback;
    }

    protected override void ResetObjectForNextUse(Material c, BodyPartMaterialParams initialiser)
    {
        DestructibleBodyPartSystem.ResetMaterial(c);
        var v3 = initialiser.BloodColour.RGB;

        var slashTexture = Assets.Load<Texture>(Utilities.PickRandom(slashTextures)).Value;
        slashTexture.WrapMode = WrapMode.Clamp;

        c.SetUniform("slashTex", slashTexture);
        c.SetUniform(ShaderDefaults.MainTextureUniform, initialiser.SkinTexture);
        c.SetUniform("scale", initialiser.Scale);
        c.SetUniform("fleshTex", initialiser.FleshTexture);
        c.SetUniform("goreTex", initialiser.GoreTexture);
        c.SetUniform("outerBloodColour", v3);
        c.SetUniform("innerBloodColour", v3 * 0.8f);
        c.SetUniform("seed", Utilities.RandomFloat());
    }
}

public struct BodyPartMaterialParams
{
    public Texture SkinTexture;
    public Texture GoreTexture;
    public Texture FleshTexture;
    public Color BloodColour;
    public float Scale;
}
