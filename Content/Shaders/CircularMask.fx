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

    float alpha = 1.0;

    if (NumMasks > 0)
    {
        // Combine multiple masks: 0 inside any circle, 1 outside all
        [unroll]
        for (int i = 0; i < MAX_MASKS; i++)
        {
            if (i < NumMasks)
            {
                float d = distance(screenPx, MaskCenters[i]);
                float ai = smoothstep(MaskRadii[i], MaskRadii[i] + feather, d); // 0 inside, 1 outside
                alpha = min(alpha, ai);
            }
        }
    }
    else
    {
        // Fallback to single mask
        float d = distance(screenPx, MaskCenterPx);
        alpha = smoothstep(MaskRadiusPx, MaskRadiusPx + feather, d);
    }

    return float4(0.0, 0.0, 0.0, alpha) * input.Color;
}

technique SpriteDrawing
{
    pass P0
    {
        VertexShader = compile vs_3_0 SpriteVertexShader();
        PixelShader  = compile ps_3_0 SpritePixelShader();
    }
}


