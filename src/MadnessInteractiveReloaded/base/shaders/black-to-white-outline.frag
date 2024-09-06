#version 330 core

in vec2 uv;
in vec4 vertexColor;

out vec4 color;

uniform sampler2D mainTex;
uniform vec4 tint = vec4(1, 1, 1, 1);

void main()
{
    vec4 col = vertexColor * texture(mainTex, uv);

    float grey = (col.r + col.g + col.b) * 0.333;

    col.a *= smoothstep(0.2, 0, grey) * tint.a;
    col.rgb = tint.rgb;

    color = col;
}