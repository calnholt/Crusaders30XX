// Falling leaves final post-process pass, converted from FallingLeavesImage.glsl.

float4x4 MatrixTransform;
float2 ViewportSize;

float BloomRadius = 0.04;
float BloomLod = 3.0;
float BloomPower = 2.0;
float RadialAmount = 0.2;
float RadialLength = 0.3;
float2 RadialTarget = float2(1.0, 1.0);
float Saturation = -0.6;
float3 ColorGrade = float3(0.84, 1.0, 0.9);
float Vignette = 0.1;

static const int BloomRadiusSamples = 8;
static const int RadialSamples = 64;

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

float3 ACES(float3 x)
{
    float a = 2.51;
    float b = 0.03;
    float c = 2.43;
    float d = 0.59;
    float e = 0.14;
    return (x * (a * x + b)) / max(x * (c * x + d) + e, 0.0001);
}

float4 Bloom(float2 p)
{
    float4 col = float4(0.0, 0.0, 0.0, 0.0);
    float sampleCount = 0.0;

    [loop]
    for (int i = -BloomRadiusSamples; i <= BloomRadiusSamples; i++)
    {
        [loop]
        for (int j = -BloomRadiusSamples; j <= BloomRadiusSamples; j++)
        {
            float2 off = float2(i, j) / BloomRadiusSamples;
            float2 sampleUv = ShaderUvToTextureUv(p + off * BloomRadius);
            col += tex2Dlod(TextureSampler, float4(sampleUv, 0.0, max(BloomLod, 0.0)));
            sampleCount += 1.0;
        }
    }

    return col / max(sampleCount, 1.0);
}

float4 RadialBlur(float2 p, float2 v)
{
    float4 col = float4(0.0, 0.0, 0.0, 0.0);
    float weightSum = 0.0;

    [loop]
    for (int sampleIndex = 0; sampleIndex < RadialSamples; sampleIndex++)
    {
        float i = (sampleIndex + 0.5) / (float)RadialSamples;
        float weight = pow(1.0 - sqrt(i), 3.0);
        col += weight * tex2D(TextureSampler, ShaderUvToTextureUv(p + RadialLength * i * v));
        weightSum += weight;
    }

    return col / max(weightSum, 0.0001);
}

float4 SpritePixelShader(VSOutput input) : COLOR0
{
    float2 p = float2(input.TexCoord.x, 1.0 - input.TexCoord.y);
    float3 col = tex2D(TextureSampler, input.TexCoord).rgb;
    float3 bloomC = pow(max(Bloom(p).rgb, 0.0), max(BloomPower, 0.001));
    float2 v = RadialTarget - p;
    float len = max(length(v), 0.0001);
    v /= len;
    float3 blur = max(RadialAmount, 0.0) * RadialBlur(p, v).rgb;

    col += blur;
    col = pow(max(col, 0.0), 0.4545);
    col += bloomC;
    col = ACES(col);
    col = lerp(col, dot(col, float3(1.0, 1.0, 1.0)) / 3.0, Saturation);
    col = pow(max(col, 0.0), max(ColorGrade, float3(0.001, 0.001, 0.001)));

    float vignetteBase = max(64.0 * p.x * p.y * (1.0 - p.x) * (1.0 - p.y), 0.0);
    col *= pow(vignetteBase, max(Vignette, 0.0));
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
