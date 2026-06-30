/* ──────────────────────────────────────────────────────────────
   FALLING LEAVES — Buffer A (main render) — ShaderToy

   Drifting cloud of leaves rendered as thin SDF shapes wrapped onto
   spheres, layered front-to-back with depth + parallax.

   This buffer does the raw scene; FallingLeavesBufferB.glsl blurs it
   (bokeh DoF), FallingLeavesImage.glsl adds bloom / radial blur /
   tonemap / vignette.

   Recreated from the original (random-coloured leaves) with ONE change:
   every leaf is now GREEN, and the green is fully tunable. Each leaf
   still gets its own shade via the same random roll the original used,
   so the layout / motion is identical — only the palette changed.

   ShaderToy multi-buffer wiring:
     Buffer A : this file. iChannel0 = your BACKGROUND image/video
                (shows through the gaps between leaves). Leave it empty
                and it previews on a procedural sky gradient.
     Buffer B : FallingLeavesBufferB.glsl. iChannel0 = Buffer A.
     Image    : FallingLeavesImage.glsl. iChannel0 = Buffer B.

   Paste into the Buffer A tab, set the channels above, Alt+Enter.

   EVERY leaf-colour knob lives in the TUNABLES block.
   ────────────────────────────────────────────────────────────── */

#define time iTime

// ═══════════════════════════════════════════════════════════════
// TUNABLES
// ═══════════════════════════════════════════════════════════════
// Comments name the ENDPOINTS — which way to push, what you'll see.

// ── Leaf colour (all green, per-leaf shade) ─────────────────────
// Each leaf rolls a random 0..1 and mixes between DARK and LIGHT, so the
// canopy spans this whole range. Collapse DARK == LIGHT for one flat green.
const vec3 LEAF_GREEN_DARK  = vec3(0.04, 0.22, 0.03);  // darkest leaf (deep shade green). raise for a brighter floor
const vec3 LEAF_GREEN_LIGHT = vec3(0.45, 0.80, 0.18);  // lightest leaf (sun-lit lime). push g up = more vivid, push r/b up = paler
const float LEAF_HUE_JITTER = 0.10;  // per-leaf warm/cool wander. 0 = uniform hue, 0.25 = some olive/yellow strays. KEEP SMALL to stay green
const float LEAF_BRIGHTNESS = 1.0;   // overall canopy gain. <1 darker leaves, >1 punchier

// ── Population & motion ─────────────────────────────────────────
#define LEAF_COUNT 80  // leaves drawn back-to-front. 40 = sparse/cheap, 120 = dense/heavier GPU
const float SCROLL_X = -0.06;  // horizontal drift of the field per second (sign = direction)
const float SCROLL_Y =  0.05;  // vertical drift (fall) per second
const vec2  SPREAD   = vec2(15.0, 13.0);  // how far leaves spread across x / y (bigger = more scattered)
const vec2  SPIN_RATE = vec2(0.2, 0.3);   // base tumble speed of the whole field (x-spin, y-spin)

// ── Leaf size (per-leaf range) ──────────────────────────────────
const float LEAF_RA_MIN = 0.7;  // smallest leaf radius (distant/tiny)
const float LEAF_RA_VAR = 0.6;  // added on top of MIN by random roll => max = MIN+VAR

// ── Background (iChannel0, seen through the gaps) ───────────────
const float BG_BRIGHTNESS = 1.0;  // gain on the bound background. <1 dims it, >1 lifts it
const vec3  BG_SKY_LO     = vec3(0.02, 0.05, 0.08);  // fallback sky at bottom (no texture bound)
const vec3  BG_SKY_HI     = vec3(0.10, 0.16, 0.20);  // fallback sky at top

// ── Fog / light glare ───────────────────────────────────────────
const vec3  LIGHT_DIR   = vec3(0.57735026919);  // direction the glare comes from (normalized)
const float GLARE_POWER = 16.0;  // glare tightness. LOW = broad haze, HIGH = tight hotspot
const float FOG_AMOUNT  = 0.8;   // volumetric fog/scatter strength (0 = clear black gaps)
const float FOG_FLOOR   = 0.004; // base fog even away from the light

// ═══════════════════════════════════════════════════════════════
// HELPERS
// ═══════════════════════════════════════════════════════════════

// rotation matrix
mat2 rot(float a)
{
    float s = sin(a), c = cos(a);
    return mat2(c, -s, s, c);
}

// hash vec2 -> float
float hash21(vec2 p)
{
    p = fract(p * vec2(452.127, 932.618));
    p += dot(p, p + 123.23);
    return fract(p.x * p.y);
}

// value noise
float noise(vec2 p)
{
    vec2 q = floor(p);
    vec2 f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    return mix(mix(hash21(q + vec2(0, 0)), hash21(q + vec2(1, 0)), f.x),
               mix(hash21(q + vec2(0, 1)), hash21(q + vec2(1, 1)), f.x), f.y);
}

// fractal noise
float fbm(vec2 p)
{
    float f = 0.0;
    f += 0.5 * noise(p);
    f += 0.25 * noise(p * 2.0);
    f += 0.125 * noise(p * 4.0);
    f += 0.0625 * noise(p * 8.0);
    return f;
}

// leaf SDF (the silhouette: blade + two side-lobes + stem)
float sdLeave(vec2 p)
{
    float d = length(p * vec2(1.0, 0.9) - vec2(0.0, 0.02)) - 0.2 + 0.9 * abs(p.x);
    p.x = abs(p.x);
    vec2 q = p;
    q -= vec2(0.0, -0.2 + 32.0 * pow(p.x, 5.0));
    q *= rot(3.141592 * 0.3 + 0.1 * fbm(16.0 * p));
    q -= vec2(0.0, 0.17);
    d = min(d, length(q) - 0.17 + 2.0 * abs(q.x));
    q = p;
    q -= vec2(-0.07, -0.1 + 6.0 * pow(p.x, 3.0));
    q *= rot(3.141592 * 0.7 + 0.1 * fbm(24.0 * p));
    q -= vec2(0.0, 0.13);
    d = min(d, length(q) - 0.13 + 2.5 * abs(q.x));
    // stem
    q = p;
    float h = clamp(q.y, -0.377, 0.0);
    q -= vec2(0.0, h);
    d = min(d + 0.2 * pow(fbm(24.0 * p), 3.0), length(q) + 0.02 * h);
    return d;
}

// sphere intersection — thanks iq: https://iquilezles.org/articles/intersectors/
vec2 sphIntersect(vec3 ro, vec3 rd, vec3 ce, float ra)
{
    vec3 oc = ro - ce;
    float b = dot(oc, rd);
    float c = dot(oc, oc) - ra * ra;
    float h = b * b - c;
    if (h < 0.0) return vec2(-1.0);
    h = sqrt(h);
    return vec2(-b - h, -b + h);
}

// 3d cartesian -> 2d polar
vec2 polar(vec3 p)
{
    return vec2(atan(p.x, p.z), p.y);
}

// leaf wrapped on a sphere -> alpha coverage for this ray
float leaveSphere(vec3 col, vec3 ro, vec3 rd, vec3 ce, vec2 an, float ra)
{
    vec2 t = sphIntersect(ro, rd, ce, ra);
    if (t.y < 0.0) return 0.0;  // no intersection
    vec3 oc = ro - ce;
    vec3 p = oc + rd * t.x;  // front point
    vec3 q = oc + rd * t.y;  // back point
    // rotate both hit points into leaf-space
    p.xz *= rot(an.x);
    q.xz *= rot(an.x);
    p.yz *= rot(an.y);
    q.yz *= rot(an.y);
    float di = ra * 2.0;  // diameter
    vec2 r = vec2(ra, 1.0) / di;  // ratio
    vec2 pf = polar(p) * r;  // polar front point
    vec2 pb = polar(q) * r;  // polar back point
    float lf = step(sdLeave(pf), 0.0);  // front face inside leaf?
    float lb = step(sdLeave(pb), 0.0);  // back face inside leaf?
    return lf * (1.0 - lb) + lb;  // coverage
}

// per-leaf RNG (each call advances the seed once)
float hash1(inout float n) { return fract(sin(n += 1.0) * 2348.3241); }
vec2  hash2(inout float n) { return fract(sin(n += 1.0) * vec2(2348.3241, 4591.5392)); }
vec3  hash3(inout float n) { return fract(sin(n += 1.0) * vec3(2348.3241, 4591.5392, 3412.4231)); }

// ═══════════════════════════════════════════════════════════════
// RENDER
// ═══════════════════════════════════════════════════════════════

vec3 render(vec3 ro, vec3 rd, vec3 bg)
{
    vec3 col = bg;  // background shows through wherever no leaf covers
    vec2 an = time * SPIN_RATE;  // field angle

    for (float i = 0.0; i < 1.0; i += 1.0 / float(LEAF_COUNT)) {
        float n = i + 1.0;  // random seed for this leaf

        // xy home position, drifting over time
        vec2 p = (0.5 - fract(hash2(n) + vec2(SCROLL_X, SCROLL_Y) * time)) * SPREAD;
        // nudge the tumble per leaf
        an += hash1(n) - 0.5;

        // ── COLOUR: all green, per-leaf shade (same RNG advance as original) ──
        vec3 rnd = hash3(n);  // this leaf's roll
        vec3 mat = mix(LEAF_GREEN_DARK, LEAF_GREEN_LIGHT, rnd.x);  // green shade
        mat.r += (rnd.y - 0.5) * LEAF_HUE_JITTER;  // warm/cool wander...
        mat.g += (rnd.z - 0.5) * LEAF_HUE_JITTER * 0.5;  // ...kept gentle so it stays green
        mat = clamp(mat * LEAF_BRIGHTNESS, 0.0, 1.0);

        vec3 ce = vec3(p, 8.0 * (1.0 - i));  // depth: first leaves far, last near
        float ra = LEAF_RA_MIN + LEAF_RA_VAR * hash1(n);  // per-leaf radius
        float alpha = leaveSphere(col, ro, rd, ce, 3.141592 * an, ra);
        col = mix(col, mat, alpha);
    }

    // light glare + fog noise (volumetric scatter feel)
    float glare = pow(clamp(dot(rd, LIGHT_DIR), 0.0, 1.0), GLARE_POWER);
    col += FOG_AMOUNT * fbm(12.0 * rd.xy + 0.1 * time * vec2(1.0, 3.0)) * (FOG_FLOOR + glare);
    return col;
}

// ═══════════════════════════════════════════════════════════════
// MAIN IMAGE
// ═══════════════════════════════════════════════════════════════

void mainImage(out vec4 fragColor, in vec2 fragCoord)
{
    // pixel coords centered at origin
    vec2 p = (fragCoord - 0.5 * iResolution.xy) / iResolution.y;
    vec3 ro = vec3(0.0, 0.0, -3.0);  // ray origin
    vec3 rd = normalize(vec3(p, 1.5));  // ray direction

    // background from iChannel0, with a procedural sky fallback so it
    // previews without a texture bound
    vec2 uv = fragCoord / iResolution.xy;
    vec3 tex = texture(iChannel0, uv).rgb;
    float hasTex = step(0.01, dot(tex, vec3(1.0)));  // ~0 when no texture bound
    vec3 bg = mix(mix(BG_SKY_LO, BG_SKY_HI, uv.y), tex, hasTex) * BG_BRIGHTNESS;

    vec3 col = render(ro, rd, bg);
    fragColor = vec4(col * 1.2, 1.0);
}
