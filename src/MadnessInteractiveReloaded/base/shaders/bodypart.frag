#version 330 core

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

uniform float seed;
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

// helper function to convert RGB to HSV
vec3 rgb2hsv(vec3 rgb) {
    vec4 K = vec4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    vec4 p = mix(vec4(rgb.bg, K.wz), vec4(rgb.gb, K.xy), step(rgb.b, rgb.g));
    vec4 q = mix(vec4(p.xyw, rgb.r), vec4(rgb.r, p.yzx), step(p.x, rgb.r));

    float d = q.x - min(q.w, q.y);
    float e = 1.0e-10;
    return vec3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

// helper function to convert HSV to RGB
vec3 hsv2rgb(vec3 hsv) {
    vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    vec3 p = abs(fract(hsv.xxx + K.xyz) * 6.0 - K.www);
    return hsv.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), hsv.y);
}

vec3 setHue(in vec3 o, in vec3 dst) {
    vec3 oHSV = rgb2hsv(o);
    vec3 dstHSV = rgb2hsv(dst);
    oHSV.x = dstHSV.x; // x is the h
    vec3 result = hsv2rgb(oHSV);
    return result;
}

void main()
{
    ivec2 texSize = textureSize(mainTex, 0).xy;
    vec2 aspectRatio = vec2(texSize.x / float(texSize.y), 1);

    vec2 obj = object.xy * aspectRatio;
    float scl = scale / aspectRatio.x;

    float minHoleDistance = 100000;
    float minInnerCutoutHoleDistance = 100000;

    float smallNoise = (snoise(obj * 16 * scl) * 2.0 - 1.0) * 0.02;
    float largeNoise = (voronoi2d(obj * 8 * scl)) * 0.15 - 0.1;
    float largeSimplexNoise = (snoise(obj * 7 * scl) * 2.0 - 1.0) * 0.015;

    vec4 skin = texture(mainTex, uv);
    vec4 gore = texture(goreTex, uv);
    vec4 flesh = texture(fleshTex, uv);

    float bloodStain = 0;
    float slashDepth = 0;

    flesh.rgb *= innerBloodColour;
    gore.rgb = setHue(gore.rgb, innerBloodColour); 
    // the gore layer is red by default (because gray and multiply is ugly), so we hue shift to the blood colour (it looks nicer)
    gore.rgb *= pow(((innerBloodColour.r + innerBloodColour.g + innerBloodColour.b) * 0.333), 0.2); 
    // we then multiply by the average to make sure the value is also transferred (we adjust the curve to make sure the default isnt too dark)
    gore.a *= skin.a;
    flesh.a *= skin.a;

    // set skin "blood" mask
    if (skin.a < 0.9 && skin.r > max(skin.g, skin.b))
    {
        skin.rgb = mix(clamp(skin.r * 1.2, 0, 1) * innerBloodColour, vec3(1,1,1), (skin.g + skin.b) / 2.5);
        skin.a = 1;
    }

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

        vec2 slashUv = ((obj * scl * 0.7 - pos) * rotation) + vec2(0.5, 0.5);

        vec4 sampled = texture(slashTex, slashUv);
        slashDepth += sampled.a * 1.1;
    }

    for (int i = 0; i < innerCutoutHolesCount; i++)
    {
        vec3 innerHole = innerCutoutHoles[i];
        vec2 holePos = innerHole.xy * aspectRatio;
        float depth = innerHole.z - largeNoise + largeSimplexNoise - smallNoise * 0.1;

        float d = length(holePos - obj) * scl - depth + THRESHOLD;

        minInnerCutoutHoleDistance = min(d, minInnerCutoutHoleDistance);
    }

    for (int i = 0; i < holesCount; i++)
    {
        vec3 hole = holes[i];
        vec2 holePos = hole.xy * aspectRatio;
        vec2 delta = holePos - obj;
        float depth = hole.z + smallNoise * 0.3 + largeSimplexNoise * 0.5 + largeNoise * 0.5;
        float magn = length(delta);
        float d = magn * scl - depth + THRESHOLD;

        minHoleDistance = min(d, minHoleDistance);

        float polar = atan(delta.y, delta.x);
        float star = (snoise(vec2(polar * 4, 0))) * 0.5 * depth / max(1, magn * 4) * 3;

        bloodStain = max(bloodStain, star);
     }

     const float SMOOTH_EDGE = 0.0025;

     // set skin blood rim
     float verySmallNoise = (1 - voronoi2d(obj * 38.0 + 0.1 * snoise(obj * 5 * scl + vec2(-7.423, 30.523)))) * 0.35;
     verySmallNoise *= verySmallNoise;
     skin.rgb = mix(outerBloodColour, skin.rgb, 
        smoothstep(THRESHOLD, THRESHOLD + SMOOTH_EDGE, minHoleDistance - verySmallNoise - bloodStain));

     vec4 finalCol = skin;

     // cut out back layer
     flesh.a *= smoothstep(0, SMOOTH_EDGE, minInnerCutoutHoleDistance + .06);

     // cut out gore
     if (seed < 0.95)
     { 
        float gc = minHoleDistance + largeNoise * 0.2 + mix(0.02, 0.1, seed);
        gore.rgb = mix(outerBloodColour, gore.rgb, smoothstep(THRESHOLD, THRESHOLD + SMOOTH_EDGE, gc - 0.2 * verySmallNoise));
        gore.a *= smoothstep(THRESHOLD, THRESHOLD + SMOOTH_EDGE, gc);
     }

     // cut out skin layer
     vec4 goreBehind = mix(flesh, gore, gore.a);
     finalCol.rgb = mix(finalCol.rgb, outerBloodColour, smoothstep(0.23, 0.23 + + SMOOTH_EDGE, min(slashDepth, 1)));
     float d = min(1 - smoothstep(0.88, .95, min(slashDepth, 1)), smoothstep(THRESHOLD, THRESHOLD + SMOOTH_EDGE, minHoleDistance));
     finalCol = mix(goreBehind, finalCol, d);

     finalCol *= vertexColor;
     finalCol *= tint;

     color = finalCol;
}