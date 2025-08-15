// MonoGame/DirectX HLSL effect for SpriteBatch (DesktopGL Reach-compatible)

// Match the user's Shadertoy-like parameter names
float2 iResolution;      // viewport width/height
float  iTime;            // seconds
float4x4 MatrixTransform; // required by SpriteBatch; set automatically if present

// Main background texture bound by SpriteBatch
texture Texture : register(t0);
sampler2D TextureSampler : register(s0) = sampler_state
{
    Texture = <Texture>;
    MinFilter = Linear; MagFilter = Linear; MipFilter = Linear;
    AddressU = Clamp; AddressV = Clamp;
};

// Distortion map bound at s1
texture NoiseTexture : register(t1);
sampler2D NoiseSampler : register(s1) = sampler_state
{
    Texture = <NoiseTexture>;
    MinFilter = Linear; MagFilter = Linear; MipFilter = Linear;
    AddressU = Wrap; AddressV = Wrap;
};

float2 iChannelResolution1; // noise texture size

struct VSInput {
    float4 Position : POSITION0;
    float4 Color    : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

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
    float2 uv = input.TexCoord;

    float2 uvDist = uv;
    uvDist.x += (1.0 / max(1.0, iResolution.x)) * sin(iTime / 100000.0);
    uvDist.y += (1.0 / max(1.0, iResolution.y)) * sin(iTime / 100000.0);

    float4 distortionColor = tex2D(NoiseSampler, uvDist);

    uv.x += distortionColor.r / 20.0;
    uv.y += distortionColor.g / 20.0;

    float4 col = tex2D(TextureSampler, uv);
    return col * input.Color;
}

technique SpriteDrawing
{
    pass P0
    {
        VertexShader = compile vs_2_0 SpriteVertexShader();
        PixelShader  = compile ps_2_0 SpritePixelShader();
    }
}


