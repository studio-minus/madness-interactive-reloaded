#version 330 core

#define FOREGROUND_WITHOUT_DECALS 1   // 255
#define BACKGROUND_WITHOUT_DECALS 0.7 // 178
#define BACKGROUND_AND_DECALS 0.3     // 77

in vec2 uv;
in vec4 vertexColor;

out vec4 color;

uniform sampler2D mainTex;
uniform vec4 tint = vec4(1, 1, 1, 1);

void main()
{
    color = vertexColor * texture(mainTex, uv) * tint;
    if (color.a < FOREGROUND_WITHOUT_DECALS)
        discard;
    color.a = 1;
}