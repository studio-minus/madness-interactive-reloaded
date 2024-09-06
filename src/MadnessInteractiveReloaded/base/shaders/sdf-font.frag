#version 330 core

in vec2 uv;
in vec4 vertexColor;

out vec4 color;

uniform sampler2D mainTex;
uniform float thickness;
uniform float softness;

void main()
{
    vec4 tex = texture(mainTex, uv);
    float t = clamp(1-thickness, 0, 1);

    float delta = fwidth(tex.a) / 2 + softness;
    tex.a = smoothstep(t - delta, t + delta , tex.a);
    color = vec4(vertexColor.rgb, vertexColor.a * tex.a);
}