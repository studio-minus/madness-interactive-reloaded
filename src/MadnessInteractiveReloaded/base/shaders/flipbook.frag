#version 330 core

in vec2 uv;
in vec4 vertexColor;

out vec4 color;

uniform sampler2D mainTex;

uniform float rows = 1;
uniform float columns = 1;
uniform float progress = 0;
uniform vec4 tint = vec4(1,1,1,1);
uniform float frameCount = 0;
uniform int alphaClip = 1;

void main()
{
    float width = max(columns, 1);
    float height = max(rows, 1);

    float frame = floor((frameCount == 0 ? (width * height) : frameCount) * progress);

    float currentColumn = mod(frame, width);
    float currentRow = floor(frame / width);

    vec2 newUv = uv;
    newUv.x = (uv.x + currentColumn) / width;
    newUv.y = (uv.y + (height - 1.0 - currentRow)) / height;
    
    color = vertexColor * texture(mainTex,  newUv);
    color.a = mix(color.a, color.a > 0.5? 1 : 0, alphaClip);
    color = color * tint;
}