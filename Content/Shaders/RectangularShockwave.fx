// Rectangular Shockwave post-process (SpriteBatch-compatible, Reach profile)
// Similar to Shockwave.fx but uses rectangular SDF instead of circular

float4x4 MatrixTransform;
float2 ViewportSize;          // in pixels

// Shockwave parameters (screen pixel space)
float2 BoundsCenterPx;        // center of rectangle in pixels
float2 BoundsSizePx;          // width and height of rectangle in pixels
float  t = 0.0;               // normalized time 0..1
float  MaxRadiusPx = 600.0;   // max expansion distance in pixels
float  RippleWidthPx = 24.0;  // feather width for the ripple band (px)
float  Strength = 1.0;        // offset strength scale

// Chromatic aberration
float  ChromaticAberrationAmp  = 0.05;   // time phase offset amplitude
float  ChromaticAberrationFreq = 3.14159; // frequency for phase offset

// Lighting/shading along the ripple
float  ShadingIntensity = 0.6;

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

// Compute the signed distance band strength for the ripple and mask/intros/outros
float getOffsetStrength(float tn, float2 screenPx)
{
    // Rectangle SDF in pixel space (expands outward uniformly from all edges)
    float2 halfSize = BoundsSizePx / 2.0;
    float2 centerOffset = screenPx - BoundsCenterPx;
    float2 q = abs(centerOffset) - halfSize;
    
    // Signed distance to rectangle boundary
    // Inside: negative distance, Outside: positive distance
    float d = length(max(q, 0.0)) + min(max(q.x, q.y), 0.0);
    
    // Subtract the expanding wavefront distance
    d = d - tn * MaxRadiusPx;

    // Mask the ripple to a thin band around the wavefront
    float band = 1.0 - smoothstep(0.0, max(RippleWidthPx, 1e-3), abs(d));

    // Smooth intro/outro across time
    float intro = smoothstep(0.0, 0.05, tn);
    float outro = 1.0 - smoothstep(0.5, 1.0, tn);
    return d * band * intro * outro;
}

float4 SpritePixelShader(VSOutput input) : COLOR0
{
    // Work in pixel space for offsets, then convert back to UV when sampling
    float2 screenPx = input.TexCoord * ViewportSize;
    
    // Compute direction from rectangle center for offset direction
    float2 dirPx = screenPx - BoundsCenterPx;
    
    // Chromatic aberration temporal phase
    float tOffset = ChromaticAberrationAmp * sin(t * ChromaticAberrationFreq);
    float rD = getOffsetStrength(t + tOffset, screenPx);
    float gD = getOffsetStrength(t,            screenPx);
    float bD = getOffsetStrength(t - tOffset,  screenPx);

    // Normalize direction for offset direction; avoid divide by zero
    float len = max(length(dirPx), 1e-5);
    float2 dirN = dirPx / len;

    float2 uvR = (screenPx + dirN * rD * Strength) / ViewportSize;
    float2 uvG = (screenPx + dirN * gD * Strength) / ViewportSize;
    float2 uvB = (screenPx + dirN * bD * Strength) / ViewportSize;

    float3 col;
    col.r = tex2D(TextureSampler, uvR).r;
    col.g = tex2D(TextureSampler, uvG).g;
    col.b = tex2D(TextureSampler, uvB).b;

    float shading = gD * ShadingIntensity / max(MaxRadiusPx, 1.0);
    col = saturate(col + shading);

    return float4(col, 1.0) * input.Color;
}

technique SpriteDrawing
{
    pass P0
    {
        VertexShader = compile vs_3_0 SpriteVertexShader();
        PixelShader  = compile ps_3_0 SpritePixelShader();
    }
}



