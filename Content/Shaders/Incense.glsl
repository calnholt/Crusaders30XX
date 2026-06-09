/* ──────────────────────────────────────────────────────────────
   INCENSE SMOKE — ShaderToy

   Whole-screen volumetric haze, the way a Catholic church feels
   the moment after the thurible has swung: thick blue-grey smoke
   rising and churning in even ambient gloom, with dust motes
   drifting and glinting through it. No directional light source
   — flat diffuse murk.

   Paste into shadertoy.com/new.
   iChannel0: leave empty — previews standalone (it's a full-screen
   procedural atmosphere, no texture).
   Layout: smoke rises from the bottom, fills the frame.
   Tunables up top.
   ────────────────────────────────────────────────────────────── */

// ═══════════════════════════════════════════════════════════════
// TUNABLES
// ═══════════════════════════════════════════════════════════════

// ── Smoke structure ───────────────────────────────────────────
const float SMOKE_SCALE     = 3.2;   // density field zoom (low = big lazy clouds, high = fine wisps)
const float WARP_STRENGTH   = 2.6;   // domain-warp churn (0 = plain noise, 4+ = violently swirly)
const float SMOKE_LO        = 0.30;  // density floor — raise to thin/clear the haze, lower to thicken
const float SMOKE_HI        = 0.85;  // density ceiling — lower it to make smoke more solid/opaque
const float DEPTH_PARALLAX  = 0.55;  // 2nd smoke layer offset scale (adds volumetric depth; 0 = flat)

// ── Motion ────────────────────────────────────────────────────
const float RISE_SPEED  = 0.055;  // upward drift of the smoke (units/sec; sign flips direction)
const float CHURN_SPEED = 0.040;  // how fast the swirl pattern itself evolves
const float DRIFT_X     = 0.010;  // slow lateral sideways breeze (sign sets left/right)

// ── Colors ────────────────────────────────────────────────────
const vec3 COL_GLOOM  = vec3(0.030, 0.034, 0.045);  // darkest corners — cold stone shadow
const vec3 COL_SMOKE  = vec3(0.34, 0.36, 0.42);     // mid smoke body — cool blue-grey ash
const vec3 COL_GLINT  = vec3(1.00, 0.82, 0.55);     // mote sparkle tint — faint warm ash glow

// ── Dust motes (specks catching the light) ────────────────────
const float MOTE_AMOUNT      = 0.1;    // density of glinting specks (0 = none, 1 = busy)
const float MOTE_SCALE       = 190.0;  // mote grid fineness (high = smaller, more numerous specks)
const float MOTE_DRIFT_MIN   = 0.008;  // slowest motes' float speed (the lazy, heavy specks)
const float MOTE_DRIFT_MAX   = 0.045;  // fastest motes' float speed (the light, racing specks)
const float MOTE_FLASH_MIN   = 0.6;    // slowest twinkle rate (specks that pulse lazily)
const float MOTE_FLASH_MAX   = 4.5;    // fastest twinkle rate (specks that flicker rapidly)
const float MOTE_FLASH_DEPTH = 0.9;    // flash contrast (0 = steady glow, 1 = blinks fully off)

// ── Finishing ─────────────────────────────────────────────────
const float VIGNETTE_AMT = 1.05;  // corner darkening (0 = none, 1.5 = heavy cathedral gloom)
const float GRAIN_AMT    = 0.035; // film grain / sensor noise (0 = clean, 0.08 = grainy)
const float EXPOSURE     = 1.15;  // overall brightness before tonemap

// ── Detail (compile-time; needs constant loop bounds) ─────────
#define FBM_OCTAVES 6  // smoke detail (4 = soft, 7 = intricate wisps)

// ═══════════════════════════════════════════════════════════════
// NOISE / HELPERS
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
    f = f * f * (3.0 - 2.0 * f);  // smootherstep interpolation
    return mix(
        mix(hash21(i), hash21(i + vec2(1.0, 0.0)), f.x),
        mix(hash21(i + vec2(0.0, 1.0)), hash21(i + vec2(1.0, 1.0)), f.x),
        f.y
    );
}

const float LACUNARITY  = 2.0;  // frequency multiplier per octave
const float PERSISTENCE = 0.5;  // amplitude decay per octave
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

// Domain-warped fbm: feed fbm into the coords of another fbm for
// natural, non-repetitive smoke. `t` slowly evolves the swirl.
float smokeField(vec2 p, float t)
{
    vec2 q = vec2(
        fbm(p + vec2(0.0, 0.0) + 0.10 * t),
        fbm(p + vec2(5.2, 1.3) - 0.10 * t)
    );
    return fbm(p + WARP_STRENGTH * q);
}

// ═══════════════════════════════════════════════════════════════
// MAIN IMAGE
// ═══════════════════════════════════════════════════════════════

void mainImage(out vec4 fragColor, in vec2 fragCoord)
{
    vec2 uv = fragCoord / iResolution.xy;
    float aspect = iResolution.x / iResolution.y;
    vec2 p = vec2(uv.x * aspect, uv.y);  // aspect-corrected so smoke stays round
    float t = iTime;

    // ── 1. SMOKE DENSITY ─────────────────────────────────────────
    // Two layers at different depths drift up at slightly different
    // rates → a sense of volume rather than a flat wallpaper.
    vec2 rise = vec2(DRIFT_X * t, -RISE_SPEED * t);  // smoke moves up (y down in sample space)
    float near = smokeField(p * SMOKE_SCALE + rise, CHURN_SPEED * t * 10.0);
    float far  = smokeField(
        p * SMOKE_SCALE * (1.0 + DEPTH_PARALLAX) + rise * 0.6 + vec2(13.0, 7.0),
        CHURN_SPEED * t * 7.0
    );
    float density = mix(near, far, 0.45);
    density = smoothstep(SMOKE_LO, SMOKE_HI, density);

    // ── 2. COLOR ─────────────────────────────────────────────────
    // Even ambient haze: gloom in the thin/empty areas, cool ash where
    // the smoke piles up. No directional light source.
    vec3 col = mix(COL_GLOOM, COL_SMOKE, density);

    // ── 3. DUST MOTES ────────────────────────────────────────────
    // Fine drifting specks; faintly catch the ambient light, brighter
    // where the smoke is thicker. Each column gets its own float speed
    // (hashed) so specks rise at a spread of rates, not in lockstep.
    vec2 mcoord = p * MOTE_SCALE;
    float mcol = floor(mcoord.x);  // stable column id
    float mspeed = mix(MOTE_DRIFT_MIN, MOTE_DRIFT_MAX, hash21(vec2(mcol, 17.0)));  // per-column drift rate
    mcoord.y -= t * mspeed * MOTE_SCALE;  // scroll this column up
    vec2 mid = floor(mcoord);  // this speck's cell id
    float mcell = hash21(mid);
    float mote = smoothstep(0.985, 1.0, mcell);  // sparse bright specks

    // Per-speck twinkle: independent rate AND phase from the cell id, so
    // every mote flashes on its own clock — never in unison.
    float frate = mix(MOTE_FLASH_MIN, MOTE_FLASH_MAX, hash21(mid + 4.0));
    float fphase = hash21(mid + 9.0) * 6.2831;
    float flash = 1.0 - MOTE_FLASH_DEPTH * (0.5 + 0.5 * sin(t * frate + fphase));
    mote *= flash;

    col += COL_GLINT * mote * MOTE_AMOUNT * (0.25 + density);

    // ── 4. FINISHING ─────────────────────────────────────────────
    vec2 vv = uv - 0.5;
    float vig = 1.0 - VIGNETTE_AMT * dot(vv, vv) * 2.5;
    col *= clamp(vig, 0.0, 1.0);

    col *= EXPOSURE;
    col = col / (1.0 + col);  // Reinhard tonemap — keeps the bloom from clipping

    float g = hash21(floor(fragCoord) + fract(t * 7.13) * 300.0) - 0.5;
    col += g * GRAIN_AMT;

    fragColor = vec4(clamp(col, 0.0, 1.0), 1.0);
}
