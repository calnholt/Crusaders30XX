// Achievement Background - Turbulent Noise Effect (SpriteBatch-compatible)
// Converted from ShaderToy GLSL to HLSL

float4x4 MatrixTransform;
float2 ViewportSize;          // in pixels

// Time and animation
float iTime = 0.0;            // animation time in seconds

// Noise parameters
float NoiseScale = 4.0;       // initial noise coordinate scale
float TimeSpeed = 0.25;       // Z-axis time scroll speed

// Turbulence parameters
float TurbInitialInc = 0.75;       // turbulence initial increment
float TurbInitialDiv = 1.75;       // turbulence initial divisor
float TurbOctaveMultiplier = 2.13; // octave scale multiplier
float TurbIncDecay = 0.5;          // increment decay per octave

// UV manipulation
float UVDistortFactor = 0.2;  // radial UV distortion strength
float RotationSpeed = 0.5;    // UV rotation time multiplier
float RayDepth = 5.0;         // ray direction Z depth

// Color parameters
float ColorBrightness = 1.0;  // overall brightness multiplier
float3 TintColor = float3(1.0, 1.0, 1.0);  // RGB tint multiplier
float ChannelWeightR = 1.0;   // red channel weight
float ChannelWeightG = 1.0;   // green channel weight  
float ChannelWeightB = 1.0;   // blue channel weight

// Vignette parameters
float VignetteStrength = 0.0; // 0 = no vignette, 1 = full vignette
float VignetteRadius = 0.8;   // distance from center where vignette starts

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

// --- Noise functions (ported from GLSL) ---

// Hash-based 3D noise
float noise3D(float3 p)
{
    p = floor(p);
    p = frac(p * float3(283.343, 251.691, 634.127));
    p += dot(p, p + 23.453);
    return frac(p.x * p.y);
}

// Trilinear interpolated noise blend
float noiseBlend(float3 p)
{
    float3 pf = floor(p);
    float3 ff = frac(p);
    
    // Sample at 8 corners of the unit cube
    float n000 = noise3D(pf + float3(0, 0, 0));
    float n100 = noise3D(pf + float3(1, 0, 0));
    float n010 = noise3D(pf + float3(0, 1, 0));
    float n110 = noise3D(pf + float3(1, 1, 0));
    float n001 = noise3D(pf + float3(0, 0, 1));
    float n101 = noise3D(pf + float3(1, 0, 1));
    float n011 = noise3D(pf + float3(0, 1, 1));
    float n111 = noise3D(pf + float3(1, 1, 1));
    
    // Trilinear interpolation
    float nx00 = lerp(n000, n100, ff.x);
    float nx10 = lerp(n010, n110, ff.x);
    float nx01 = lerp(n001, n101, ff.x);
    float nx11 = lerp(n011, n111, ff.x);
    
    float nxy0 = lerp(nx00, nx10, ff.y);
    float nxy1 = lerp(nx01, nx11, ff.y);
    
    return lerp(nxy0, nxy1, ff.z);
}

// Turbulence function with multiple octaves
float turb(float3 p)
{
    p *= NoiseScale;
    float3 dp = float3(p.xy, p.z + iTime * TimeSpeed);
    float inc = TurbInitialInc;
    float divVal = TurbInitialDiv;
    float3 octs = dp * TurbOctaveMultiplier;
    float n = noiseBlend(dp);
    
    // Unrolled loop for 5 octaves (shader model 3.0 compatibility)
    [unroll]
    for (int i = 0; i < 5; i++)
    {
        float ns = noiseBlend(octs);
        n += inc * ns;
        
        float3 offset = float3(ns, noiseBlend(octs + float3(n, 0.0, 0.1)), 0.0);
        octs *= 2.0 + offset;
        inc *= TurbIncDecay * n;
        divVal += inc;
    }
    
    float v = n / divVal;
    // Radial falloff from a point in space
    v *= 1.0 - max(0.0, 1.2 - length(float3(0.5, 0.0, 6.0) - p));
    return v;
}

float4 SpritePixelShader(VSOutput input) : COLOR0
{
    // Convert texture coordinates to normalized device coordinates (-1 to 1, aspect corrected)
    float2 uv = (2.0 * input.TexCoord - float2(1.0, 1.0));
    float aspect = ViewportSize.x / ViewportSize.y;
    uv.x *= aspect;
    
    // Apply radial UV distortion
    float uvLen = length(uv);
    uv *= 1.0 + UVDistortFactor * uvLen;
    float uvLenInv = 1.0 - uvLen;
    
    // Calculate rotation based on time and position
    float tt = RotationSpeed * iTime + (0.3 - 0.3 * uvLenInv * uvLenInv);
    float sinTT = sin(tt);
    float cosTT = cos(tt);
    
    // Apply rotation to UV
    float2 rotatedUV = float2(
        uv.x * sinTT + uv.y * cosTT,
        uv.x * cosTT - uv.y * sinTT
    );
    
    // Set up ray origin and direction (ignoring mouse, using center)
    float3 ro = float3(0.0, 0.0, -1.0);
    float3 rd = normalize(float3(rotatedUV, RayDepth) - ro);
    
    // Apply time-based Z offset to ray direction
    rd.z += tt * 0.01;
    
    // Calculate turbulence and accumulate color
    float3 col = float3(0, 0, 0);
    float nv = turb(rd);
    
    // Unrolled color accumulation loop (5 iterations, step 0.2)
    [unroll]
    for (int i = 0; i < 5; i++)
    {
        float t = (float)i * 0.2;
        nv *= 0.5;
        nv = turb(float3(rd.xy, rd.z + t));
        
        // Original color formula: vec3(nv, nv*nv*(3.-2.*nv), nv*nv)
        float smoothNV = nv * nv * (3.0 - 2.0 * nv);
        float3 layerCol = float3(
            nv * ChannelWeightR,
            smoothNV * ChannelWeightG,
            nv * nv * ChannelWeightB
        );
        col += (1.5 - t) * layerCol;
    }
    col /= 5.0;
    
    // Apply brightness and tint
    col *= ColorBrightness;
    col *= TintColor;
    
    // Apply vignette if enabled
    if (VignetteStrength > 0.001)
    {
        float2 uvNorm = input.TexCoord * 2.0 - 1.0;
        float vignette = 1.0 - smoothstep(VignetteRadius, 1.0, length(uvNorm));
        col = lerp(col, col * vignette, VignetteStrength);
    }
    
    return float4(saturate(col), 1.0) * input.Color;
}

technique SpriteDrawing
{
    pass P0
    {
        VertexShader = compile vs_3_0 SpriteVertexShader();
        PixelShader  = compile ps_3_0 SpritePixelShader();
    }
}
