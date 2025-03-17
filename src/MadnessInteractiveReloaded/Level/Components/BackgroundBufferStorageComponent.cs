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

    public static readonly Shader Shader = new Shader(BuiltInShaders.WorldSpaceVertex, Assets.Load<string>("shaders/decal-mask.frag").Value);

    public BackgroundBufferStorageComponent(RenderTexture buffer)
    {
        Buffer = buffer;
        Material = new Material(Shader)
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
