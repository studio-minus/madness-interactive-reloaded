using System;
using System.Collections.Generic;
using Walgelijk;
using Walgelijk.AssetManager;

namespace MIR;

/// <summary>
/// Helpers for creating flipbooks.
/// </summary>
public static class FlipbookMaterialCreator
{
    private static readonly Dictionary<int, Material> cache = new();
    private static readonly Shader fragmentShader = new Shader(ShaderDefaults.WorldSpaceVertex, Assets.Load<string>("shaders/flipbook.frag").Value);

    public static Material LoadMaterialFor(IReadableTexture flipbook, int columns, int rows, int frames, Color color, bool alphaClip, int cacheId)
    {
        var (r, g, b, a) = color.ToBytes(); //rough colour to make the hash work lol
        int id = HashCode.Combine(flipbook, r, g, b, a, HashCode.Combine(columns, rows, frames, alphaClip), cacheId);

        if (cache.TryGetValue(id, out var material))
            return material;

        material = CreateUncached(flipbook, columns, rows, color, alphaClip, frames);
        cache.Add(id, material);

        return material;
    }

    public static Material CreateUncached(IReadableTexture flipbook, int columns, int rows, Color color, bool alphaClip, int frames = 0)
    {
        var material = new Material(fragmentShader);
        SetupMaterial(material, flipbook, columns, rows, color, alphaClip);
        material.SetUniform("progress", 0f);
        material.SetUniform("frameCount", (float)frames);
        return material;
    }

    public static void SetupMaterial(Material material, IReadableTexture flipbook, int columns, int rows, Color color, bool alphaClip, int frames = 0)
    {
        material.SetUniform("mainTex", flipbook);
        material.SetUniform("rows", (float)rows);
        material.SetUniform("columns", (float)columns);
        material.SetUniform("tint", color);
        material.SetUniform("frameCount", (float)frames);
        material.SetUniform("alphaClip", alphaClip ? 1 : 0);
    }
}
