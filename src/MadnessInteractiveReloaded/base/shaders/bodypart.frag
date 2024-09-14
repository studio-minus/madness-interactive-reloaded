#version 330

#define FLOAT_MAX 3.402823466e+38
#define FLOAT_MIN 1.175494351e-38
#define HOLE_MAX_COUNT 32
#define THRESHOLD 1
#define DISCARD_THRESHOLD .9
#define PI 3.141592653589793

in vec2 uv;
in vec4 vertexColor;
in vec3 object;

out vec4 color;

uniform vec4 tint = vec4(1,1,1,1);
uniform sampler2D mainTex;
uniform sampler2D goreTex;
uniform sampler2D fleshTex;

uniform sampler2D slashTex;

uniform float scale = 1;

uniform vec3 outerBloodColour = vec3(0.93, 0.08, 0);
uniform vec3 innerBloodColour = vec3(0.60, 0.05, 0);

uniform int holesCount;
uniform vec3 holes[HOLE_MAX_COUNT];

uniform int innerCutoutHolesCount;
uniform vec3 innerCutoutHoles[HOLE_MAX_COUNT];

uniform int slashesCount;
uniform vec3 slashes[HOLE_MAX_COUNT];

// https://github.com/MaxBittker/glsl-voronoi-noise/blob/master/2d.glsl
    const mat2 myt = mat2(.12121212, .13131313, -.13131313, .12121212);
    const vec2 mys = vec2(1e4, 1e6);
    vec2 rhash(vec2 uv) {
      uv *= myt;
      uv *= mys;
      return fract(fract(uv / mys) * uv);
    }
    vec3 hash(vec3 p) {
      return fract(sin(vec3(dot(p, vec3(1.0, 57.0, 113.0)),
                            dot(p, vec3(57.0, 113.0, 1.0)),
                            dot(p, vec3(113.0, 1.0, 57.0)))) *
                   43758.5453);
    }
    float voronoi2d(const in vec2 point) {
      vec2 p = floor(point);
      vec2 f = fract(point);
      float res = 0.0;
      for (int j = -1; j <= 1; j++) {
        for (int i = -1; i <= 1; i++) {
          vec2 b = vec2(i, j);
          vec2 r = vec2(b) - f + rhash(p + b);
          res += 1. / pow(dot(r, r), 8.);
        }
      }
      return pow(1. / res, 0.0625);
    }
// end of MaxBittker/glsl-voronoi-noise

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

float saw(float x){
    return abs(mod(x / PI - 0.5, 2) - 1) * 2 - 1;
}

void main()
{
    ivec2 texSize = textureSize(mainTex, 0).xy;
    vec2 aspectRatio = vec2(texSize.x / float(texSize.y), 1);

    vec2 correctedObjUv = object.xy * aspectRatio;
    float correctScale = scale;

    float minHoleDistance = FLOAT_MAX;
    float minInnerCutoutHoleDistance = FLOAT_MAX;
    float smallNoise = (snoise(correctedObjUv * 16 * correctScale) * 2.0 - 1.0) * 0.02;
    float largeNoise = (voronoi2d(correctedObjUv * 8 * correctScale)) * 0.15 - 0.1;
    float largeSimplexNoise = (snoise(correctedObjUv * 7 * correctScale) * 2.0 - 1.0) * 0.015;

    vec4 skin = texture(mainTex, uv);
    vec4 gore = texture(goreTex, uv);
    vec4 flesh = texture(fleshTex, uv);

    float slashDepth = 0;

    for (int i = 0; i < slashesCount; i++)
    {
        vec3 slash = slashes[i];
        vec2 pos = slash.xy * aspectRatio;
        float th = slash.z; // rads
        float cosTh = cos(th);
        float sinTh = sin(th);
        mat2 rotation = mat2(
          cosTh, -sinTh,
          sinTh, cosTh
        );

        vec2 slashUv = ((correctedObjUv * correctScale * 0.8 - pos) * rotation) + vec2(0.5, 0.5);

        vec4 sampled = texture(slashTex, slashUv);
        slashDepth += sampled.a * 1.1;
    }

    for (int i = 0; i < innerCutoutHolesCount; i++)
    {
        vec3 innerHole = innerCutoutHoles[i];
        vec2 holePos = innerHole.xy * aspectRatio;
        float depth = innerHole.z + largeSimplexNoise * 0.15;

        float d = length(holePos - correctedObjUv) * correctScale - depth + THRESHOLD;

        if (d < minInnerCutoutHoleDistance)
            minInnerCutoutHoleDistance = d;
    }

    for (int i = 0; i < holesCount; i++)
    {
        vec3 hole = holes[i];
        vec2 holePos = hole.xy * aspectRatio;
        float depth = hole.z;

        float largeStar = saw(atan(holePos.y - correctedObjUv.y, holePos.x - correctedObjUv.x) * 9) * 0.02 * depth;
        float d = length(holePos - correctedObjUv) * correctScale - depth + largeNoise + THRESHOLD + largeStar;

        if (d < DISCARD_THRESHOLD)
        {
            if (minInnerCutoutHoleDistance > 0)
            {
                color.rgb = (minInnerCutoutHoleDistance < mix(smallNoise, 0.01, 0.8)) ? outerBloodColour : flesh.rgb * outerBloodColour;
                color.a = skin.a;
                return;
            }
            else discard;
        }

        float smallStar = saw(atan(holePos.y - correctedObjUv.y, holePos.x - correctedObjUv.x) * 16) * 0.02;
        d -= smallStar + smallNoise;

        if (d < minHoleDistance)
            minHoleDistance = d;
     }

    if (skin.a < 0.9 && skin.r > max(skin.g, skin.b))
    {
        skin.rgb = mix(clamp(skin.r * 1.2, 0, 1) * innerBloodColour, vec3(1,1,1), (skin.g + skin.b) / 2.5);
        skin.a = 1;
    }

    vec4 c = skin;

    float verySmallNoise = (1 - voronoi2d(correctedObjUv * 32.0)) * 0.35;
    verySmallNoise *= verySmallNoise;

     if (minHoleDistance < THRESHOLD - 0.09)
         c = vec4(innerBloodColour, skin.a);
     else if (minHoleDistance < THRESHOLD + verySmallNoise)
         c = vec4(outerBloodColour, skin.a);

    // slash damage
     c.rgb = mix(c.rgb, outerBloodColour, smoothstep(0.23, 0.25, min(slashDepth, 1)));
     c.rgb = mix(c.rgb, flesh.rgb * innerBloodColour, smoothstep(0.88, .95, min(slashDepth, 1)));

     color = vertexColor * c;
     color *= tint;
     color.rgb *= color.a;

}