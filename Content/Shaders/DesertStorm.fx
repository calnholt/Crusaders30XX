// Desert sandstorm post-process, converted from DesertStorm.glsl.

float4x4 MatrixTransform;
float2 ViewportSize;
float Time;

float BaseScale = 1.5;
float Lacunarity = 2.0;
float Persistence = 0.5;
float WarpStrength = 3.5;
float DensityRemapLow = 0.2;
float DensityRemapHigh = 0.8;

float DriftSpeed = 0.025;
float DriftVertical = 0.006;
float WarpDriftA = 0.018;
float WarpDriftB = 0.012;
float MorphSpeed = 0.008;

float3 ShadowColor = float3(0.55, 0.47, 0.37);
float3 MidColor = float3(0.70, 0.62, 0.51);
float3 HighlightColor = float3(0.82, 0.75, 0.64);
float3 BrightColor = float3(0.89, 0.82, 0.71);
float VerticalGradient = 0.08;

float DustBase = 0.55;
float DustDensity = 0.45;
float3 SceneTint = float3(0.90, 0.82, 0.68);
float SceneTintStrength = 0.40;

float GrainIntensity = 0.10;
float GrainFineness = 1.0;
float VignetteAmount = 0.20;

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
    float3 p3 = frac(p.xyx * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

float ValueNoise(float2 p)
{
    float2 cell = floor(p);
    float2 blend = frac(p);
    blend = blend * blend * (3.0 - 2.0 * blend);

    return lerp(
        lerp(Hash21(cell), Hash21(cell + float2(1.0, 0.0)), blend.x),
        lerp(Hash21(cell + float2(0.0, 1.0)), Hash21(cell + float2(1.0, 1.0)), blend.x),
        blend.y
    );
}

float2 RotateFbmDomain(float2 p)
{
    return float2(
        0.80 * p.x - 0.60 * p.y,
        0.60 * p.x + 0.80 * p.y
    );
}

float Fbm(float2 p)
{
    float value = 0.0;
    float amplitude = 0.5;

    [unroll]
    for (int i = 0; i < 5; i++)
    {
        value += amplitude * ValueNoise(p);
        p = RotateFbmDomain(p) * max(Lacunarity, 0.001);
        amplitude *= saturate(Persistence);
    }

    return value;
}

float CloudDensity(float2 p, float time)
{
    float2 q;
    q.x = Fbm(p + float2(0.00, 0.00) + time * float2(-WarpDriftA, MorphSpeed));
    q.y = Fbm(p + float2(5.20, 1.30) + time * float2(-WarpDriftA, -MorphSpeed * 0.7));

    float2 r;
    r.x = Fbm(p + WarpStrength * q + float2(1.70, 9.20) + time * float2(-WarpDriftB, MorphSpeed * 0.5));
    r.y = Fbm(p + WarpStrength * q + float2(8.30, 2.80) + time * float2(-WarpDriftB, -MorphSpeed * 0.3));

    return Fbm(p + WarpStrength * r + time * float2(-DriftSpeed, DriftVertical));
}

float4 SpritePixelShader(VSOutput input) : COLOR0
{
    float2 sceneUv = input.TexCoord;
    float2 uv = float2(sceneUv.x, 1.0 - sceneUv.y);
    float2 viewport = max(ViewportSize, float2(1.0, 1.0));
    float aspect = viewport.x / viewport.y;
    float2 p = float2(uv.x * aspect, uv.y) * max(BaseScale, 0.001);

    float3 scene = tex2D(TextureSampler, sceneUv).rgb;
    scene = lerp(scene, scene * SceneTint, saturate(SceneTintStrength));

    float density = CloudDensity(p, Time);
    float densityLow = min(DensityRemapLow, DensityRemapHigh - 0.0001);
    float densityHigh = max(DensityRemapHigh, densityLow + 0.0001);
    density = smoothstep(densityLow, densityHigh, density);

    float3 sandColor = ShadowColor;
    sandColor = lerp(sandColor, MidColor, smoothstep(0.00, 0.35, density));
    sandColor = lerp(sandColor, HighlightColor, smoothstep(0.35, 0.65, density));
    sandColor = lerp(sandColor, BrightColor, smoothstep(0.65, 1.00, density));

    float fog = saturate(DustBase + density * DustDensity);
    float3 color = lerp(scene, sandColor, fog);
    color *= 1.0 + VerticalGradient * (uv.y - 0.5);

    float2 fragCoord = uv * viewport;
    float grainScale = max(GrainFineness, 0.001);
    float grain = Hash21(floor(fragCoord * grainScale) + frac(Time * 7.13) * 300.0);
    color += (grain - 0.5) * GrainIntensity;

    float2 vignetteCoord = uv - 0.5;
    float vignette = 1.0 - VignetteAmount * dot(vignetteCoord, vignetteCoord) * 2.5;
    color *= max(vignette, 0.0);

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
