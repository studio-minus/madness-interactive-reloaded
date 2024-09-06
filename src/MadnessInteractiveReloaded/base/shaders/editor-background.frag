#version 330 core

in vec2 uv;
in vec4 vertexColor;
in vec3 object;

out vec4 color;

uniform vec2 windowSize;
uniform vec3 cameraTransform;

float getGrid(float value, float size)
{
    return mod(value * size, 1.0) < 0.01 ? 1 : 0;
}

void main()
{
    float g = 0;

    vec2 b = vec2(object.x, object.y);
    b += vec2(-0.5, 0.5);
    b *= cameraTransform.z;
    b -= vec2(-0.5, 0.5);

    b += cameraTransform.xy / windowSize;
    b *= windowSize / 1080.0;

    for (int i = 1; i < 5; i+=2)
    {
        float x = getGrid(b.x, 5 * i);
        float y = getGrid(-b.y, 5 * i);

        g = max(g, max(x, y) / (i * i));
    }

    g /= max(cameraTransform.z * 3, 2);

    color = vertexColor * vec4((1 - uv.y) * 0.1 ,0 ,0 ,1);
    color = mix(color, vec4(1,1,1,1), g);
}