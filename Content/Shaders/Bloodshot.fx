// Bloodshot post-process shader
// Converted from GLSL to HLSL for MonoGame

float4x4 MatrixTransform;
float2 ViewportSize;
float Time;

// Oval shape
float OvalHorizontalScale;  // Wider = more horizontal stretch
float OvalVerticalScale;    // Larger = more vertical stretch

// Blur effect
float BlurRadius;           // Blur radius at edges
float BlurStart;            // Where blur starts (distance from center)
float BlurEnd;              // Where blur is maximum

// Vein generation
float VeinBaseFrequency;    // Higher = more vein branches
float VeinAnimationSpeed;   // Vein pulsing speed
float VeinRadialFrequency;  // Radial vein pattern density
float VeinRadialScale;      // Radial vein spacing
float VeinTimeScale;        // Vein animation intensity

// Vein appearance
float VeinEdgeStart;        // Where veins start appearing
float VeinEdgeEnd;          // Where veins are fully visible
float VeinSharpnessPow;     // Higher = sharper veins
float VeinSharpnessMult;    // Vein contrast multiplier
float VeinThresholdLow;     // Vein visibility threshold low
float VeinThresholdHigh;    // Vein visibility threshold high
float VeinColorStrength;    // How visible veins are (0-1)

// Redness effect
float RednessIntensity;     // Overall red tint strength
float RedTintR;             // Red channel for tint
float RedTintG;             // Green channel for tint
float RedTintB;             // Blue channel for tint

// Blood color (vein color)
float3 BloodColor;

// Clarity/blur
float ClarityStart;         // Where blur starts
float ClarityEnd;           // Where view is clear
float BlurDarkness;         // How dark blurred areas are

texture Texture : register(t0);
sampler2D TextureSampler : register(s0) = sampler_state
{
    Texture = <Texture>;
    MinFilter = Linear; MagFilter = Linear; MipFilter = Linear;
    AddressU = Clamp; AddressV = Clamp;
};

struct VSInput { float4 Position : POSITION0; float4 Color : COLOR0; float2 TexCoord : TEXCOORD0; };
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
    // ===== CONSTANTS =====
    const int BLUR_SAMPLES = 8;               // Number of blur samples (more = smoother but slower)
    const int VEIN_ITERATIONS = 1;            // More = more detailed veins
    
    // ===== SHADER CODE =====
    
    // Normalize coordinates to -1 to 1
    float2 fragCoord = input.TexCoord * ViewportSize;
    float2 uv = (fragCoord - 0.5 * ViewportSize) / ViewportSize.y;
    
    // Create oval shape by scaling UV differently on each axis
    float2 ovalUV = uv;
    ovalUV.x *= OvalHorizontalScale;
    ovalUV.y *= OvalVerticalScale;
    
    // Distance from center using oval coordinates
    float dist = length(ovalUV);
    
    // Calculate blur amount based on distance from center
    float blurAmount = smoothstep(BlurStart, BlurEnd, dist);
    
    // Get scene UV
    float2 sceneUV = input.TexCoord;
    
    // Sample scene with blur
    float3 sceneColor = float3(0.0, 0.0, 0.0);
    
    if (blurAmount > 0.01) {
        // Apply radial blur at edges
        float totalWeight = 0.0;
        for(int i = 0; i < BLUR_SAMPLES; i++) {
            float angle = float(i) * 6.28318 / float(BLUR_SAMPLES);
            float2 offset = float2(cos(angle), sin(angle)) * BlurRadius * blurAmount;
            float2 sampleUV = sceneUV + offset;
            
            // Keep samples in bounds
            if (sampleUV.x >= 0.0 && sampleUV.x <= 1.0 && sampleUV.y >= 0.0 && sampleUV.y <= 1.0) {
                sceneColor += tex2Dlod(TextureSampler, float4(sampleUV, 0, 0)).rgb;
                totalWeight += 1.0;
            }
        }
        sceneColor /= max(totalWeight, 1.0);
    } else {
        // No blur in center
        sceneColor = tex2D(TextureSampler, sceneUV).rgb;
    }
    
    // Create vein pattern using fractal noise
    float veins = 0.0;
    float amplitude = 1.0;
    float frequency = VeinBaseFrequency;
    
    for(int j = 0; j < VEIN_ITERATIONS; j++) {
        // Create branching vein pattern
        float2 p = uv * frequency + Time * VeinAnimationSpeed;
        float noise1 = frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
        float noise2 = frac(sin(dot(p + float2(1.0, 1.0), float2(45.123, 67.891))) * 23421.631);
        
        // Create vein-like structures using oval distance
        float veinAngle = atan2(ovalUV.y, ovalUV.x) + noise1 * 6.28;
        float radial = abs(sin(veinAngle * VeinRadialFrequency + dist * VeinRadialScale - Time * VeinTimeScale));
        
        veins += radial * amplitude * noise2;
        
        amplitude *= 0.5;
        frequency *= 2.0;
    }
    
    // Make veins appear more at edges (using oval distance)
    float edgeFactor = smoothstep(VeinEdgeStart, VeinEdgeEnd, dist);
    veins *= edgeFactor;
    
    // Sharpen veins
    veins = pow(veins, VeinSharpnessPow) * VeinSharpnessMult;
    veins = smoothstep(VeinThresholdLow, VeinThresholdHigh, veins);
    
    // Redness increases at edges
    float redness = edgeFactor * RednessIntensity;
    
    // Combine effects
    float3 color = sceneColor;
    color = lerp(color, color * float3(RedTintR, RedTintG, RedTintB), redness);
    color = lerp(color, BloodColor, veins * VeinColorStrength);
    
    // Add slight blur/fuzziness at edges (oval-based)
    float clarity = smoothstep(ClarityStart, ClarityEnd, dist);
    color = lerp(color * BlurDarkness, color, clarity);
    
    return float4(color, 1.0) * input.Color;
}

technique SpriteDrawing
{
    pass P0
    {
        VertexShader = compile vs_3_0 SpriteVertexShader();
        PixelShader  = compile ps_3_0 SpritePixelShader();
    }
}
