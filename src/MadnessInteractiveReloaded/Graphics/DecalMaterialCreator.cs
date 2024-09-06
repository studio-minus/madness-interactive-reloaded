namespace MIR;

using Walgelijk;
using Walgelijk.AssetManager;

/// <summary>
/// Caches the spritesheets and materials for decals.
/// </summary>
public class DecalMaterialCreator : Cache<(int columns, Texture tex), Material>
{
    public static readonly DecalMaterialCreator Instance = new();
    public const string DecalMaskUniform = "decalMask";

    private static readonly Shader shader = new Shader(
        Assets.Load<string>("shaders/decal.vert").Value,
        Assets.Load<string>("shaders/decal.frag").Value
    );

    public DecalMaterialCreator() { }

    protected override Material CreateNew((int columns, Texture tex) v)
    {
        Material mat = new Material(shader);
        //TODO uniform string constants ofzo?
        mat.SetUniform(DecalMaskUniform, Texture.White);
        mat.SetUniform("mainTex", v.tex);
        mat.SetUniform("rows", 1f);
        mat.SetUniform("columns", (float)v.columns);
        return mat;
    }

    protected override void DisposeOf(Material loaded)
    {
        loaded.Dispose();
    }

    //public Material Load(int columns, string resourcePath) => Load((columns, Assets.Load<Texture>(resourcePath)));
}
