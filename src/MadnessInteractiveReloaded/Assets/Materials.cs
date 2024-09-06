using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.SimpleDrawing;

namespace MIR;

/// <summary>
/// Some materials we use in specific places, such as for the train, spinning head loading icon, etc.
/// </summary>
public static class Materials
{
    public static readonly Material AccurateShotAxis = new Material(SpriteMaterialCreator.Instance.Load(Texture.White));

    public static readonly Material RedPostProcessor = new Material(new Shader(
        ShaderDefaults.WorldSpaceVertex,
        Assets.Load<string>("shaders/red.frag").Value));

    public static readonly Material EditorBackground = new Material(new Shader(
        Assets.Load<string>("shaders/worldspace-vertex-object-pos.vert").Value,
        Assets.Load<string>("shaders/editor-background.frag").Value));

    public static readonly Material TrainMovingBackground = new Material(new Shader(
        Assets.Load<string>("shaders/worldspace-vertex-object-pos.vert").Value,
        Assets.Load<string>("shaders/train-background.frag").Value));

    public static readonly Material LoadingSpinningHead = FlipbookMaterialCreator.LoadMaterialFor(Textures.UserInterface.LoadingFlipbook.Value, 8, 1, 0, Colors.White, true, 0);

    public static readonly Material AlphaClipDraw = new Material(new Shader(DrawingMaterialCreator.DefaultShader.VertexShader, AlphaClipDrawMat.FragmentShader));

    public static readonly Material BlackToWhiteOutline= new Material(
        new Shader(DrawingMaterialCreator.DefaultShader.VertexShader, Assets.LoadNoCache<string>("shaders/black-to-white-outline.frag")));
}

internal static class AlphaClipDrawMat
{
    public const string FragmentShader =
    @$"#version 330 core

in vec2 uv;
in vec4 vertexColor;

out vec4 color;

uniform vec2 {DrawingMaterialCreator.ScaleUniform};
uniform float {DrawingMaterialCreator.RoundednessUniform} = 0;

uniform float {DrawingMaterialCreator.OutlineWidthUniform} = 0;
uniform vec4 {DrawingMaterialCreator.OutlineColourUniform} = vec4(0,0,0,0);

uniform sampler2D {DrawingMaterialCreator.MainTexUniform};
uniform vec4 {DrawingMaterialCreator.TintUniform} = vec4(1, 1, 1, 1);
uniform int {DrawingMaterialCreator.ImageModeUniform} = 0;

// The MIT License
// Copyright (C) 2015 Inigo Quilez
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the ""Software""), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions: The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software. THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// https://www.youtube.com/c/InigoQuilez
// https://iquilezles.org

float sdRoundBox(in vec2 p, in vec2 b, in float r ) 
{{
    vec2 q = abs(p - 0.5) * 2.0 * b - b + r;
    return min(max(q.x,q.y),0.0) + length(max(q,0.0)) - r;
}}

vec2 getSlicedUv(in vec2 p)
{{
    vec2 size = textureSize({DrawingMaterialCreator.MainTexUniform}, 0);
    vec2 ratio = {DrawingMaterialCreator.ScaleUniform} / size;
    vec2 edge = min(vec2(0.5), 0.5 / ratio);
    vec2 min = p * ratio;
    vec2 max = (p - 1.0) * ratio + 1.0;
    vec2 maxEdge = 1.0 - edge;
    return vec2(
        p.x > edge.x ? (p.x < maxEdge.x ? 0.5 : max.x) : min.x,
        p.y > edge.y ? (p.y < maxEdge.y ? 0.5 : max.y) : min.y
    );
}}

vec2 getTiledUv(in vec2 p)
{{
    vec2 size = textureSize({DrawingMaterialCreator.MainTexUniform}, 0);
    vec2 ratio = {DrawingMaterialCreator.ScaleUniform} / size;
    return p * ratio;
}}

void main()
{{
    float clampedRoundness = clamp({DrawingMaterialCreator.RoundednessUniform}, 0, min({DrawingMaterialCreator.ScaleUniform}.x, {DrawingMaterialCreator.ScaleUniform}.y) / 2);
    float d = sdRoundBox(uv, {DrawingMaterialCreator.ScaleUniform}, clampedRoundness * 2.0);

    float corner = step(0.0, -d);
    float outline = {DrawingMaterialCreator.OutlineWidthUniform} < 1 ? 0 : (ceil(d + fwidth(uv.x) + 0.05) < -{DrawingMaterialCreator.OutlineWidthUniform} ? 0 : 1);

    vec2 wUv = ({DrawingMaterialCreator.ImageModeUniform} == 0) ? uv : ({DrawingMaterialCreator.ImageModeUniform} == 1 ? getSlicedUv(uv) : getTiledUv(uv));

    vec4 baseColor = texture({DrawingMaterialCreator.MainTexUniform}, wUv) * {DrawingMaterialCreator.TintUniform};
    color = vertexColor * mix(baseColor, {DrawingMaterialCreator.OutlineColourUniform}, outline * {DrawingMaterialCreator.OutlineColourUniform}.a);
    color.a *= corner;
    if (color.a < 0.5)
        discard;
}}";
}
