#version 330 core

#define FOREGROUND_WITHOUT_DECALS 1   // 255
#define BACKGROUND_WITHOUT_DECALS 0.7 // 178
#define BACKGROUND_AND_DECALS 0.3     // 77

in vec2 uv;
in vec4 vertexColor;
in float frame;
in vec2 maskPos;

out vec4 color;

uniform sampler2D mainTex;
uniform sampler2D decalMask;

uniform float rows = 1;
uniform float columns = 1;

float getColumn(float i)
{
    return floor(mod(i, max(columns, 1)));
}

float getRow(float i)
{
    return floor(i / max(columns, 1));
}

bool isInAlphaMask(vec4 mask)
{
    return mask.r > 0.5;
}

void main()
{
    if (!isInAlphaMask(texture(decalMask, maskPos)))
        discard;

    float width = max(columns, 1);
    float height = max(rows, 1);

    vec2 offset = vec2(getColumn(frame), getRow(frame));
    vec2 newUv = vec2(
        (uv.x + offset.x) / width, 
        (uv.y + offset.y) / height);
    color = vertexColor * texture(mainTex,  newUv);

    if (color.a < 0.5)
        discard;
}