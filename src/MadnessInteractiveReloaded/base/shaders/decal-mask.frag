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
    vec4 tex = texture(mainTex, uv);
    vec3 cc = vec3(0,0,1);
    
    if (tex.a < 0.01)
        cc = vec3(0,0,0);
    else if (tex.a <= BACKGROUND_AND_DECALS)
        cc = vec3(1,0,0);
    else if (tex.a <= BACKGROUND_WITHOUT_DECALS)
        cc = vec3(0,1,0);

    color = vertexColor * vec4(cc, 1) * tint;
}