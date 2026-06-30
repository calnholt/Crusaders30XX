/* ──────────────────────────────────────────────────────────────
   FALLING LEAVES — Image (post / present) — ShaderToy

   Final pass over the blurred scene: bloom + radial light-streak blur,
   gamma, ACES tonemap, saturation boost, colour grade, vignette.

   iChannel0 = Buffer B (FallingLeavesBufferB.glsl).

   ShaderToy multi-buffer wiring:
     Buffer A : FallingLeavesBufferA.glsl. iChannel0 = background.
     Buffer B : FallingLeavesBufferB.glsl. iChannel0 = Buffer A.
     Image    : this file. iChannel0 = Buffer B.

   Paste into the Image tab, bind iChannel0 = Buffer B, Alt+Enter.
   ────────────────────────────────────────────────────────────── */

// ═══════════════════════════════════════════════════════════════
// TUNABLES
// ═══════════════════════════════════════════════════════════════

// ── Bloom (soft glow off bright leaves) ─────────────────────────
#define BLOOM_N 8  // bloom blur taps per axis. higher = smoother glow, heavier

const float BLOOM_RADIUS = 0.04;  // glow spread in uv. small = tight halo, big = hazy wash
const float BLOOM_LOD    = 3.0;   // mip level sampled (cheap big blur). higher = softer/wider
const float BLOOM_POWER  = 2.0;   // glow contrast. higher = only the brightest leaves bloom

// ── Radial blur (light streaks toward a corner) ─────────────────
#define RADIAL_N 64.0  // streak samples. lower = banded, higher = silky/heavy

const float RADIAL_AMOUNT = 0.2;  // streak strength mixed back in. 0 = off
const float RADIAL_LENGTH = 0.3;  // how far the streaks reach across the frame
const vec2  RADIAL_TARGET = vec2(1.0);  // point the streaks pull toward (uv; (1,1)=top-right)

// ── Grade ───────────────────────────────────────────────────────
const float SATURATION  = -0.6;  // mix toward grey. negative = MORE saturated (counter-intuitive), positive = washed
const vec3  COLOR_GRADE = vec3(0.84, 1.0, 0.9);  // per-channel gamma. <1 lifts that channel. tilts the green/teal mood
const float VIGNETTE    = 0.1;   // edge darkening power. 0 = none, higher = heavier dark frame

// ═══════════════════════════════════════════════════════════════
// HELPERS
// ═══════════════════════════════════════════════════════════════

// tonemap (ACES filmic)
vec3 ACES(vec3 x)
{
    float a = 2.51, b = 0.03, c = 2.43, d = 0.59, e = 0.14;
    return (x * (a * x + b)) / (x * (c * x + d) + e);
}

// bloom: wide low-mip box blur
vec4 bloom(sampler2D sam, vec2 p)
{
    vec4 col = vec4(0.0);
    for (int i = -BLOOM_N; i <= BLOOM_N; i++)
    for (int j = -BLOOM_N; j <= BLOOM_N; j++) {
        vec2 off = vec2(float(i), float(j)) / float(BLOOM_N);
        col += textureLod(sam, p + off * BLOOM_RADIUS, BLOOM_LOD);
    }
    return col / col.a;
}

// radial blur along direction v
vec4 radialBlur(sampler2D sam, vec2 p, vec2 v)
{
    vec4 col = vec4(0.0);
    for (float i = 0.0; i < 1.0; i += 1.0 / RADIAL_N) {
        col += pow(1.0 - sqrt(i), 3.0) * texture(sam, p + RADIAL_LENGTH * i * v);
    }
    return col / col.a;
}

// ═══════════════════════════════════════════════════════════════
// MAIN IMAGE
// ═══════════════════════════════════════════════════════════════

void mainImage(out vec4 fragColor, in vec2 fragCoord)
{
    vec2 p = fragCoord / iResolution.xy;
    vec3 col = texture(iChannel0, p).rgb;  // base
    vec3 bloomC = pow(bloom(iChannel0, p).rgb, vec3(BLOOM_POWER));
    vec2 v = normalize(RADIAL_TARGET - p);  // streak direction
    vec3 blur = RADIAL_AMOUNT * radialBlur(iChannel0, p, v).rgb;
    col += blur;
    col = pow(col, vec3(0.4545));  // gamma to linear-ish before bloom add
    col += bloomC;
    col = ACES(col);
    col = mix(col, dot(col, vec3(1.0)) / vec3(3.0), SATURATION);  // saturation
    col = pow(col, COLOR_GRADE);  // colour grade
    // vignette
    col *= pow(64.0 * p.x * p.y * (1.0 - p.x) * (1.0 - p.y), VIGNETTE);
    fragColor = vec4(col, 1.0);
}
