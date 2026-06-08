/* ──────────────────────────────────────────────────────────────
   Red Dripping Liquid — ShaderToy

   Thick red liquid dripping in randomized streaks, leaving fading
   trails on a near-black surface.

   Paste into shadertoy.com/new. Leave all channels unbound.
   All tunables at the top — tweak the look without reading GLSL.
   ────────────────────────────────────────────────────────────── */

// ═══════════════════════════════════════════════════════════════
// TUNABLES
// ═══════════════════════════════════════════════════════════════

// ── Drip Population ──────────────────────────────────────────
#define NUM_DRIPS  20   // per layer (5 = sparse, 25 = drenched)
#define NUM_LAYERS 1    // depth layers, each dimmer (1 = flat, 3+ = depth; fewer = faster)

// ── Motion ───────────────────────────────────────────────────
const float SPEED_MIN = 0.06;  // slowest fall (lower = thick syrup)
const float SPEED_MAX = 0.15;  // fastest fall (higher = watery)
const float REST_MIN  = 1.5;   // shortest pause between drip cycles (seconds)
const float REST_MAX  = 5.0;   // longest pause (higher = calmer)

// ── Trail Fade ───────────────────────────────────────────────
const float FADE_POW       = 1.8;   // how trail fades toward origin (1 = linear, 3 = sharp cutoff near top)
const float OFFSCREEN_FADE = 0.35;  // how far below screen the head travels before trail fully fades

// ── Shape ────────────────────────────────────────────────────
const float WIDTH_MIN     = 0.003;   // thinnest streak (screen fraction)
const float WIDTH_MAX     = 0.016;   // thickest streak
const float TAPER_AT_TOP  = 0.65;    // how thin trail is at origin (0 = razor, 1 = no taper)
const float TIP_ROUND     = 1.0;     // bottom roundness (0 = flat cutoff, 0.5 = semicircle, 1.0 = full capsule)
const float WOBBLE_AMP    = 0.0025;  // side-to-side wobble amount (0 = straight, 0.008 = wavy)
const float WOBBLE_FREQ   = 14.0;    // wobble spatial frequency (4 = gentle, 20 = jagged)
const float THICKNESS_VAR = 0.35;    // noise-driven width variation (0 = uniform, 0.8 = blobby lumps)

// ── Colors ───────────────────────────────────────────────────
const vec3 COL_BG   = vec3(0.05, 0.003, 0.003);  // near-black, slight red warmth
const vec3 COL_DRIP = vec3(0.70, 0.02, 0.02);    // flat drip red — single color, no shading

// ── Atmosphere ───────────────────────────────────────────────
const float VIGNETTE = 0.0;  // corner darkening (0 = flat, 1 = heavy)

// ═══════════════════════════════════════════════════════════════
// NOISE / HELPERS
// ═══════════════════════════════════════════════════════════════

float hash21(vec2 p)
{
    vec3 p3 = fract(vec3(p.xyx) * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return fract((p3.x + p3.y) * p3.z);
}

float hashS(float s)
{
    return fract(sin(s * 12.9898 + 78.233) * 43758.5453);
}

float vnoise(vec2 p)
{
    vec2 i = floor(p);
    vec2 f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    return mix(
        mix(hash21(i), hash21(i + vec2(1.0, 0.0)), f.x),
        mix(hash21(i + vec2(0.0, 1.0)), hash21(i + vec2(1.0, 1.0)), f.x),
        f.y);
}

// ═══════════════════════════════════════════════════════════════
// MAIN IMAGE
// ═══════════════════════════════════════════════════════════════

void mainImage(out vec4 fragColor, in vec2 fragCoord)
{
    vec2 uv = fragCoord / iResolution.xy;
    float px = 1.0 / iResolution.y;  // 1-pixel feather width for AA
    vec3 col = COL_BG;

    for (int layer = 0; layer < NUM_LAYERS; layer++)
    {
        float lf = float(layer);
        float lSeed = lf * 173.7;
        float lScale = 1.0 - lf * 0.10;  // farther layers thinner streaks

        for (int i = 0; i < NUM_DRIPS; i++)
        {
            float fi = float(i);
            float seed = fi + lSeed;

            // ── per-drip random properties ──
            float dripX  = hashS(seed + 0.13);
            float speed  = mix(SPEED_MIN, SPEED_MAX, hashS(seed + 1.37));
            float width  = mix(WIDTH_MIN, WIDTH_MAX, hashS(seed + 2.71)) * lScale;
            float phase  = hashS(seed + 3.91);
            float startY = mix(1.00, 1.08, hashS(seed + 4.23));
            float rest   = mix(REST_MIN, REST_MAX, hashS(seed + 5.67));

            // ── drip cycle: head falls from startY, rests, repeats ──
            float travelDist = startY + OFFSCREEN_FADE + 0.1;
            float travelTime = travelDist / speed;
            float period = travelTime + rest;
            float cycleT = mod(iTime + phase * period, period);
            float headY = startY - cycleT * speed;

            // ── wobble: organic lateral drift ──
            float wob = (vnoise(vec2(uv.y * WOBBLE_FREQ + seed * 3.1, seed)) - 0.5) * 2.0 * WOBBLE_AMP;
            float dx = abs(uv.x - dripX - wob);

            // ── trail: between head and origin ──
            float above = step(headY, uv.y);
            float below = step(uv.y, startY);
            float inTrail = above * below;
            float trailLen = max(startY - headY, 0.001);
            float trailPos = clamp((uv.y - headY) / trailLen, 0.0, 1.0);

            // taper + noise lumps for thick-liquid feel
            float nThk = 1.0 - THICKNESS_VAR + THICKNESS_VAR * vnoise(vec2(uv.y * 25.0 + seed * 7.0, seed * 0.3));
            float trailW = width * mix(1.0, TAPER_AT_TOP, sqrt(trailPos)) * nThk;

            // rounded bottom: elliptical cap below head.
            // vertical radius = trailW * TIP_ROUND (0 = flat, 1 = semicircle).
            float belowHead = max(headY - uv.y, 0.0);
            float vRad = trailW * TIP_ROUND + 1e-5;  // avoid /0

            // ellipse distance in normalized space (1.0 = edge)
            float ed = length(vec2(dx / trailW, belowHead / vRad));
            float feather = px / trailW;  // AA in normalized units
            float inTip = smoothstep(1.0 + feather, 1.0 - feather, ed);

            // streak body above head + rounded tip below (AA via smoothstep)
            float bodyMask = smoothstep(trailW + px, trailW - px, dx) * inTrail;
            float trailMask = max(bodyMask, inTip);

            // fade toward origin (old liquid dries and thins)
            float trailFade = pow(max(1.0 - trailPos, 0.0), FADE_POW);

            // fade out once head goes offscreen
            float offFade = smoothstep(-OFFSCREEN_FADE, 0.0, headY);
            trailMask *= trailFade * offFade;

            // ── flat color composite — no shading, no glow ──
            col = mix(col, COL_DRIP, trailMask);
        }
    }

    // vignette: darken corners
    vec2 vc = uv - 0.5;
    col *= 1.0 - VIGNETTE * dot(vc, vc) * 2.5;

    fragColor = vec4(clamp(col, 0.0, 1.0), 1.0);
}
