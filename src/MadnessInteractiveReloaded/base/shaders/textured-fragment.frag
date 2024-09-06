#version 330 core

in vec2 uv;
in vec4 vertexColor;

out vec4 color;

uniform sampler2D mainTex;
uniform vec4 tint = vec4(1, 1, 1, 1);

void main()
{
    color = vertexColor * texture(mainTex, uv) * tint;
}