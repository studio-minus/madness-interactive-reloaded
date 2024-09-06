#version 330 core
#define ANIMATION_DURATION 0.6

smooth in vec2 uv;
in vec4 vertexColor;
in vec3 object;
in vec4 world;

out vec4 color;

uniform float time;
uniform sampler2D mainTex;
uniform vec4 tint = vec4(1, 1, 1, 1);

void main()
{
    vec2 tUv = vec2(world.x / 1024.0, uv.y);
    tUv.x = mod(tUv.x + time, 1);
    color = vertexColor * texture(mainTex, tUv) * tint;
}