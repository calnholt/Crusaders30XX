/* ──────────────────────────────────────────────────────────────
   Brittle Card — ShaderToy

   A card crumbling into irregular chunks: pieces break off, fall away
   while fading out, then fade back onto the card and the cycle repeats.
   Chunks are VORONOI cells (irregular polygons), so the intact card is
   solid but shatters into natural shard shapes.

   Paste into shadertoy.com/new. Set iChannel0 to the card image (PNG/JPG).
   A procedural placeholder shows if no texture is bound, so you can preview
   standalone. All tunables at the top.

   HOW THE EFFECT IS BUILT:
   - The card is partitioned into a VORONOI mosaic of chunk cells.
   - Each chunk runs a looping lifecycle (attached → falling → reform).
   - The home cell shows a hole while its chunk is gone.
   - Falling chunks are gathered per-pixel from cells ABOVE: we undo each
     candidate chunk's displacement and test if the pixel lands in its
     voronoi cell, so irregular shapes fall and overlap correctly.
   ────────────────────────────────────────────────────────────── */

// ═══════════════════════════════════════════════════════════════
// TUNABLES
// ═══════════════════════════════════════════════════════════════

// ── Card Region (0..1 screen space, y=0 bottom) ───────────────
const float CARD_LEFT   = 0.30;  // left edge of card
const float CARD_RIGHT  = 0.70;  // right edge of card
const float CARD_BOTTOM = 0.20;  // bottom edge of card
const float CARD_TOP    = 0.90;  // top edge of card
const float CARD_RADIUS = 0.03;  // rounded-corner radius

// ── Chunk Shapes ──────────────────────────────────────────────
const float GRID_MIN    = 18.0;  // smallest chunk density (fewer, bigger shards)
const float GRID_MAX    = 18.0;  // largest chunk density (more, smaller shards)
const float GRID_SEED   = 12.0;  // reroll knob: change this number to pick a new random GRID in [MIN,MAX].
                                   // (in main, swap to iDate.w for a fresh random size every run)
const float CELL_JITTER = 0.9;   // shape irregularity (0 = regular hex-ish tiles, 1 = chaotic shards)
const float SEAM_WIDTH  = 0.00;  // gap between chunks (cell units). Higher = wider cracks/mosaic grout

// ── How Many Chunks Fall ──────────────────────────────────────
const float FALL_FRACTION = 0.15;  // YOUR MAIN KNOB: share of chunks that crumble = how many fall at once.
                                   // 0 = none, 0.2 = light sprinkle, 1 = whole card disintegrates

// ── Crumble Timing ────────────────────────────────────────────
// Each chunk runs on its OWN random loop period (desynced), and re-rolls
// whether it falls every cycle — so different pieces crumble over time
// instead of the same set repeating.
const float PERIOD_MIN = 2.5;   // shortest per-chunk loop, seconds (lower = that chunk falls more often)
const float PERIOD_MAX = 9.0;   // longest per-chunk loop, seconds (wider gap from MIN = more varied rhythm)
const float ATTACH_END = 0.45;  // fraction of a cycle a chunk stays attached before breaking off
const float FALL_END   = 0.80;  // fraction of a cycle when the chunk has fully fallen + faded
                                // (FALL_END..1.0 is the "fade back onto card" reform phase)

// ── Falling Motion ────────────────────────────────────────────
const float MAX_FALL     = 12.0;  // how far a chunk falls, in cell heights (keep <= SEARCH_CELLS)
const float MAX_DRIFT    = 1.2;   // sideways sway as a chunk falls, in cell widths
const float FALL_GRAVITY = 2.0;   // fall acceleration (1 = linear, 2 = quadratic ease-in, higher = snappier)
const float FALL_ROT     = 2.2;   // max spin per chunk as it falls, in radians (0 = no rotation, 6.28 = a full turn).
                                  // Each shard gets a random direction + magnitude up to this.

// ── Look ──────────────────────────────────────────────────────
const float DEBRIS_DARK    = 0.95;                    // how much chunks dim as they fall (1 = no dimming, 0 = fade to black)
const vec3  EDGE_GLOW      = vec3(1.0, 0.85, 0.45); // hot rim on broken chunk edges (set to 0 to disable)
const float EDGE_GLOW_AMT  = 0.6;                     // strength of that broken-edge glow
const float HOLE_DARKEN    = 0.85;                    // darkness of the empty hole left behind (0 = black, 1 = full bg)

// ── Background ────────────────────────────────────────────────
const vec3 BG_COLOR = vec3(0.03, 0.04, 0.06);  // backdrop behind card + falling debris

// ── Gather Quality ────────────────────────────────────────────
#define SEARCH_CELLS 9  // cells scanned upward for falling chunks (>= MAX_FALL)
#define DRIFT_CELLS  2  // horizontal scan radius for drifting chunks (>= ceil(MAX_DRIFT)+1)

// ═══════════════════════════════════════════════════════════════
// HASH / NOISE
// ═══════════════════════════════════════════════════════════════

float hash21(vec2 p)
{
    vec3 p3 = fract(vec3(p.xyx) * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return fract((p3.x + p3.y) * p3.z);
}

vec2 hash22(vec2 p)
{
    vec3 p3 = fract(vec3(p.xyx) * vec3(0.1031, 0.1030, 0.0973));
    p3 += dot(p3, p3.yzx + 33.33);
    return fract((p3.xx + p3.yz) * p3.zy);
}

// ═══════════════════════════════════════════════════════════════
// SIGNED DISTANCE: CARD
// ═══════════════════════════════════════════════════════════════

float cardSDF(vec2 uv)
{
    vec2 c = vec2((CARD_LEFT + CARD_RIGHT) * 0.5, (CARD_BOTTOM + CARD_TOP) * 0.5);
    vec2 h = vec2((CARD_RIGHT - CARD_LEFT) * 0.5, (CARD_TOP - CARD_BOTTOM) * 0.5) - CARD_RADIUS;
    vec2 d = abs(uv - c) - h;
    return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0) - CARD_RADIUS;
}

// ═══════════════════════════════════════════════════════════════
// VORONOI CHUNK MOSAIC
// ═══════════════════════════════════════════════════════════════

// Jittered feature point for chunk cell `c`.
vec2 featPt(vec2 c)
{
    return c + 0.5 + (hash22(c) - 0.5) * CELL_JITTER;
}

// Evaluate the voronoi mosaic at q. Returns the nearest cell id; writes
// the nearest (d1) and second-nearest (d2) distances. d2-d1 is small at
// a cell boundary — that's how we carve the seams between chunks.
vec2 voro(vec2 q, out float d1, out float d2)
{
    vec2 ip = floor(q);
    d1 = 1e9;
    d2 = 1e9;
    vec2 id = ip;

    for (int dy = -1; dy <= 1; dy++)
        for (int dx = -1; dx <= 1; dx++)
        {
            vec2 c = ip + vec2(float(dx), float(dy));
            vec2 f = featPt(c);
            float d = dot(q - f, q - f);
            if (d < d1)
            {
                d2 = d1;
                d1 = d;
                id = c;
            }
            else if (d < d2)
            {
                d2 = d;
            }
        }

    d1 = sqrt(d1);
    d2 = sqrt(d2);
    return id;
}

// ═══════════════════════════════════════════════════════════════
// CARD COLOR SOURCE
// ═══════════════════════════════════════════════════════════════

// Sample the card image; fall back to a procedural pattern if no
// texture is bound (so the shader previews standalone).
vec3 sampleCard(vec2 uv)
{
    vec3 tex = texture(iChannel0, uv).rgb;
    vec2 g = abs(fract(uv * 12.0) - 0.5);
    float line = smoothstep(0.45, 0.5, max(g.x, g.y));
    vec3 proc = mix(vec3(0.20, 0.35, 0.55), vec3(0.85, 0.80, 0.55), uv.y);
    proc = mix(proc, vec3(1.0), line * 0.25);
    float hasTex = step(0.01, dot(tex, vec3(1.0)));
    return mix(proc, tex, hasTex);
}

// ═══════════════════════════════════════════════════════════════
// CHUNK LIFECYCLE
// ═══════════════════════════════════════════════════════════════

// Per-chunk state this frame.
struct Chunk
{
    float fall;   // fall distance (cell heights, downward)
    float drift;  // sideways drift (cell widths, signed)
    float alpha;  // visibility of the FALLING piece
    float home;   // home presence (1 = sitting at home, 0 = hole)
    float angle;  // current spin (radians)
};

Chunk chunkLife(vec2 cell)
{
    Chunk C = Chunk(0.0, 0.0, 0.0, 1.0, 0.0);  // default: attached & whole

    // Each chunk has its OWN loop period → cells desync, no global rhythm.
    float period = mix(PERIOD_MIN, PERIOD_MAX, hash21(cell + 5.7));
    float tl = iTime / period + hash21(cell);  // this chunk's timeline
    float cyc = floor(tl);                     // which cycle we're in
    float u = fract(tl);                       // 0..1 within the cycle

    // Re-roll EACH cycle whether this chunk falls. FALL_FRACTION is the
    // per-cycle chance → over time many different pieces crumble, and the
    // count falling at once still tracks FALL_FRACTION.
    if (hash21(cell + vec2(cyc, 0.7) * 1.7) > FALL_FRACTION)
        return C;

    // Motion seeds re-rolled per cycle so repeats look different.
    float dirX = (hash21(cell + vec2(cyc, 7.3)) - 0.5) * 2.0;
    float rotV = (hash21(cell + vec2(cyc, 11.7)) - 0.5) * 2.0 * FALL_ROT;

    if (u < ATTACH_END)
    {
        return C;  // attached & whole
    }
    else if (u < FALL_END)
    {
        float fp = (u - ATTACH_END) / (FALL_END - ATTACH_END);  // 0→1 through fall
        C.fall = pow(fp, FALL_GRAVITY) * MAX_FALL;
        C.drift = dirX * MAX_DRIFT * fp;
        C.alpha = 1.0 - fp;
        C.home = 0.0;  // leaves a hole
        C.angle = rotV * fp;
        return C;
    }
    else
    {
        float rp = (u - FALL_END) / (1.0 - FALL_END);  // 0→1 reform
        C.home = rp;  // fades back onto card
        return C;
    }
}

// ═══════════════════════════════════════════════════════════════
// MAIN IMAGE
// ═══════════════════════════════════════════════════════════════

void mainImage(out vec4 fragColor, in vec2 fragCoord)
{
    vec2 uv = fragCoord / iResolution.xy;
    float aspect = iResolution.x / iResolution.y;
    vec3 col = BG_COLOR;

    // Pick a random GRID density within [GRID_MIN, GRID_MAX]. Constant for
    // the whole frame (same mosaic everywhere); bump GRID_SEED to reroll.
    // (Well-spread scalar hash so consecutive seeds give very different sizes.)
    float gRand = fract(sin(GRID_SEED * 12.9898 + 78.233) * 43758.5453);
    float GRID = floor(mix(GRID_MIN, GRID_MAX, gRand));

    // Square-ish chunk space: GRID cells tall, scaled across by aspect.
    vec2 gridDim = vec2(floor(GRID * aspect), GRID);
    vec2 p = uv * gridDim;

    // ── HOME LAYER: card carved into chunks, holes where pieces left ──
    if (cardSDF(uv) < 0.0)
    {
        float hd1, hd2;
        vec2 homeC = voro(p, hd1, hd2);
        Chunk L = chunkLife(homeC);
        vec3 card = sampleCard(uv);
        float seam = smoothstep(0.0, SEAM_WIDTH, hd2 - hd1);  // 0 at boundary → 1 inside chunk
        float show = L.home * seam;
        col = mix(BG_COLOR * HOLE_DARKEN, card, show);
    }

    // ── FALLING LAYER: gather chunks that broke off cells above ──
    // For each candidate cell, undo its displacement (q = p - D) and ask:
    // does q land inside that cell's voronoi region? If so, the displaced
    // chunk covers this pixel.
    for (int ky = 0; ky <= SEARCH_CELLS; ky++)
    {
        for (int kx = -DRIFT_CELLS; kx <= DRIFT_CELLS; kx++)
        {
            vec2 c = floor(p) + vec2(float(kx), float(ky));
            Chunk L = chunkLife(c);
            if (L.alpha <= 0.001)
                continue;  // not currently falling

            // Map the pixel back into the chunk's REST frame, undoing both
            // its fall/drift AND its spin (rotation is about the shard's own
            // center, so membership + texture stay correct).
            vec2 rest = featPt(c);                         // chunk center at rest
            vec2 centerNow = rest + vec2(L.drift, -L.fall);  // displaced center
            float cs = cos(-L.angle), sn = sin(-L.angle);  // inverse rotation
            vec2 rel = p - centerNow;
            vec2 q = mat2(cs, -sn, sn, cs) * rel + rest;  // pixel in rest frame

            float qd1, qd2;
            vec2 nc = voro(q, qd1, qd2);
            if (!all(equal(nc, c)))
                continue;  // q not inside chunk c

            float seam = smoothstep(0.0, SEAM_WIDTH, qd2 - qd1);
            if (seam <= 0.0)
                continue;

            vec2 srcUV = q / gridDim;  // rest-frame uv → sample original art
            if (cardSDF(srcUV) > 0.0)
                continue;  // chunk must originate on the card

            vec3 cc = sampleCard(srcUV);

            // Dim as it falls + hot rim along the fractured edge.
            float fallFrac = clamp(L.fall / MAX_FALL, 0.0, 1.0);
            cc *= mix(1.0, DEBRIS_DARK, fallFrac);
            float edge = 1.0 - smoothstep(0.0, SEAM_WIDTH + 0.15, qd2 - qd1);
            cc += EDGE_GLOW * EDGE_GLOW_AMT * edge * L.alpha;
            col = mix(col, cc, L.alpha * seam);
        }
    }

    fragColor = vec4(clamp(col, 0.0, 1.0), 1.0);
}
