namespace MIR;

using Walgelijk;

/// <summary>
/// Cache for sprite materials.
/// </summary>
public class SpriteMaterialCreator : Cache<IReadableTexture, Material>
{
    public static readonly SpriteMaterialCreator Instance = new();

    protected override Material CreateNew(IReadableTexture texture)
    {
        Material mat = new(Shader.Default);
        mat.SetUniform(ShaderDefaults.MainTextureUniform, texture);
        return mat;
    }

    protected override void DisposeOf(Material loaded)
    {
        Game.Main.Window.Graphics.Delete(loaded);
    }
}
