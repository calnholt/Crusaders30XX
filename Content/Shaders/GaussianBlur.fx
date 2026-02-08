// Separable Gaussian blur (SpriteBatch-compatible, Reach profile)
// Use BlurDirection = (1,0) for horizontal, (0,1) for vertical pass

float4x4 MatrixTransform;
float2 ViewportSize;
float2 BlurDirection;       // (1,0) or (0,1)
float  BlurRadius = 4.0;    // spread in pixels

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
    float2 texelSize = BlurDirection / ViewportSize;
    float2 uv = input.TexCoord;

    // 9-tap gaussian kernel (sigma ~= BlurRadius / 3)
    // Precomputed weights for a normalized gaussian: sum = 1.0
    // We scale offsets by BlurRadius to control spread
    float weights[5] = { 0.227027, 0.194594, 0.121621, 0.054054, 0.016216 };

    float4 result = tex2D(TextureSampler, uv) * weights[0];

    for (int i = 1; i < 5; i++)
    {
        float2 offset = texelSize * (float(i) * BlurRadius / 4.0);
        result += tex2D(TextureSampler, uv + offset) * weights[i];
        result += tex2D(TextureSampler, uv - offset) * weights[i];
    }

    return result * input.Color;
}

technique SpriteDrawing
{
    pass P0
    {
        VertexShader = compile vs_3_0 SpriteVertexShader();
        PixelShader  = compile ps_3_0 SpritePixelShader();
    }
}
