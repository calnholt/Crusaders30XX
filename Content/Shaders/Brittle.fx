// Brittle card crumble effect
// Converted from Shadertoy GLSL to HLSL for MonoGame SpriteBatch.

float4x4 MatrixTransform;
float2 iResolution; // viewport width/height in pixels
float iTime;        // seconds

// Chunk shapes
float GRID_MIN = 18.0;
float GRID_MAX = 18.0;
float GRID_SEED = 12.0;
float CELL_JITTER = 0.9;
float SEAM_WIDTH = 0.00;

// How many chunks fall
float FALL_FRACTION = 0.15;

// Crumble timing
float PERIOD_MIN = 2.5;
float PERIOD_MAX = 9.0;
float ATTACH_END = 0.45;
float FALL_END = 0.80;

// Falling motion
float MAX_FALL = 12.0;
float MAX_DRIFT = 1.2;
float FALL_GRAVITY = 2.0;
float FALL_ROT = 2.2;

// Look
float CHUNK_SIZE_PX = 22.0;
float MASK_THRESHOLD = 0.02;
float DEBRIS_DARK = 0.95;
float3 EDGE_GLOW = float3(1.0, 0.85, 0.45);
float EDGE_GLOW_AMT = 0.6;
float HOLE_DARKEN = 1.0;

#define SEARCH_CELLS 9
#define DRIFT_CELLS 2

texture Texture : register(t0);
sampler2D TextureSampler : register(s0) = sampler_state
{
    Texture = <Texture>;
    MinFilter = Linear; MagFilter = Linear; MipFilter = Linear;
    AddressU = Clamp; AddressV = Clamp;
};

texture BackgroundTexture;
sampler2D BackgroundSampler = sampler_state
{
    Texture = <BackgroundTexture>;
    MinFilter = Linear; MagFilter = Linear; MipFilter = Linear;
    AddressU = Clamp; AddressV = Clamp;
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

struct Chunk
{
    float fall;
    float drift;
    float alpha;
    float home;
    float angle;
};

VSOutput SpriteVertexShader(VSInput input)
{
    VSOutput o;
    o.Position = mul(input.Position, MatrixTransform);
    o.Color = input.Color;
    o.TexCoord = input.TexCoord;
    return o;
}

float Hash21(float2 p)
{
    float3 p3 = frac(float3(p.x, p.y, p.x) * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

float2 Hash22(float2 p)
{
    float3 p3 = frac(float3(p.x, p.y, p.x) * float3(0.1031, 0.1030, 0.0973));
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.xx + p3.yz) * p3.zy);
}

float2 FeatPt(float2 c)
{
    return c + 0.5 + (Hash22(c) - 0.5) * CELL_JITTER;
}

float2 Voro(float2 q, out float d1, out float d2)
{
    float2 ip = floor(q);
    d1 = 1e9;
    d2 = 1e9;
    float2 id = ip;

    for (int dy = -1; dy <= 1; dy++)
    {
        for (int dx = -1; dx <= 1; dx++)
        {
            float2 c = ip + float2((float)dx, (float)dy);
            float2 f = FeatPt(c);
            float d = dot(q - f, q - f);

            if (d < d1)
            {
                d2 = d1;
                d1 = d;
                id = c;
            }
            else if (d < d2)
            {
                d2 = d;
            }
        }
    }

    d1 = sqrt(d1);
    d2 = sqrt(d2);
    return id;
}

float2 ToTextureUV(float2 shaderUV)
{
    return float2(shaderUV.x, 1.0 - shaderUV.y);
}

float3 SampleCurrent(float2 shaderUV)
{
    return tex2D(TextureSampler, ToTextureUV(shaderUV)).rgb;
}

float3 SampleBackground(float2 shaderUV)
{
    return tex2D(BackgroundSampler, ToTextureUV(shaderUV)).rgb;
}

float ChangedMask(float2 shaderUV)
{
    float3 current = SampleCurrent(shaderUV);
    float3 background = SampleBackground(shaderUV);
    float delta = max(max(abs(current.r - background.r), abs(current.g - background.g)), abs(current.b - background.b));
    return smoothstep(MASK_THRESHOLD, MASK_THRESHOLD * 2.0, delta);
}

float CellInterior(float distanceToEdge)
{
    return smoothstep(0.0, max(SEAM_WIDTH, 0.0001), distanceToEdge);
}

float3 SampleCard(float2 shaderUV)
{
    return SampleCurrent(shaderUV);
}

Chunk MakeChunk(float fall, float drift, float alpha, float home, float angle)
{
    Chunk c;
    c.fall = fall;
    c.drift = drift;
    c.alpha = alpha;
    c.home = home;
    c.angle = angle;
    return c;
}

Chunk ChunkLife(float2 cell)
{
    Chunk c = MakeChunk(0.0, 0.0, 0.0, 1.0, 0.0);

    float period = lerp(PERIOD_MIN, PERIOD_MAX, Hash21(cell + 5.7));
    float tl = iTime / period + Hash21(cell);
    float cyc = floor(tl);
    float u = frac(tl);

    if (Hash21(cell + float2(cyc, 0.7) * 1.7) > FALL_FRACTION)
    {
        return c;
    }

    float dirX = (Hash21(cell + float2(cyc, 7.3)) - 0.5) * 2.0;
    float rotV = (Hash21(cell + float2(cyc, 11.7)) - 0.5) * 2.0 * FALL_ROT;

    if (u < ATTACH_END)
    {
        return c;
    }
    else if (u < FALL_END)
    {
        float fp = (u - ATTACH_END) / (FALL_END - ATTACH_END);
        c.fall = pow(saturate(fp), FALL_GRAVITY) * MAX_FALL;
        c.drift = dirX * MAX_DRIFT * fp;
        c.alpha = 1.0 - fp;
        c.home = 0.0;
        c.angle = rotV * fp;
        return c;
    }

    float rp = (u - FALL_END) / (1.0 - FALL_END);
    c.home = rp;
    return c;
}

float4 SpritePixelShader(VSOutput input) : COLOR0
{
    float2 resolution = max(iResolution, float2(1.0, 1.0));
    float2 uv = float2(input.TexCoord.x, 1.0 - input.TexCoord.y);
    float chunkSize = max(CHUNK_SIZE_PX, 1.0);
    float2 gridDim = max(resolution / chunkSize, float2(1.0, 1.0));
    float3 col = SampleCurrent(uv);

    float gRand = frac(sin(GRID_SEED * 12.9898 + 78.233) * 43758.5453);
    float gridJitter = lerp(GRID_MIN, GRID_MAX, gRand) / max(GRID_MIN, 1.0);
    gridDim *= max(gridJitter, 0.001);
    float2 p = uv * gridDim;
    float localMask = ChangedMask(uv);

    if (localMask > 0.001)
    {
        float hd1;
        float hd2;
        float2 homeC = Voro(p, hd1, hd2);
        Chunk life = ChunkLife(homeC);
        float3 card = SampleCard(uv);
        float3 background = SampleBackground(uv) * HOLE_DARKEN;
        float seam = CellInterior(hd2 - hd1);
        float show = life.home * seam;
        col = lerp(background, card, show * localMask);
    }

    for (int ky = 0; ky <= SEARCH_CELLS; ky++)
    {
        for (int kx = -DRIFT_CELLS; kx <= DRIFT_CELLS; kx++)
        {
            float2 c = floor(p) + float2((float)kx, (float)ky);
            Chunk life = ChunkLife(c);

            if (life.alpha <= 0.001)
            {
                continue;
            }

            float2 rest = FeatPt(c);
            float2 centerNow = rest + float2(life.drift, -life.fall);
            float cs = cos(-life.angle);
            float sn = sin(-life.angle);
            float2 rel = p - centerNow;
            float2 q = float2(cs * rel.x - sn * rel.y, sn * rel.x + cs * rel.y) + rest;

            float qd1;
            float qd2;
            float2 nc = Voro(q, qd1, qd2);
            if (any(abs(nc - c) > 0.001))
            {
                continue;
            }

            float seam = CellInterior(qd2 - qd1);
            if (seam <= 0.0)
            {
                continue;
            }

            float2 srcUV = q / gridDim;
            if (srcUV.x < 0.0 || srcUV.x > 1.0 || srcUV.y < 0.0 || srcUV.y > 1.0)
            {
                continue;
            }

            float srcMask = ChangedMask(srcUV);
            if (srcMask <= 0.001)
            {
                continue;
            }

            float3 chunkColor = SampleCard(srcUV);
            float fallFrac = saturate(life.fall / MAX_FALL);
            chunkColor *= lerp(1.0, DEBRIS_DARK, fallFrac);
            float edge = 1.0 - smoothstep(0.0, SEAM_WIDTH + 0.15, qd2 - qd1);
            chunkColor += EDGE_GLOW * EDGE_GLOW_AMT * edge * life.alpha;
            col = lerp(col, chunkColor, life.alpha * seam * srcMask);
        }
    }

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
