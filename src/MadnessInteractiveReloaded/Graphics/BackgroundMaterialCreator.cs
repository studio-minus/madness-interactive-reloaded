using Walgelijk;
using Walgelijk.AssetManager;

namespace MIR;

/// <summary>
/// Caches background textures and their background/foreground materials.
/// </summary>
public class BackgroundMaterialCache : Cache<Texture, (Material bg, Material fg)>
{
    public static readonly BackgroundMaterialCache Instance = new();

    private readonly Shader front;
    private readonly Shader behind;

    public BackgroundMaterialCache()
    {
        front = new Shader(
            Shader.Default.VertexShader,
            Assets.Load<string>("shaders/background-front.frag").Value
            );

        behind = new Shader(
            Shader.Default.VertexShader,
            Assets.Load<string>("shaders/background-behind.frag").Value
            );
    }

    protected override (Material bg, Material fg) CreateNew(Texture texture)
    {
        Material bg = new(behind);
        bg.SetUniform(ShaderDefaults.MainTextureUniform, texture);

        Material fg = new(front);
        fg.SetUniform(ShaderDefaults.MainTextureUniform, texture);

        return (bg, fg);
    }

    protected override void DisposeOf((Material bg, Material fg) loaded)
    {
        Game.Main.Window.Graphics.Delete(loaded.bg);
        Game.Main.Window.Graphics.Delete(loaded.fg);
    }

    //public (Material bg, Material fg) Load(string resourcePath) => Load(Assets.Load<Texture>(resourcePath).Value);
}
