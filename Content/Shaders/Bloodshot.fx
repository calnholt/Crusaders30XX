// Bloodshot post-process shader
// Converted from GLSL to HLSL for MonoGame

float4x4 MatrixTransform;
float2 ViewportSize;
float Time;

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
    
    // Oval shape
    const float OVAL_HORIZONTAL_SCALE = 0.5;  // Wider = more horizontal stretch
    const float OVAL_VERTICAL_SCALE = 0.9;    // Larger = more vertical stretch
    
    // Blur effect
    const int BLUR_SAMPLES = 8;               // Number of blur samples (more = smoother but slower)
    const float BLUR_RADIUS = 0.003;          // Blur radius at edges
    const float BLUR_START = 0.4;             // Where blur starts (distance from center)
    const float BLUR_END = 0.8;               // Where blur is maximum
    
    // Vein generation
    const int VEIN_ITERATIONS = 1;            // More = more detailed veins
    const float VEIN_BASE_FREQUENCY = 10.0;   // Higher = more vein branches
    const float VEIN_ANIMATION_SPEED = 0.01;  // Vein pulsing speed
    const float VEIN_RADIAL_FREQUENCY = 8.0;  // Radial vein pattern density
    const float VEIN_RADIAL_SCALE = 10.0;     // Radial vein spacing
    const float VEIN_TIME_SCALE = 0.5;        // Vein animation intensity
    
    // Vein appearance
    const float VEIN_EDGE_START = 0.2;        // Where veins start appearing
    const float VEIN_EDGE_END = 0.9;          // Where veins are fully visible
    const float VEIN_SHARPNESS_POW = 1.0;     // Higher = sharper veins
    const float VEIN_SHARPNESS_MULT = 2.0;    // Vein contrast multiplier
    const float VEIN_THRESHOLD_LOW = 0.3;     // Vein visibility threshold low
    const float VEIN_THRESHOLD_HIGH = 0.7;    // Vein visibility threshold high
    const float VEIN_COLOR_STRENGTH = 0.5;    // How visible veins are (0-1)
    
    // Redness effect
    const float REDNESS_INTENSITY = 0.2;      // Overall red tint strength
    const float RED_TINT_R = 1.0;             // Red channel for tint
    const float RED_TINT_G = 0.7;             // Green channel for tint
    const float RED_TINT_B = 0.7;             // Blue channel for tint
    
    // Blood color (vein color)
    const float3 BLOOD_COLOR = float3(1.0, 0.0, 0.0);
    
    // Clarity/blur
    const float CLARITY_START = 0.8;          // Where blur starts
    const float CLARITY_END = 0.2;            // Where view is clear
    const float BLUR_DARKNESS = 0.7;          // How dark blurred areas are
    
    // ===== SHADER CODE =====
    
    // Normalize coordinates to -1 to 1
    float2 fragCoord = input.TexCoord * ViewportSize;
    float2 uv = (fragCoord - 0.5 * ViewportSize) / ViewportSize.y;
    
    // Create oval shape by scaling UV differently on each axis
    float2 ovalUV = uv;
    ovalUV.x *= OVAL_HORIZONTAL_SCALE;
    ovalUV.y *= OVAL_VERTICAL_SCALE;
    
    // Distance from center using oval coordinates
    float dist = length(ovalUV);
    
    // Calculate blur amount based on distance from center
    float blurAmount = smoothstep(BLUR_START, BLUR_END, dist);
    
    // Get scene UV
    float2 sceneUV = input.TexCoord;
    
    // Sample scene with blur
    float3 sceneColor = float3(0.0, 0.0, 0.0);
    
    if (blurAmount > 0.01) {
        // Apply radial blur at edges
        float totalWeight = 0.0;
        for(int i = 0; i < BLUR_SAMPLES; i++) {
            float angle = float(i) * 6.28318 / float(BLUR_SAMPLES);
            float2 offset = float2(cos(angle), sin(angle)) * BLUR_RADIUS * blurAmount;
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
    float frequency = VEIN_BASE_FREQUENCY;
    
    for(int j = 0; j < VEIN_ITERATIONS; j++) {
        // Create branching vein pattern
        float2 p = uv * frequency + Time * VEIN_ANIMATION_SPEED;
        float noise1 = frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
        float noise2 = frac(sin(dot(p + float2(1.0, 1.0), float2(45.123, 67.891))) * 23421.631);
        
        // Create vein-like structures using oval distance
        float veinAngle = atan2(ovalUV.y, ovalUV.x) + noise1 * 6.28;
        float radial = abs(sin(veinAngle * VEIN_RADIAL_FREQUENCY + dist * VEIN_RADIAL_SCALE - Time * VEIN_TIME_SCALE));
        
        veins += radial * amplitude * noise2;
        
        amplitude *= 0.5;
        frequency *= 2.0;
    }
    
    // Make veins appear more at edges (using oval distance)
    float edgeFactor = smoothstep(VEIN_EDGE_START, VEIN_EDGE_END, dist);
    veins *= edgeFactor;
    
    // Sharpen veins
    veins = pow(veins, VEIN_SHARPNESS_POW) * VEIN_SHARPNESS_MULT;
    veins = smoothstep(VEIN_THRESHOLD_LOW, VEIN_THRESHOLD_HIGH, veins);
    
    // Redness increases at edges
    float redness = edgeFactor * REDNESS_INTENSITY;
    
    // Combine effects
    float3 color = sceneColor;
    color = lerp(color, color * float3(RED_TINT_R, RED_TINT_G, RED_TINT_B), redness);
    color = lerp(color, BLOOD_COLOR, veins * VEIN_COLOR_STRENGTH);
    
    // Add slight blur/fuzziness at edges (oval-based)
    float clarity = smoothstep(CLARITY_START, CLARITY_END, dist);
    color = lerp(color * BLUR_DARKNESS, color, clarity);
    
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
