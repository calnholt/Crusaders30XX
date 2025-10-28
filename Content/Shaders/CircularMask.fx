// Circular Mask Overlay (SpriteBatch-compatible, Reach profile)
// Reveals only circular areas around one or more centers; soft feathered edge; rest is black

float4x4 MatrixTransform;
float2 ViewportSize;          // in pixels

// Single-mask parameters (fallback when NumMasks == 0)
float2 MaskCenterPx;          // center in pixels
float  MaskRadiusPx = 140.0;  // radius in pixels

// Multi-mask parameters
static const int MAX_MASKS = 64;
float2 MaskCenters[MAX_MASKS];
float  MaskRadii[MAX_MASKS];
int    NumMasks = 0;          // if > 0, uses arrays above

float  FeatherPx    = 4.0;    // edge softness in pixels
// Global easing for entire mask alpha
float  iTime        = 0.0;    // seconds
float  EaseSpeed    = 1.0;    // cycles per second (2π rad / sec scaled inside)
float  GlobalAlphaMin = 0.5;  // minimum alpha when fully eased in
float  GlobalAlphaMax = 0.75;  // maximum alpha when fully eased out
float  DeathContrast  = 1.3;   // contrast multiplier for lifeless outside region
float  LifelessDesaturateMix = 0.3; // mix factor toward original color (0=gray,1=color)
float  LifelessDarkenMul    = 0.7;  // brightness multiplier after desaturation

// Legacy simple horizontal distortion controls (kept for compatibility; not used by the new warp)
float  DistortAmplitudePx = 8.0;   // horizontal shift in pixels
float  DistortSpatialFreq = 0.005; // cycles per pixel along Y (e.g., 0.005 -> 1 cycle per 200px)
float  DistortSpeed      = 0.2;    // cycles per second

// Domain-warp controls
float  NoiseScale   = 0.004;  // pixels -> noise space scale
float  WarpAmountPx = 12.0;   // max pixel displacement from warp
float  WarpSpeed    = 0.7;    // time multiplier for noise animation

// Camera world-space vertical origin (in pixels). Used to anchor distortion to world Y
float  CameraOriginYPx   = 0.0;

texture Texture : register(t0);
sampler2D TextureSampler : register(s0) = sampler_state
{
    Texture = <Texture>;
    MinFilter = Linear; MagFilter = Linear; MipFilter = Linear;
    AddressU = Clamp; AddressV = Clamp;
};

// Dedicated noise texture for procedural domain warping
texture NoiseTex : register(t1);
sampler2D NoiseSampler : register(s1) = sampler_state
{
    Texture = <NoiseTex>;
    MinFilter = Linear; MagFilter = Linear; MipFilter = Linear;
    AddressU = Wrap; AddressV = Wrap;
};

struct VSInput  { float4 Position : POSITION0; float4 Color : COLOR0; float2 TexCoord : TEXCOORD0; };
struct VSOutput { float4 Position : POSITION0; float4 Color : COLOR0; float2 TexCoord : TEXCOORD0; };

VSOutput SpriteVertexShader(VSInput input)
{
    VSOutput o;
    o.Position = mul(input.Position, MatrixTransform);
    o.Color = input.Color;
    o.TexCoord = input.TexCoord;
    return o;
}

// --- Noise helpers (ported from GLSL to HLSL) ---
static const float INV_NOISE_TEX_SIZE = 0.00390625; // 1/256 - matches sample scale

float2 hash2(float n)
{
    float2 s = sin(float2(n, n + 1.0)) * float2(13.5453123, 31.1459123);
    return frac(s);
}

float noise(float2 x)
{
    float2 p = floor(x);
    float2 f = frac(x);
    f = f * f * (3.0 - 2.0 * f);

    // Sample dedicated noise texture (acts like iChannel0 in the reference)
    float a = tex2Dlod(NoiseSampler, float4((p + float2(0.5, 0.5)) * INV_NOISE_TEX_SIZE, 0.0, 0.0)).x;
    float b = tex2Dlod(NoiseSampler, float4((p + float2(1.5, 0.5)) * INV_NOISE_TEX_SIZE, 0.0, 0.0)).x;
    float c = tex2Dlod(NoiseSampler, float4((p + float2(0.5, 1.5)) * INV_NOISE_TEX_SIZE, 0.0, 0.0)).x;
    float d = tex2Dlod(NoiseSampler, float4((p + float2(1.5, 1.5)) * INV_NOISE_TEX_SIZE, 0.0, 0.0)).x;

    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}

static const float2x2 FBM_MTX = float2x2(0.80, 0.60, -0.60, 0.80);

float fbm(float2 p)
{
    float f = 0.0;
    float2 pp = p;

    f += 0.500000 * noise(pp); pp = mul(pp, FBM_MTX) * 2.02;
    f += 0.250000 * noise(pp); pp = mul(pp, FBM_MTX) * 2.03;
    f += 0.125000 * noise(pp); pp = mul(pp, FBM_MTX) * 2.01;
    f += 0.062500 * noise(pp); pp = mul(pp, FBM_MTX) * 2.04;
    f += 0.031250 * noise(pp); pp = mul(pp, FBM_MTX) * 2.01;
    f += 0.015625 * noise(pp);

    return f / 0.96875;
}

float pattern(in float2 p, in float t, in float2 uv, out float2 q, out float2 r, out float2 g)
{
    q = float2(fbm(p), fbm(p + float2(10.0, 1.3)));

    r = float2(
        fbm(p + 4.0 * q + float2(t, t) + float2(1.7, 9.2)),
        fbm(p + 4.0 * q + float2(t, t) + float2(8.3, 2.8))
    );
    g = float2(
        fbm(p + 2.0 * r + float2(t * 20.0, t * 20.0) + float2(2.0, 6.0)),
        fbm(p + 2.0 * r + float2(t * 10.0, t * 10.0) + float2(5.0, 3.0))
    );
    return fbm(p + 5.5 * g + float2(-t * 7.0, -t * 7.0));
}

float4 SpritePixelShader(VSOutput input) : COLOR0
{
    // When drawing a fullscreen quad with SpriteBatch, TexCoord spans 0..1 across the screen
    float2 screenPx = input.TexCoord * ViewportSize;

    // Avoid undefined smoothstep when FeatherPx == 0
    float feather = max(FeatherPx, 1e-3);

    // Compute a binary warp factor: 0 strictly inside any hole, 1 otherwise.
    // This keeps the inside undistorted but still warps right up to the edge.
    float warpAlpha = 1.0;
    if (NumMasks > 0)
    {
        [unroll]
        for (int i = 0; i < MAX_MASKS; i++)
        {
            if (i < NumMasks)
            {
                float rU = MaskRadii[i];
                float2 cU = MaskCenters[i];
                float dU = distance(screenPx, cU);
                if (dU <= rU) { warpAlpha = 0.0; }
            }
        }
    }
    else
    {
        float rU = MaskRadiusPx;
        float dU = distance(screenPx, MaskCenterPx);
        if (dU <= rU) { warpAlpha = 0.0; }
    }

    // Domain-warp the sampling coordinates for the outside region only
    float2 maskPx = screenPx;
    if (WarpAmountPx > 1e-4)
    {
        float2 p = screenPx * NoiseScale;
        float2 q, r, g;
        float f = pattern(p, iTime * WarpSpeed, input.TexCoord, q, r, g);
        // Center flows around 0 and avoid normalization to keep spatial variation
        float2 flow = float2(q.x - 0.5, r.y - 0.5) + 0.5 * (g - float2(0.5, 0.5));
        // Add a swirl component driven by f for more obvious distortion
        float angle = f * 6.28318530718;
        float2 swirlDir = float2(cos(angle), sin(angle));
        float swirlMag = ((q.x + r.y + g.x + g.y) * 0.25 - 0.5);
        float2 swirl = swirlDir * swirlMag;
        float2 warp = (flow + swirl) * (WarpAmountPx * 2.0) * warpAlpha;
        maskPx += warp;
    }

    // Compute outside factor (0 inside any hole, 1 far outside, feathered near edge)
    float outside = 1.0;

    if (NumMasks > 0)
    {
        // Combine multiple masks: min over all masks
        [unroll]
        for (int i = 0; i < MAX_MASKS; i++)
        {
            if (i < NumMasks)
            {
                float r = MaskRadii[i];
                float2 c = MaskCenters[i];
                float d0 = distance(screenPx, c); // undistorted distance for inside test
                float ai;
                if (d0 <= r)
                {
                    ai = 0.0; // inside stays undistorted/undarkened
                }
                else
                {
                    float dw = distance(maskPx, c); // distorted distance for outside feather
                    ai = smoothstep(r, r + feather, dw); // 0 near edge, 1 farther outside
                }
                outside = min(outside, ai);
            }
        }
    }
    else
    {
        // Fallback to single mask
        float r = MaskRadiusPx;
        float d0 = distance(screenPx, MaskCenterPx);
        if (d0 <= r)
        {
            outside = 0.0;
        }
        else
        {
            float dw = distance(maskPx, MaskCenterPx);
            outside = smoothstep(r, r + feather, dw);
        }
    }

    // Sample the scene texture undistorted and distorted
    float2 uvUndist = input.TexCoord;                  // 0..1 across the screen
    float2 uvDist   = maskPx / ViewportSize;           // warped sampling coordinates
    float4 colUndist = tex2D(TextureSampler, uvUndist);
    float4 colDist   = tex2D(TextureSampler, uvDist);

    // Blend between undistorted (inside) and distorted (outside)
    float4 sceneCol = lerp(colUndist, colDist, outside);

    // Global easing-driven intensity applied only outside the holes
    float phase   = iTime * EaseSpeed * 6.28318530718; // 2π * t * speed
    float ease    = 0.5 + 0.5 * sin(phase);            // 0..1
    float globalA = lerp(GlobalAlphaMin, GlobalAlphaMax, ease);
    float effect  = outside * globalA;

    // Desaturate + darken (lifeless look)
    float gray = dot(sceneCol.rgb, float3(0.3, 0.59, 0.11));
    float3 lifeless = lerp(float3(gray, gray, gray), sceneCol.rgb, LifelessDesaturateMix);
    lifeless *= LifelessDarkenMul;
    lifeless = saturate((lifeless - 0.5) * DeathContrast + 0.5);

    // Apply outside-only with feathering and easing
    sceneCol.rgb = lerp(sceneCol.rgb, lifeless, effect);

    // Output fully opaque scene color (SpriteBatch blending is fine with alpha=1)
    return float4(sceneCol.rgb, 1.0) * input.Color;
}

technique SpriteDrawing
{
    pass P0
    {
        VertexShader = compile vs_3_0 SpriteVertexShader();
        PixelShader  = compile ps_3_0 SpritePixelShader();
    }
}


