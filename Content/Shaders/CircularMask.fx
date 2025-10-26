// Circular Mask Overlay (SpriteBatch-compatible, Reach profile)
// Reveals only circular areas around one or more centers; soft feathered edge; rest is black

float4x4 MatrixTransform;
float2 ViewportSize;          // in pixels

// Single-mask parameters (fallback when NumMasks == 0)
float2 MaskCenterPx;          // center in pixels
float  MaskRadiusPx = 140.0;  // radius in pixels

// Multi-mask parameters
static const int MAX_MASKS = 32;
float2 MaskCenters[MAX_MASKS];
float  MaskRadii[MAX_MASKS];
int    NumMasks = 0;          // if > 0, uses arrays above

float  FeatherPx    = 4.0;    // edge softness in pixels
// Global easing for entire mask alpha
float  iTime        = 0.0;    // seconds
float  EaseSpeed    = 1.0;    // cycles per second (2π rad / sec scaled inside)
float  GlobalAlphaMin = 0.5;  // minimum alpha when fully eased in
float  GlobalAlphaMax = 0.75;  // maximum alpha when fully eased out

// Horizontal distortion (mask warping) so fog shifts left/right like a sine wave
float  DistortAmplitudePx = 8.0;   // horizontal shift in pixels
float  DistortSpatialFreq = 0.005; // cycles per pixel along Y (e.g., 0.005 -> 1 cycle per 200px)
float  DistortSpeed      = 0.2;    // cycles per second

// Camera world-space vertical origin (in pixels). Used to anchor distortion to world Y
float  CameraOriginYPx   = 0.0;

texture Texture : register(t0);
sampler2D TextureSampler : register(s0) = sampler_state
{
    Texture = <Texture>;
    MinFilter = Linear; MagFilter = Linear; MipFilter = Linear;
    AddressU = Clamp; AddressV = Clamp;
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

float4 SpritePixelShader(VSOutput input) : COLOR0
{
    // When drawing a fullscreen quad with SpriteBatch, TexCoord spans 0..1 across the screen
    float2 screenPx = input.TexCoord * ViewportSize;

    // Avoid undefined smoothstep when FeatherPx == 0
    float feather = max(FeatherPx, 1e-3);

    // Apply horizontal sine-wave distortion used for warping OUTSIDE the holes
    float tau = 6.28318530718;
    // Anchor the wave to world Y by offsetting with camera origin
    float wave = sin(iTime * DistortSpeed * tau + (screenPx.y + CameraOriginYPx) * DistortSpatialFreq * tau);
    float2 maskPx = screenPx;
    maskPx.x += DistortAmplitudePx * wave;

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

    // Global easing-driven darkening applied only outside the holes
    float phase   = iTime * EaseSpeed * 6.28318530718; // 2π * t * speed
    float ease    = 0.5 + 0.5 * sin(phase);            // 0..1
    float globalA = lerp(GlobalAlphaMin, GlobalAlphaMax, ease);
    float darkAmt = outside * globalA;
    sceneCol.rgb = lerp(sceneCol.rgb, float3(0.0, 0.0, 0.0), darkAmt);

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


