/*
   Darkened Card - ShaderToy

   A card is swallowed by black, swirling smoke. The card face is never
   visible: the interior is fully replaced by dense animated darkness, while
   wispy tendrils spill past the rounded card boundary.

   Paste into shadertoy.com/new. Set iChannel0 to a background or scene image.
   The card uses a procedural placeholder underneath the smoke because the
   effect is meant to hide card identity completely. All tunables and
   implementation constants are kept at the top for fast iteration.
*/

// ========================================================================
// TUNABLES
// ========================================================================

// Preview card region, in 0..1 screen space with y=0 at the bottom.
const float CARD_LEFT = 0.30;
const float CARD_RIGHT = 0.70;
const float CARD_BOTTOM = 0.20;
const float CARD_TOP = 0.90;
const float CARD_RADIUS = 0.035;
const float CARD_EDGE_FEATHER = 0.003;

// Smoke coverage.
const float INTERIOR_ALPHA = 1.00;       // 1 = card art is completely hidden.
const float OUTSIDE_MAX_ALPHA = 0.88;    // Max opacity of smoke outside the card.
const float OUTSIDE_REACH = 0.145;       // How far smoke can escape from the card edge.
const float OUTSIDE_SOFTNESS = 0.120;    // Feather for escaped smoke cutoff.
const float OUTSIDE_WISP_POWER = 1.65;   // Higher = thinner outside tendrils.
const float EDGE_DENSITY_BOOST = 0.32;   // Extra smoke density around the boundary.
const float EDGE_BOOST_WIDTH = 0.040;

// Smoke structure.
#define FBM_OCTAVES 5
const vec2 SMOKE_SCALE = vec2(3.35, 4.80);
const vec2 SMOKE_DRIFT = vec2(-0.080, 0.155);
const float SMOKE_DENSITY_LOW = 0.34;
const float SMOKE_DENSITY_HIGH = 0.78;
const float SMOKE_CONTRAST = 1.30;
const float FINE_SMOKE_SCALE = 2.60;
const float FINE_SMOKE_MIX = 0.34;

// Swirl motion.
const float SWIRL_SPEED = 0.62;
const float SWIRL_TWIST = 5.40;
const float SWIRL_STRENGTH = 0.58;
const float SWIRL_NOISE_SCALE = 2.10;
const float SWIRL_NOISE_AMOUNT = 1.15;
const vec2 SWIRL_NOISE_DRIFT = vec2(0.060, -0.035);

// Domain warp.
const float WARP_STRENGTH_A = 1.85;
const float WARP_STRENGTH_B = 2.70;
const vec2 WARP_OFFSET_A = vec2(2.17, 11.43);
const vec2 WARP_OFFSET_B = vec2(7.91, 3.31);
const vec2 WARP_OFFSET_C = vec2(14.37, 19.71);
const vec2 WARP_OFFSET_D = vec2(23.11, 5.83);
const vec2 WARP_DRIFT_A = vec2(0.090, -0.040);
const vec2 WARP_DRIFT_B = vec2(-0.055, 0.070);
const vec2 WARP_DRIFT_C = vec2(0.035, 0.090);
const vec2 WARP_DRIFT_D = vec2(-0.075, -0.030);

// Wispy ribbon mask.
const float WISP_ARMS = 3.0;
const float WISP_RADIAL_FREQ = 7.0;
const float WISP_SPEED = 1.25;
const float WISP_NOISE_BEND = 4.60;
const float WISP_NOISE_SCALE = 1.75;
const float WISP_RIBBON_POWER = 2.20;
const float WISP_RIBBON_WEIGHT = 1.58;
const float WISP_NOISE_WEIGHT = 0.56;
const float WISP_THRESHOLD = 0.56;
const float WISP_SOFTNESS = 0.58;

// Color and background compositing.
const vec3 BG_COLOR = vec3(0.022, 0.024, 0.030);
const float BG_TEXTURE_STRENGTH = 1.00;
const float BG_FALLBACK_LIFT = 0.18;
const vec3 SMOKE_BLACK = vec3(0.210, 0.200, 0.260);
const vec3 SMOKE_CHARCOAL = vec3(0.018, 0.020, 0.026);
const vec3 SMOKE_WISP = vec3(0.070, 0.074, 0.086);
const vec3 EDGE_INK = vec3(0.000, 0.000, 0.000);
const float WISP_HIGHLIGHT = 0.94;
const float VIGNETTE_AMOUNT = 0.0;
const float GRAIN_INTENSITY = 0.00;
const float GRAIN_SPEED = 17.0;

// Texture detection and procedural placeholder card.
const float TEXTURE_DETECT_THRESHOLD = 0.010;
const float PLACEHOLDER_GRID = 12.0;
const float PLACEHOLDER_LINE_LOW = 0.450;
const float PLACEHOLDER_LINE_HIGH = 0.500;
const float PLACEHOLDER_LINE_MIX = 0.22;
const vec3 PLACEHOLDER_BOTTOM = vec3(0.20, 0.34, 0.55);
const vec3 PLACEHOLDER_TOP = vec3(0.86, 0.80, 0.55);

// Hash, noise, and fixed math constants.
const float HASH_SCALE = 0.1031;
const float HASH_OFFSET = 33.33;
const vec2 NOISE_STEP_X = vec2(1.0, 0.0);
const vec2 NOISE_STEP_Y = vec2(0.0, 1.0);
const vec2 NOISE_STEP_XY = vec2(1.0, 1.0);
const mat2 FBM_ROT = mat2(0.80, 0.60, -0.60, 0.80);
const float FBM_LACUNARITY = 2.02;
const float FBM_PERSISTENCE = 0.50;
const float TWO_PI = 6.28318530718;

// ========================================================================
// HASH / NOISE
// ========================================================================

float hash21(vec2 p)
{
    vec3 p3 = fract(vec3(p.xyx) * HASH_SCALE);
    p3 += dot(p3, p3.yzx + HASH_OFFSET);
    return fract((p3.x + p3.y) * p3.z);
}

float vnoise(vec2 p)
{
    vec2 i = floor(p);
    vec2 f = fract(p);
    f = f * f * (3.0 - 2.0 * f);

    float a = hash21(i);
    float b = hash21(i + NOISE_STEP_X);
    float c = hash21(i + NOISE_STEP_Y);
    float d = hash21(i + NOISE_STEP_XY);

    return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
}

float fbm(vec2 p)
{
    float value = 0.0;
    float amp = 0.5;

    for (int i = 0; i < FBM_OCTAVES; i++)
    {
        value += amp * vnoise(p);
        p = FBM_ROT * p * FBM_LACUNARITY;
        amp *= FBM_PERSISTENCE;
    }

    return value;
}

// ========================================================================
// CARD GEOMETRY / PREVIEW SOURCE
// ========================================================================

float roundedBoxSDF(vec2 p, vec2 halfSize, float radius)
{
    vec2 q = abs(p) - halfSize + radius;
    return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - radius;
}

vec2 cardSize()
{
    return vec2(CARD_RIGHT - CARD_LEFT, CARD_TOP - CARD_BOTTOM);
}

vec2 cardCenter()
{
    return vec2((CARD_LEFT + CARD_RIGHT) * 0.5, (CARD_BOTTOM + CARD_TOP) * 0.5);
}

float cardSDF(vec2 uv)
{
    vec2 halfSize = cardSize() * 0.5;
    return roundedBoxSDF(uv - cardCenter(), halfSize, CARD_RADIUS);
}

vec2 cardUV(vec2 uv)
{
    return clamp((uv - vec2(CARD_LEFT, CARD_BOTTOM)) / cardSize(), 0.0, 1.0);
}

vec2 cardLocal(vec2 uv)
{
    return (uv - cardCenter()) / cardSize();
}

vec3 sampleCard(vec2 uv)
{
    vec2 cuv = cardUV(uv);
    vec2 grid = abs(fract(cuv * PLACEHOLDER_GRID) - 0.5);
    float line = smoothstep(PLACEHOLDER_LINE_LOW, PLACEHOLDER_LINE_HIGH, max(grid.x, grid.y));
    vec3 proc = mix(PLACEHOLDER_BOTTOM, PLACEHOLDER_TOP, cuv.y);
    return mix(proc, vec3(1.0), line * PLACEHOLDER_LINE_MIX);
}

vec3 sampleBackground(vec2 uv)
{
    vec3 tex = texture(iChannel0, uv).rgb;
    float hasTex = step(TEXTURE_DETECT_THRESHOLD, dot(tex, vec3(1.0)));
    vec3 fallback = BG_COLOR + BG_FALLBACK_LIFT * vec3(uv.x, uv.y, 1.0 - uv.y);
    return mix(fallback, mix(fallback, tex, BG_TEXTURE_STRENGTH), hasTex);
}

// ========================================================================
// SMOKE FIELD
// ========================================================================

vec2 smokeDomain(vec2 local, float t)
{
    float distFromCenter = length(local);
    float angle = atan(local.y, local.x);
    float n = fbm(local * SWIRL_NOISE_SCALE + t * SWIRL_NOISE_DRIFT);

    float swirlAngle = angle + t * SWIRL_SPEED + distFromCenter * SWIRL_TWIST + n * SWIRL_NOISE_AMOUNT;
    vec2 spun = vec2(cos(swirlAngle), sin(swirlAngle)) * distFromCenter;
    vec2 warpedLocal = mix(local, spun, SWIRL_STRENGTH);

    return warpedLocal * SMOKE_SCALE + t * SMOKE_DRIFT;
}

float warpedSmoke(vec2 p, float t)
{
    vec2 q;
    q.x = fbm(p + WARP_OFFSET_A + t * WARP_DRIFT_A);
    q.y = fbm(p + WARP_OFFSET_B + t * WARP_DRIFT_B);

    vec2 r;
    r.x = fbm(p + q * WARP_STRENGTH_A + WARP_OFFSET_C + t * WARP_DRIFT_C);
    r.y = fbm(p + q * WARP_STRENGTH_A + WARP_OFFSET_D + t * WARP_DRIFT_D);

    float broad = fbm(p + r * WARP_STRENGTH_B);
    float fine = fbm(p * FINE_SMOKE_SCALE + q * WARP_STRENGTH_B + t * (WARP_DRIFT_A - WARP_DRIFT_B));
    float density = mix(broad, fine, FINE_SMOKE_MIX);

    return pow(clamp(density, 0.0, 1.0), SMOKE_CONTRAST);
}

float wispMask(vec2 local, vec2 p, float density, float t)
{
    float radius = length(local);
    float angle = atan(local.y, local.x);
    float ribbon = sin(angle * WISP_ARMS + radius * WISP_RADIAL_FREQ - t * WISP_SPEED + density * WISP_NOISE_BEND);
    ribbon = pow(0.5 + 0.5 * ribbon, WISP_RIBBON_POWER);

    float n = fbm(p * WISP_NOISE_SCALE + t * (WARP_DRIFT_C + WARP_DRIFT_D));
    float w = ribbon * WISP_RIBBON_WEIGHT + n * WISP_NOISE_WEIGHT;
    return smoothstep(WISP_THRESHOLD, WISP_THRESHOLD + WISP_SOFTNESS, w);
}

vec3 smokeColor(float density, float wisp, float edgeBoost)
{
    float body = smoothstep(SMOKE_DENSITY_LOW, SMOKE_DENSITY_HIGH, density);
    vec3 col = mix(SMOKE_BLACK, SMOKE_CHARCOAL, body);
    col = mix(col, SMOKE_WISP, wisp * WISP_HIGHLIGHT);
    col = mix(col, EDGE_INK, edgeBoost);
    return clamp(col, 0.0, 1.0);
}

// ========================================================================
// MAIN IMAGE
// ========================================================================

void mainImage(out vec4 fragColor, in vec2 fragCoord)
{
    vec2 uv = fragCoord / iResolution.xy;
    vec2 local = cardLocal(uv);
    float sdf = cardSDF(uv);

    float insideCard = smoothstep(CARD_EDGE_FEATHER, -CARD_EDGE_FEATHER, sdf);
    float outsideDistance = max(sdf, 0.0);
    float outsideReach = smoothstep(OUTSIDE_REACH, 0.0, outsideDistance);

    vec2 p = smokeDomain(local, iTime);
    float density = warpedSmoke(p, iTime);
    float wisp = wispMask(local, p, density, iTime);

    float edgeBoost = smoothstep(EDGE_BOOST_WIDTH, 0.0, abs(sdf)) * EDGE_DENSITY_BOOST;
    density = clamp(density + edgeBoost, 0.0, 1.0);

    float smokeBody = smoothstep(SMOKE_DENSITY_LOW, SMOKE_DENSITY_HIGH, density);
    float outsideWisps = pow(clamp(max(smokeBody, wisp), 0.0, 1.0), OUTSIDE_WISP_POWER);
    float outsideMask = smoothstep(0.0, OUTSIDE_SOFTNESS, outsideReach * outsideWisps);
    float outsideAlpha = outsideMask * outsideReach * outsideWisps * OUTSIDE_MAX_ALPHA;

    float smokeAlpha = max(insideCard * INTERIOR_ALPHA, outsideAlpha * (1.0 - insideCard));

    vec3 card = sampleCard(uv);
    vec3 bg = sampleBackground(uv);
    vec3 base = mix(bg, card, insideCard);
    vec3 smoke = smokeColor(density, wisp, edgeBoost);
    vec3 col = mix(base, smoke, smokeAlpha);

    vec2 vc = uv - 0.5;
    col *= 1.0 - VIGNETTE_AMOUNT * dot(vc, vc) * 2.5;

    float grain = hash21(floor(fragCoord) + fract(iTime * GRAIN_SPEED) * TWO_PI);
    col += (grain - 0.5) * GRAIN_INTENSITY * smokeAlpha;

    fragColor = vec4(clamp(col, 0.0, 1.0), 1.0);
}
