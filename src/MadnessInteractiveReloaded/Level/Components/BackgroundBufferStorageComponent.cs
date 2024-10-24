using System;
using Walgelijk;
using Walgelijk.AssetManager;

namespace MIR;

/// <summary>
/// Stores the final background buffer
/// </summary>
public sealed class BackgroundBufferStorageComponent : Component, IDisposable
{
    public readonly RenderTexture Buffer;
    public readonly Material Material;

    public BackgroundBufferStorageComponent(RenderTexture buffer)
    {
        Buffer = buffer;
        Material = new Material(new Shader(ShaderDefaults.WorldSpaceVertex, Assets.Load<string>("shaders/decal-mask.frag").Value))
        {
            BlendMode = BlendMode.Overwrite,
        };
    }

    public void Dispose()
    {
        Material.Dispose();
        Buffer.Dispose();
    }
}
