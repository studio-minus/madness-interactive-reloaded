#version 330 core

in vec2 uv;
in vec4 vertexColor;

out vec4 color;

uniform sampler2D mainTex;

uniform float rows = 1.0;
uniform float columns = 1.0;
uniform float progress = 0.0;
uniform vec4 tint = vec4(1.0, 1.0, 1.0, 1.0);
uniform float frameCount = 0.0;
uniform int alphaClip = 1;

void main()
{
    float width = max(columns, 1.0);
    float height = max(rows, 1.0);

    float totalFrames = (frameCount == 0.0) ? (width * height) : frameCount;
    float frame = floor(totalFrames * progress);

    float currentColumn = mod(frame, width);
    float currentRow = floor(frame / width);

    vec2 frameSize = vec2(1.0 / width, 1.0 / height);
    vec2 frameOffset = vec2(currentColumn, height - 1.0 - currentRow);
    vec2 newUv = uv * frameSize + frameOffset * frameSize;
    
    color = vertexColor * texture(mainTex, newUv);
    color.a = mix(color.a, color.a > 0.5 ? 1.0 : 0.0, float(alphaClip));
    color *= tint;
}