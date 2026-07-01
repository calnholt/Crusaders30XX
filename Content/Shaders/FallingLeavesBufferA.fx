// Falling leaves raw scene pass, converted from FallingLeavesBufferA.glsl.

float4x4 MatrixTransform;
float2 ViewportSize;
float Time;

float LeafCount = 80.0;
float3 LeafGreenDark = float3(0.04, 0.22, 0.03);
float3 LeafGreenLight = float3(0.45, 0.80, 0.18);
float LeafHueJitter = 0.10;
float LeafBrightness = 1.0;
float ScrollX = -0.06;
float ScrollY = 0.05;
float2 Spread = float2(15.0, 13.0);
float2 SpinRate = float2(0.2, 0.3);
float LeafRadiusMin = 0.7;
float LeafRadiusVariation = 0.6;
float BackgroundBrightness = 1.0;
float3 BackgroundSkyLow = float3(0.02, 0.05, 0.08);
float3 BackgroundSkyHigh = float3(0.10, 0.16, 0.20);
float3 LightDir = float3(0.57735026919, 0.57735026919, 0.57735026919);
float GlarePower = 16.0;
float FogAmount = 0.8;
float FogFloor = 0.004;

static const float Pi = 3.14159265359;
static const int MaxLeafCount = 120;

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

float2 Rotate(float2 p, float a)
{
    float s = sin(a);
    float c = cos(a);
    return float2(c * p.x - s * p.y, s * p.x + c * p.y);
}

float Hash21(float2 p)
{
    p = frac(p * float2(452.127, 932.618));
    p += dot(p, p + 123.23);
    return frac(p.x * p.y);
}

float Noise(float2 p)
{
    float2 q = floor(p);
    float2 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);
    return lerp(
        lerp(Hash21(q + float2(0.0, 0.0)), Hash21(q + float2(1.0, 0.0)), f.x),
        lerp(Hash21(q + float2(0.0, 1.0)), Hash21(q + float2(1.0, 1.0)), f.x),
        f.y
    );
}

float Fbm(float2 p)
{
    float f = 0.0;
    f += 0.5 * Noise(p);
    f += 0.25 * Noise(p * 2.0);
    f += 0.125 * Noise(p * 4.0);
    f += 0.0625 * Noise(p * 8.0);
    return f;
}

float SdLeave(float2 p)
{
    float d = length(p * float2(1.0, 0.9) - float2(0.0, 0.02)) - 0.2 + 0.9 * abs(p.x);
    p.x = abs(p.x);

    float2 q = p;
    q -= float2(0.0, -0.2 + 32.0 * pow(p.x, 5.0));
    q = Rotate(q, Pi * 0.3 + 0.1 * Fbm(16.0 * p));
    q -= float2(0.0, 0.17);
    d = min(d, length(q) - 0.17 + 2.0 * abs(q.x));

    q = p;
    q -= float2(-0.07, -0.1 + 6.0 * pow(p.x, 3.0));
    q = Rotate(q, Pi * 0.7 + 0.1 * Fbm(24.0 * p));
    q -= float2(0.0, 0.13);
    d = min(d, length(q) - 0.13 + 2.5 * abs(q.x));

    q = p;
    float h = clamp(q.y, -0.377, 0.0);
    q -= float2(0.0, h);
    d = min(d + 0.2 * pow(Fbm(24.0 * p), 3.0), length(q) + 0.02 * h);
    return d;
}

float2 SphIntersect(float3 ro, float3 rd, float3 ce, float ra)
{
    float3 oc = ro - ce;
    float b = dot(oc, rd);
    float c = dot(oc, oc) - ra * ra;
    float h = b * b - c;
    if (h < 0.0) return float2(-1.0, -1.0);
    h = sqrt(h);
    return float2(-b - h, -b + h);
}

float2 Polar(float3 p)
{
    return float2(atan2(p.x, p.z), p.y);
}

float LeaveSphere(float3 ro, float3 rd, float3 ce, float2 an, float ra)
{
    float2 t = SphIntersect(ro, rd, ce, ra);
    if (t.y < 0.0) return 0.0;

    float3 oc = ro - ce;
    float3 p = oc + rd * t.x;
    float3 q = oc + rd * t.y;

    p.xz = Rotate(p.xz, an.x);
    q.xz = Rotate(q.xz, an.x);
    p.yz = Rotate(p.yz, an.y);
    q.yz = Rotate(q.yz, an.y);

    float di = max(ra * 2.0, 0.0001);
    float2 r = float2(ra, 1.0) / di;
    float2 pf = Polar(p) * r;
    float2 pb = Polar(q) * r;
    float lf = step(SdLeave(pf), 0.0);
    float lb = step(SdLeave(pb), 0.0);
    return lf * (1.0 - lb) + lb;
}

float Hash1(inout float n)
{
    n += 1.0;
    return frac(sin(n) * 2348.3241);
}

float2 Hash2(inout float n)
{
    n += 1.0;
    return frac(sin(n) * float2(2348.3241, 4591.5392));
}

float3 Hash3(inout float n)
{
    n += 1.0;
    return frac(sin(n) * float3(2348.3241, 4591.5392, 3412.4231));
}

float3 RenderLeaves(float3 ro, float3 rd, float3 bg)
{
    float3 col = bg;
    float2 an = Time * SpinRate;
    float count = clamp(LeafCount, 1.0, MaxLeafCount);

    [loop]
    for (int leaf = 0; leaf < MaxLeafCount; leaf++)
    {
        if (leaf < count)
        {
            float i = leaf / count;
            float n = i + 1.0;
            float2 p = (0.5 - frac(Hash2(n) + float2(ScrollX, ScrollY) * Time)) * Spread;
            an += Hash1(n) - 0.5;

            float3 rnd = Hash3(n);
            float3 mat = lerp(LeafGreenDark, LeafGreenLight, rnd.x);
            mat.r += (rnd.y - 0.5) * LeafHueJitter;
            mat.g += (rnd.z - 0.5) * LeafHueJitter * 0.5;
            mat = clamp(mat * max(LeafBrightness, 0.0), 0.0, 1.0);

            float3 ce = float3(p, 8.0 * (1.0 - i));
            float ra = max(LeafRadiusMin + LeafRadiusVariation * Hash1(n), 0.001);
            float alpha = LeaveSphere(ro, rd, ce, Pi * an, ra);
            col = lerp(col, mat, alpha);
        }
    }

    float3 lightDir = normalize(LightDir);
    float glare = pow(clamp(dot(rd, lightDir), 0.0, 1.0), max(GlarePower, 0.001));
    col += max(FogAmount, 0.0) * Fbm(12.0 * rd.xy + 0.1 * Time * float2(1.0, 3.0)) * (max(FogFloor, 0.0) + glare);
    return col;
}

float4 SpritePixelShader(VSOutput input) : COLOR0
{
    float2 viewport = max(ViewportSize, float2(1.0, 1.0));
    float2 uv = float2(input.TexCoord.x, 1.0 - input.TexCoord.y);
    float2 p = (uv * viewport - 0.5 * viewport) / viewport.y;
    float3 ro = float3(0.0, 0.0, -3.0);
    float3 rd = normalize(float3(p, 1.5));

    float3 tex = tex2D(TextureSampler, input.TexCoord).rgb;
    float hasTex = step(0.01, dot(tex, float3(1.0, 1.0, 1.0)));
    float3 sky = lerp(BackgroundSkyLow, BackgroundSkyHigh, uv.y);
    float3 bg = lerp(sky, tex, hasTex) * max(BackgroundBrightness, 0.0);

    float3 col = RenderLeaves(ro, rd, bg);
    return float4(saturate(col * 1.2), 1.0) * input.Color;
}

technique SpriteDrawing
{
    pass P0
    {
        VertexShader = compile vs_3_0 SpriteVertexShader();
        PixelShader = compile ps_3_0 SpritePixelShader();
    }
}
