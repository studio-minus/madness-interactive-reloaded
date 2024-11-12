#version 330
#define ANIMATION_DURATION 0.4

in vec2 uv;
in vec4 vertexColor;

out vec4 color;

uniform float time;
uniform float timeSinceChange;
uniform float isOpen;
uniform sampler2D mainTex;
uniform vec4 tint = vec4(1, 1, 1, 1);

float easeInOutQuad(float x) {
    float z = -2.0 * x + 2.0;
    return x < 0.5 ? 2.0 * x * x : 1.0 - (z * z) / 2.0;
}

void main()
{
    float progress = easeInOutQuad(clamp(time - timeSinceChange, 0.0, ANIMATION_DURATION) / ANIMATION_DURATION);

    vec2 tUv = uv;
    tUv.y -= (isOpen > 0.5 ? progress : 1.0 - progress) * 0.95;
    color = vertexColor * texture(mainTex, tUv) * tint;
}