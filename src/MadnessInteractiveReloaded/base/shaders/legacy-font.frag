#version 330 core

in vec2 uv;
in vec4 vertexColor;

out vec4 color;

uniform sampler2D mainTex;
uniform vec4 tint;

void main()
{
    vec4 tex = texture(mainTex, uv);
    color = vertexColor * tint;
    color.a = min(tex.r, min(tex.g, min(tex.b, min(tex.a, color.a))));
}