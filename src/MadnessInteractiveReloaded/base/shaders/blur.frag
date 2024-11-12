#version 330 core

in vec2 uv;
out vec4 color;

uniform sampler2D mainTex;
uniform vec4 tint = vec4(1, 1, 1, 1);
uniform int kernelSize = 4;

void main()
{
    ivec2 texSize = textureSize(mainTex, 0).xy;
    vec2 offset = 1.0 / texSize;

    vec4 sum = vec4(0.0);
    float weight = 0;

    // just box blur for now
    for (int x = -kernelSize; x <= kernelSize; x++) {
        for (int y = -kernelSize; y <= kernelSize; y++) {
            vec2 samplePos = uv + vec2(float(x) * offset.x, float(y) * offset.y);
            float w = 1;
            sum += texture(mainTex, samplePos) * w;
            weight += w;
        }
    }

    sum /= weight;
    color = sum * tint;
}