#version 330 core

#define HOLE_MAX_COUNT 32
#define THRESHOLD 0.5

in vec2 uv;
in vec4 vertexColor;
in vec3 object;

out vec4 color;

uniform sampler2D mainTex;
uniform vec4 tint = vec4(1, 1, 1, 1);

uniform float seed;
uniform float scale = 1;
uniform int holesCount;
uniform vec3 holes[HOLE_MAX_COUNT];

// Simplex 2D noise
    // https://gist.github.com/patriciogonzalezvivo/670c22f3966e662d2f83
    vec3 permute(vec3 x) { return mod(((x*34.0)+1.0)*x, 289.0); }
    float snoise(vec2 v){
      const vec4 C = vec4(0.211324865405187, 0.366025403784439,
               -0.577350269189626, 0.024390243902439);
      vec2 i  = floor(v + dot(v, C.yy) );
      vec2 x0 = v -   i + dot(i, C.xx);
      vec2 i1;
      i1 = (x0.x > x0.y) ? vec2(1.0, 0.0) : vec2(0.0, 1.0);
      vec4 x12 = x0.xyxy + C.xxzz;
      x12.xy -= i1;
      i = mod(i, 289.0);
      vec3 p = permute( permute( i.y + vec3(0.0, i1.y, 1.0 ))
      + i.x + vec3(0.0, i1.x, 1.0 ));
      vec3 m = max(0.5 - vec3(dot(x0,x0), dot(x12.xy,x12.xy),
        dot(x12.zw,x12.zw)), 0.0);
      m = m*m ;
      m = m*m ;
      vec3 x = 2.0 * fract(p * C.www) - 1.0;
      vec3 h = abs(x) - 0.5;
      vec3 ox = floor(x + 0.5);
      vec3 a0 = x - ox;
      m *= 1.79284291400159 - 0.85373472095314 * ( a0*a0 + h*h );
      vec3 g;
      g.x  = a0.x  * x0.x  + h.x  * x0.y;
      g.yz = a0.yz * x12.xz + h.yz * x12.yw;
      return 130.0 * dot(m, g);
    }
// end of patriciogonzalezvivo/670c22f3966e662d2f83

void main()
{
    ivec2 texSize = textureSize(mainTex, 0).xy;
    vec2 aspectRatio = vec2(texSize.x / float(texSize.y), 1);

    vec2 obj = object.xy * aspectRatio;
    float scl = scale / aspectRatio.x;

    float accDepth = 1000;

    for (int i = 0; i < holesCount; i++)
    {
        vec3 hole = holes[i];
        vec2 holePos = (hole.xy + 0.1 * vec2(snoise(obj.xy * 1.5 - seed), snoise(obj.xy * 1.5 + seed))) * aspectRatio;
        vec2 delta = holePos - obj;
        float depth = hole.z;
        float magn = length(delta);

        float polar = atan(delta.y, delta.x);
        float star = 
            snoise(vec2(polar * 2, holePos.x * 23.45)) 
            + snoise(vec2(polar * 8, holePos.y * 23.45)) * 0.32
            + snoise(vec2(polar * 17, holePos.y * 23.45)) * 0.1;

        star *= 0.5 * depth / max(1, magn * 4);

        float d = magn * scl - depth + THRESHOLD + star * 0.25;

        accDepth = min(accDepth, d);
     }
          
    const float SMOOTH_EDGE = 0.0025;
    float OUTLINE_WIDTH = 0.012 * scale;

    float outline = smoothstep(0.5, 0.5 + SMOOTH_EDGE, accDepth - OUTLINE_WIDTH);

    color = vertexColor * texture(mainTex, uv) * tint;

    color.rgb = mix(vec3(0), color.rgb, outline); // outline color
    color.a = mix(smoothstep(0.1, 0.1+SMOOTH_EDGE, color.a), color.a, outline); // outline alpha
    color.a *= smoothstep(0.5, 0.5 + SMOOTH_EDGE, accDepth); // hole alpha

}