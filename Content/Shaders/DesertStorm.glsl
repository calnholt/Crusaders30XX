/* ──────────────────────────────────────────────────────────────
   Desert Sandstorm v2 — ShaderToy

   Natural, organic sand-dust clouds with heavy grain.
   Domain-warped FBM — no discrete shapes.

   Paste into shadertoy.com/new. Set iChannel0 to any scene texture
   (or leave empty for standalone). All tunables at the top.
   ────────────────────────────────────────────────────────────── */

// ═══════════════════════════════════════════════════════════════
// TUNABLES
// ═══════════════════════════════════════════════════════════════

// ── Cloud Structure ───────────────────────────────────────────
#define FBM_OCTAVES 5          // noise detail (3 = soft blobs, 7 = fine wisps)
const float BASE_SCALE       = 1.5;   // cloud mass size (lower = larger)
const float LACUNARITY       = 2.0;   // frequency multiplier per octave
const float PERSISTENCE      = 0.50;  // amplitude decay per octave
const float WARP_STRENGTH    = 3.5;   // organic shape distortion (0 = plain FBM)
const float DENSITY_REMAP_LO = 0.20;  // smoothstep low — pulls shadows darker
const float DENSITY_REMAP_HI = 0.80;  // smoothstep high — pushes highlights brighter

// ── Motion (positive values = clouds drift rightward) ─────────
const float DRIFT_SPEED    = 0.025;  // primary horizontal drift (UV/sec)
const float DRIFT_VERTICAL = 0.006;  // subtle vertical drift
const float WARP_DRIFT_A   = 0.018;  // warp field A speed
const float WARP_DRIFT_B   = 0.012;  // warp field B speed
const float MORPH_SPEED    = 0.008;  // shape evolution rate

// ── Colors ────────────────────────────────────────────────────
const vec3 COL_SHADOW    = vec3(0.55, 0.47, 0.37);  // deepest shadow
const vec3 COL_MID       = vec3(0.70, 0.62, 0.51);  // mid-tone
const vec3 COL_HIGHLIGHT = vec3(0.82, 0.75, 0.64);  // highlight
const vec3 COL_BRIGHT    = vec3(0.89, 0.82, 0.71);  // brightest dust
const float VERT_GRADIENT = 0.08;  // top-bright / bottom-dark bias

// ── Scene Compositing (iChannel0) ─────────────────────────────
const float DUST_BASE       = 0.55;                    // minimum dust coverage / ambient haze (0–1)
const float DUST_DENSITY    = 0.45;                    // additional obscuration from cloud pattern
const vec3  SCENE_TINT      = vec3(0.90, 0.82, 0.68);  // warm haze tint over scene
const float SCENE_TINT_STR  = 0.40;                      // tint strength (0 = no tint, 1 = full)

// ── Grain ─────────────────────────────────────────────────────
const float GRAIN_INTENSITY = 0.10;  // overall grain strength
const float GRAIN_FINENESS  = 1.0;   // spatial scale (1.0 = per-pixel)

// ── Vignette ──────────────────────────────────────────────────
const float VIGNETTE_AMOUNT = 0.20;  // edge/corner darkening

// ═══════════════════════════════════════════════════════════════
// NOISE CORE
// ═══════════════════════════════════════════════════════════════

float hash21(vec2 p)
{
    vec3 p3 = fract(vec3(p.xyx) * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return fract((p3.x + p3.y) * p3.z);
}

float vnoise(vec2 p)
{
    vec2 i = floor(p);
    vec2 f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    return mix(
        mix(hash21(i), hash21(i + vec2(1.0, 0.0)), f.x),
        mix(hash21(i + vec2(0.0, 1.0)), hash21(i + vec2(1.0, 1.0)), f.x),
        f.y
    );
}

const mat2 FBM_ROT = mat2(0.80, 0.60, -0.60, 0.80);

float fbm(vec2 p)
{
    float v = 0.0;
    float a = 0.5;
    for (int i = 0; i < FBM_OCTAVES; i++)
    {
        v += a * vnoise(p);
        p = FBM_ROT * p * LACUNARITY;
        a *= PERSISTENCE;
    }
    return v;
}

// ═══════════════════════════════════════════════════════════════
// DOMAIN-WARPED CLOUD DENSITY
// ═══════════════════════════════════════════════════════════════

float cloudDensity(vec2 p, float t)
{
    // First warp field — large-scale organic distortion
    vec2 q;
    q.x = fbm(p + vec2(0.00, 0.00) + t * vec2(-WARP_DRIFT_A, MORPH_SPEED));
    q.y = fbm(p + vec2(5.20, 1.30) + t * vec2(-WARP_DRIFT_A, -MORPH_SPEED * 0.7));

    // Second warp field — feeds off first for richer shapes
    vec2 r;
    r.x = fbm(p + WARP_STRENGTH * q + vec2(1.70, 9.20) + t * vec2(-WARP_DRIFT_B, MORPH_SPEED * 0.5));
    r.y = fbm(p + WARP_STRENGTH * q + vec2(8.30, 2.80) + t * vec2(-WARP_DRIFT_B, -MORPH_SPEED * 0.3));

    // Final density with primary drift
    return fbm(p + WARP_STRENGTH * r + t * vec2(-DRIFT_SPEED, DRIFT_VERTICAL));
}

// ═══════════════════════════════════════════════════════════════
// MAIN IMAGE
// ═══════════════════════════════════════════════════════════════

void mainImage(out vec4 fragColor, in vec2 fragCoord)
{
    vec2 uv = fragCoord / iResolution.xy;
    float aspect = iResolution.x / iResolution.y;
    vec2 p = vec2(uv.x * aspect, uv.y) * BASE_SCALE;

    // Scene from iChannel0 (black if unset — shader still works standalone)
    vec3 scene = texture(iChannel0, uv).rgb;
    scene = mix(scene, scene * SCENE_TINT, SCENE_TINT_STR);

    // Cloud density → 0..1
    float d = cloudDensity(p, iTime);
    d = smoothstep(DENSITY_REMAP_LO, DENSITY_REMAP_HI, d);

    // 4-stop sand color gradient
    vec3 sandCol = COL_SHADOW;
    sandCol = mix(sandCol, COL_MID, smoothstep(0.00, 0.35, d));
    sandCol = mix(sandCol, COL_HIGHLIGHT, smoothstep(0.35, 0.65, d));
    sandCol = mix(sandCol, COL_BRIGHT, smoothstep(0.65, 1.00, d));

    // Dust fog — dense areas fully obscure, thin areas let scene through
    float fog = clamp(DUST_BASE + d * DUST_DENSITY, 0.0, 1.0);
    vec3 col = mix(scene, sandCol, fog);

    // Vertical gradient — slightly brighter at top
    col *= 1.0 + VERT_GRADIENT * (uv.y - 0.5);

    // Film grain — temporal, per-pixel
    float grain = hash21(floor(fragCoord * GRAIN_FINENESS) + fract(iTime * 7.13) * 300.0);
    grain = (grain - 0.5) * GRAIN_INTENSITY;
    col += grain;

    // Vignette
    vec2 vc = uv - 0.5;
    float vig = 1.0 - VIGNETTE_AMOUNT * dot(vc, vc) * 2.5;
    col *= vig;

    fragColor = vec4(clamp(col, 0.0, 1.0), 1.0);
}
