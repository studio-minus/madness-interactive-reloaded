namespace MIR;

using Walgelijk;
using Walgelijk.ParticleSystem;

/// <summary>
/// Cache for particle textures/materials.
/// </summary>
public class ParticleMaterialCreator : Cache<Texture, Material>
{
    public static readonly ParticleMaterialCreator Instance = new();

    public ParticleMaterialCreator() { }

    protected override Material CreateNew(Texture texture)
    {
        Material mat = new Material(Particle.DefaultMaterial.Shader);
        mat.SetUniform("mainTex", texture);
        return mat;
    }

    protected override void DisposeOf(Material loaded)
    {
        Game.Main.Window.Graphics.Delete(loaded);
    }
}
