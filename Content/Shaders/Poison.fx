// Poison damage post-process effect (SpriteBatch-compatible, Reach profile)
// Converted from Shadertoy GLSL to HLSL for MonoGame/XNA Effects

float4x4 MatrixTransform;
float2 ViewportSize;          // in pixels

// Time parameter (normalized 0..1)
float t = 0.0;                // normalized time 0..1

// Fade curve parameters
float AttackDuration = 0.1;   // fast fade-in phase duration (0.1 = 10% of effect)
float DecayRate = 4.0;        // exponential decay speed

// Screen shake parameters
float ShakeFrequency = 50.0;  // shake oscillation speed
float ShakeAmplitude = 0.006; // shake displacement amount

// Radial distortion wave parameters
float WaveFrequency = 20.0;   // radial wave density
float WaveSpeed = 25.0;       // wave movement speed
float WaveAmplitude = 0.008;  // wave distortion strength

// Vignette parameters
float VignetteStart = 0.2;    // vignette inner radius
float VignetteIntensity = 0.8; // vignette darkening amount

// Color grading parameters
float3 PoisonTint = float3(0.15, 0.9, 0.3);  // poison green color
float PoisonMixAmount = 0.4;  // poison tint blend strength
float DesaturationAmount = 0.5; // grayscale blend strength

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
    float2 uv = input.TexCoord;
    
    // Compute fade intensity with sharp attack and slow decay
    float fadeIntensity = 0.0;
    if (t > 0.0)
    {
        if (t < AttackDuration)
        {
            // Fast fade in over attack duration
            fadeIntensity = t / max(AttackDuration, 0.001);
        }
        else
        {
            // Exponential decay over remaining time
            float decayTime = (t - AttackDuration) / max(1.0 - AttackDuration, 0.001);
            fadeIntensity = exp(-decayTime * DecayRate);
        }
    }
    
    // Early out if effect is negligible
    float4 color = tex2D(TextureSampler, uv);
    if (fadeIntensity < 0.01)
    {
        return color * input.Color;
    }
    
    // Screen center
    float2 center = float2(0.5, 0.5);
    float2 toCenter = center - uv;
    
    // Screen shake - oscillating offset
    float2 shakeUV = uv + sin(t * ShakeFrequency) * fadeIntensity * ShakeAmplitude;
    
    // Radial distortion wave
    float dist = length(uv - center);
    float wave = sin(dist * WaveFrequency - t * WaveSpeed) * fadeIntensity * WaveAmplitude;
    float2 distortedUV = shakeUV + normalize(toCenter + float2(0.00001, 0.00001)) * wave;
    
    // Sample with distortion
    color = tex2D(TextureSampler, distortedUV);
    
    // Vignette effect
    float vignette = 1.0 - smoothstep(VignetteStart, 1.0, dist) * fadeIntensity * VignetteIntensity;
    color.rgb *= vignette;
    
    // Poison overlay tint - fades in and out with fadeIntensity
    color.rgb = lerp(color.rgb, PoisonTint, fadeIntensity * PoisonMixAmount);
    
    // Desaturate
    float gray = dot(color.rgb, float3(0.3, 0.59, 0.11));
    color.rgb = lerp(color.rgb, float3(gray, gray, gray), fadeIntensity * DesaturationAmount);
    
    return float4(color.rgb, color.a) * input.Color;
}

technique SpriteDrawing
{
    pass P0
    {
        VertexShader = compile vs_3_0 SpriteVertexShader();
        PixelShader  = compile ps_3_0 SpritePixelShader();
    }
}

