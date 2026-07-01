// Falling leaves bokeh depth-of-field pass, converted from FallingLeavesBufferB.glsl.

float4x4 MatrixTransform;
float2 ViewportSize;

float BlurStrength = 0.05;
float2 FocusA = float2(0.6, 0.6);
float2 FocusB = float2(0.5, 0.5);
float2 BokehAspect = float2(0.5625, 1.0);

static const int BokehRadius = 6;

texture Texture : register(t0);
sampler2D TextureSampler : register(s0) = sampler_state
{
    Texture = <Texture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};

struct VSInput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

struct VSOutput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

VSOutput SpriteVertexShader(VSInput input)
{
    VSOutput output;
    output.Position = mul(input.Position, MatrixTransform);
    output.Color = input.Color;
    output.TexCoord = input.TexCoord;
    return output;
}

float2 ShaderUvToTextureUv(float2 uv)
{
    return float2(uv.x, 1.0 - uv.y);
}

float4 Bokeh(float2 p, float b)
{
    float4 col = float4(0.0, 0.0, 0.0, 0.0);
    float sampleCount = 0.0;

    [loop]
    for (int i = -BokehRadius; i <= BokehRadius; i++)
    {
        [loop]
        for (int j = -BokehRadius; j <= BokehRadius; j++)
        {
            float2 off = float2(i, j) / BokehRadius;
            if (dot(off, off) < 1.0)
            {
                col += tex2D(TextureSampler, ShaderUvToTextureUv(p + b * off * BokehAspect));
                sampleCount += 1.0;
            }
        }
    }

    return col / max(sampleCount, 1.0);
}

float4 SpritePixelShader(VSOutput input) : COLOR0
{
    float2 p = float2(input.TexCoord.x, 1.0 - input.TexCoord.y);
    float b = dot(p - FocusA, p - FocusB) * BlurStrength;
    float3 col = Bokeh(p, b).rgb;
    return float4(saturate(col), 1.0) * input.Color;
}

technique SpriteDrawing
{
    pass P0
    {
        VertexShader = compile vs_3_0 SpriteVertexShader();
        PixelShader = compile ps_3_0 SpritePixelShader();
    }
}
