#version 330 core

in vec2 uv;
in vec4 vertexColor;

out vec4 color;

uniform sampler2D mainTex;
uniform vec4 tint = vec4(1, 1, 1, 1);

void main()
{
    vec2 texSize = vec2(textureSize(mainTex, 0));
    vec2 texelSize = 1.0 / texSize;

    vec2 dx = dFdx(uv);
    vec2 dy = dFdy(uv);

    float dudx = length(dx) * texSize.x;
    float dvdy = length(dy) * texSize.y;

    float filterRadius = max(dudx, dvdy);
    vec4 texColor;

    if(filterRadius <= 1.0)
        texColor = texture(mainTex, uv);
    else
    {
        int radius = int(clamp(filterRadius, 1.0, 3.0));
        vec4 accum = vec4(0.0);
        float totalWeight = 0.0;
        for(int i = -radius; i <= radius; ++i)
            for(int j = -radius; j <= radius; ++j)
            {
                vec2 offset = vec2(i, j) * texelSize;
                const float weight = 1.0; 
                accum += texture(mainTex, uv + offset) * weight;
                totalWeight += weight;
            }

        texColor = accum / totalWeight;
    }

    vec4 col = vertexColor * texColor;
    float grey = (col.r + col.g + col.b) * 0.333;
    col.a *= smoothstep(0.2, 0.0, grey) * tint.a;
    col.rgb = tint.rgb;
    color = col;
}