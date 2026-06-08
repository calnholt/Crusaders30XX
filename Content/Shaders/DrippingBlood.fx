// Animated dripping blood background.
// Converted from the ShaderToy-style DrippingBlood.glsl source for MonoGame SpriteBatch.

float4x4 MatrixTransform;
float2 ViewportSize;
float Time;

int DripCount = 20;
int LayerCount = 1;

float SpeedMin = 0.06;
float SpeedMax = 0.15;
float RestMin = 1.5;
float RestMax = 5.0;

float FadePower = 1.8;
float OffscreenFade = 0.35;

float WidthMin = 0.003;
float WidthMax = 0.016;
float TaperAtTop = 0.65;
float TipRoundness = 1.0;
float WobbleAmount = 0.0025;
float WobbleFrequency = 14.0;
float ThicknessVariation = 0.35;

float3 BackgroundColor = float3(0.05, 0.003, 0.003);
float3 DripColor = float3(0.70, 0.02, 0.02);
float VignetteStrength = 0.0;

#define MAX_DRIPS 25
#define MAX_LAYERS 3

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

float Hash21(float2 p)
{
    float3 p3 = frac(float3(p.x, p.y, p.x) * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

float HashScalar(float value)
{
    return frac(sin(value * 12.9898 + 78.233) * 43758.5453);
}

float ValueNoise(float2 p)
{
    float2 cell = floor(p);
    float2 local = frac(p);
    local = local * local * (3.0 - 2.0 * local);

    return lerp(
        lerp(Hash21(cell), Hash21(cell + float2(1.0, 0.0)), local.x),
        lerp(Hash21(cell + float2(0.0, 1.0)), Hash21(cell + float2(1.0, 1.0)), local.x),
        local.y);
}

float4 SpritePixelShader(VSOutput input) : COLOR0
{
    // ShaderToy coordinates start at the bottom-left; SpriteBatch UVs start at the top-left.
    float2 uv = float2(input.TexCoord.x, 1.0 - input.TexCoord.y);
    float pixelWidth = 1.0 / max(ViewportSize.y, 1.0);
    float3 color = BackgroundColor;

    [loop]
    for (int layer = 0; layer < MAX_LAYERS; layer++)
    {
        if (layer >= LayerCount)
        {
            break;
        }

        float layerIndex = (float)layer;
        float layerSeed = layerIndex * 173.7;
        float layerScale = 1.0 - layerIndex * 0.10;

        [loop]
        for (int drip = 0; drip < MAX_DRIPS; drip++)
        {
            if (drip >= DripCount)
            {
                break;
            }

            float seed = (float)drip + layerSeed;
            float dripX = HashScalar(seed + 0.13);
            float speed = lerp(SpeedMin, SpeedMax, HashScalar(seed + 1.37));
            float width = lerp(WidthMin, WidthMax, HashScalar(seed + 2.71)) * layerScale;
            float phase = HashScalar(seed + 3.91);
            float startY = lerp(1.00, 1.08, HashScalar(seed + 4.23));
            float rest = lerp(RestMin, RestMax, HashScalar(seed + 5.67));

            float travelDistance = startY + OffscreenFade + 0.1;
            float travelTime = travelDistance / max(speed, 0.0001);
            float period = travelTime + rest;
            float cycleTime = fmod(Time + phase * period, period);
            float headY = startY - cycleTime * speed;

            float wobble = (ValueNoise(float2(uv.y * WobbleFrequency + seed * 3.1, seed)) - 0.5)
                * 2.0 * WobbleAmount;
            float horizontalDistance = abs(uv.x - dripX - wobble);

            float aboveHead = step(headY, uv.y);
            float belowOrigin = step(uv.y, startY);
            float inTrail = aboveHead * belowOrigin;
            float trailLength = max(startY - headY, 0.001);
            float trailPosition = saturate((uv.y - headY) / trailLength);

            float thicknessNoise = 1.0 - ThicknessVariation
                + ThicknessVariation * ValueNoise(float2(uv.y * 25.0 + seed * 7.0, seed * 0.3));
            float trailWidth = max(
                width * lerp(1.0, TaperAtTop, sqrt(trailPosition)) * thicknessNoise,
                0.00001);

            float belowHead = max(headY - uv.y, 0.0);
            float verticalRadius = trailWidth * TipRoundness + 0.00001;
            float ellipseDistance = length(float2(
                horizontalDistance / trailWidth,
                belowHead / verticalRadius));
            float feather = pixelWidth / trailWidth;
            float tipMask = smoothstep(1.0 + feather, 1.0 - feather, ellipseDistance);

            float bodyMask = smoothstep(
                trailWidth + pixelWidth,
                trailWidth - pixelWidth,
                horizontalDistance) * inTrail;
            float trailMask = max(bodyMask, tipMask);

            float trailFade = pow(max(1.0 - trailPosition, 0.0), FadePower);
            float offscreenMask = smoothstep(-OffscreenFade, 0.0, headY);
            trailMask *= trailFade * offscreenMask;

            color = lerp(color, DripColor, trailMask);
        }
    }

    float2 vignettePosition = uv - 0.5;
    color *= 1.0 - VignetteStrength * dot(vignettePosition, vignettePosition) * 2.5;

    return float4(saturate(color), 1.0) * input.Color;
}

technique SpriteDrawing
{
    pass P0
    {
        VertexShader = compile vs_3_0 SpriteVertexShader();
        PixelShader = compile ps_3_0 SpritePixelShader();
    }
}
