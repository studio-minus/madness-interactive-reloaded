namespace MIR;

using Walgelijk;
using Walgelijk.AssetManager;

/// <summary>
/// Material pool for doors.
/// </summary>
public class DoorMaterialPool : Pool<Material, float>
{
    public static readonly DoorMaterialPool Instance = new();

    private static readonly Material fallback = Material.DefaultTextured;

    protected override Material CreateFresh()
    {
        var mat = new Material(new Shader(
            BuiltInShaders.WorldSpaceVertex,
            Assets.Load<string>("shaders/door.frag").Value
            ));
        return mat;
    }

    protected override Material GetOverCapacityFallback() => fallback;

    protected override void ResetObjectForNextUse(Material c, float initialiser)
    {
        c.SetUniform(ShaderDefaults.MainTextureUniform, Textures.Door.Value);
        c.SetUniform(DoorComponent.IsOpenUniform, 0f);
        c.SetUniform(DoorComponent.TimeSinceChangeUniform, 5f);
    }

    public Material ForceCreateNew() => CreateFresh();
}
