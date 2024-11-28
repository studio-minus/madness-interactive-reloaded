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
uniform float doorType = 0;

float easeInOutQuad(float x) {
    float z = -2.0 * x + 2.0;
    return x < 0.5 ? 2.0 * x * x : 1.0 - (z * z) / 2.0;
}

vec2 verticalDoor(in vec2 uv, in float progress)
{
    vec2 tUv = uv;
    tUv.y -= progress * 0.95;
    return tUv;
}

vec2 horizontalDoor(vec2 uv, float progress)
{
    float side = sign(uv.x - 0.5);
    vec2 tUv = uv - vec2(side * progress * 0.5, 0);
    return mix(tUv, vec2(-1.0), step(0.0, -(side * (tUv.x - 0.5))));
}

float clampToZeroOrOne(float value) {
    return step(0.0, value) * step(value, 1.0);
}

void main()
{
    float progress = easeInOutQuad(clamp(time - timeSinceChange, 0.0, ANIMATION_DURATION) / ANIMATION_DURATION);
    progress = (isOpen > 0.5 ? progress : 1.0 - progress);
	vec2 tUv = mix(verticalDoor(uv, progress), horizontalDoor(uv, progress), doorType);
    color = texture(mainTex, tUv) * tint * clampToZeroOrOne(tUv.x) * clampToZeroOrOne(tUv.y);
}