// Cursed card cracks effect.
// Adapted from Content/Shaders/CorruptedCracks.glsl for MonoGame SpriteBatch.

float4x4 MatrixTransform;
float2 iResolution;
float iTime;
float2 CARD_CENTER;
float2 CARD_SIZE;
float CARD_ROTATION = 0.0;

float CARD_LEFT = 0.36;
float CARD_RIGHT = 0.64;
float CARD_BOTTOM = 0.12;
float CARD_TOP = 0.88;
float CARD_RADIUS = 0.035;

float EFFECT_SEED = 1.0;
float3 CARD_SHADOW_TINT = float3(0.080, 0.035, 0.125);
float3 CARD_SICKLY_TINT = float3(0.180, 0.055, 0.270);
float CARD_DESATURATION = 0.40;
float CARD_TINT_STRENGTH = 0.34;
float CARD_EDGE_DARKEN = 0.34;
float CARD_CENTER_PRESERVE = 0.62;

float PRIMARY_CRACK_SCALE = 5.4;
float SECONDARY_CRACK_SCALE = 10.5;
float HAIRLINE_CRACK_SCALE = 18.0;
float PRIMARY_CRACK_WIDTH = 0.105;
float SECONDARY_CRACK_WIDTH = 0.068;
float HAIRLINE_CRACK_WIDTH = 0.028;
float CRACK_BRANCH_CUTOFF = 0.42;
float CRACK_DARKEN = 0.58;
float CRACK_FLICKER_SPEED = 3.40;
float CRACK_FLICKER_DEPTH = 0.20;

float3 CORE_PURPLE = float3(0.98, 0.22, 1.00);
float3 INNER_PURPLE = float3(0.55, 0.08, 0.92);
float3 OUTER_PURPLE = float3(0.20, 0.04, 0.42);
float CORE_BRIGHTNESS = 1.26;
float RIM_BRIGHTNESS = 0.62;
float HALO_BRIGHTNESS = 0.34;
float HALO_WIDTH = 0.52;
float OOZE_SWELL_AMOUNT = 0.38;
float OOZE_SWIRL_STRENGTH = 0.18;
float OOZE_FLOW_SPEED = 0.16;
float OOZE_SURFACE_SHINE = 0.52;
float OOZE_EDGE_SHADOW = 0.36;
float ARCANE_SPARK_AMOUNT = 0.18;
float ARCANE_SPARK_SPEED = 2.10;

float BUBBLE_AMOUNT = 0.90;
float BUBBLE_SCALE = 14.0;
float BUBBLE_SPEED = 0.42;
float BUBBLE_SIZE_MIN = 0.055;
float BUBBLE_SIZE_MAX = 0.135;
float BUBBLE_HIGHLIGHT = 0.58;
float3 BUBBLE_RIM_COLOR = float3(1.00, 0.35, 1.00);

float MIST_INTENSITY = 0.52;
float MIST_SCALE = 5.50;
float MIST_RISE_SPEED = 0.055;
float MIST_SIDE_DRIFT = 0.020;
float MIST_SWIRL_STRENGTH = 1.45;
float3 MIST_COLOR_LOW = float3(0.20, 0.05, 0.34);
float3 MIST_COLOR_HIGH = float3(0.62, 0.16, 0.95);

float CURRENT_OPACITY = 0.26;
float CURRENT_SPEED = 0.18;
float VIGNETTE_STRENGTH = 0.42;
float GRAIN_AMOUNT = 0.025;
float EXPOSURE = 1.08;
float TIME_SPEED = 1.0;

static const float TWO_PI = 6.28318530718;
static const float2x2 FBM_ROT = float2x2(0.80, 0.60, -0.60, 0.80);

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

float2 Hash22(float2 p)
{
    float3 p3 = frac(float3(p.x, p.y, p.x) * float3(0.1031, 0.1030, 0.0973));
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.xx + p3.yz) * p3.zy);
}

float2 SeedOffset(float salt)
{
    return float2(EFFECT_SEED * 17.23 + salt * 31.70, EFFECT_SEED * -41.11 + salt * 19.90);
}

float ValueNoise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);
    return lerp(
        lerp(Hash21(i), Hash21(i + float2(1.0, 0.0)), f.x),
        lerp(Hash21(i + float2(0.0, 1.0)), Hash21(i + float2(1.0, 1.0)), f.x),
        f.y);
}

float Fbm(float2 p)
{
    float value = 0.0;
    float amp = 0.5;
    for (int i = 0; i < 4; i++)
    {
        value += amp * ValueNoise(p);
        p = mul(FBM_ROT, p) * 2.0;
        amp *= 0.5;
    }
    return value;
}

float2 Rotate(float2 value, float angle)
{
    float cs = cos(angle);
    float sn = sin(angle);
    return float2(cs * value.x - sn * value.y, sn * value.x + cs * value.y);
}

float CardAspect()
{
    return max(CARD_SIZE.x, 1.0) / max(CARD_SIZE.y, 1.0);
}

float CardSdf(float2 uv)
{
    float radius = max(CARD_RADIUS, 0.0);
    float2 center = float2((CARD_LEFT + CARD_RIGHT) * 0.5, (CARD_BOTTOM + CARD_TOP) * 0.5);
    float2 halfSize = float2((CARD_RIGHT - CARD_LEFT) * 0.5, (CARD_TOP - CARD_BOTTOM) * 0.5) - radius;
    float2 d = abs(uv - center) - halfSize;
    return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0) - radius;
}

float2 TextureUVToEffectUV(float2 textureUV)
{
    float2 size = max(CARD_SIZE, float2(1.0, 1.0));
    float2 screenPosition = textureUV * max(iResolution, float2(1.0, 1.0));
    float2 local = Rotate(screenPosition - CARD_CENTER, -CARD_ROTATION);
    float2 normalized = float2(local.x / size.x + 0.5, 0.5 - local.y / size.y);
    return float2(lerp(CARD_LEFT, CARD_RIGHT, normalized.x), lerp(CARD_BOTTOM, CARD_TOP, normalized.y));
}

float2 EffectUVToTextureUV(float2 effectUV)
{
    float2 size = max(CARD_SIZE, float2(1.0, 1.0));
    float2 normalized = float2(
        (effectUV.x - CARD_LEFT) / max(CARD_RIGHT - CARD_LEFT, 0.0001),
        (effectUV.y - CARD_BOTTOM) / max(CARD_TOP - CARD_BOTTOM, 0.0001));
    float2 local = float2((normalized.x - 0.5) * size.x, (0.5 - normalized.y) * size.y);
    float2 screenPosition = CARD_CENTER + Rotate(local, CARD_ROTATION);
    return screenPosition / max(iResolution, float2(1.0, 1.0));
}

float2 CardUV(float2 uv)
{
    return float2(
        (uv.x - CARD_LEFT) / max(CARD_RIGHT - CARD_LEFT, 0.0001),
        (uv.y - CARD_BOTTOM) / max(CARD_TOP - CARD_BOTTOM, 0.0001));
}

float2 CardSpace(float2 q)
{
    float2 p = q - 0.5;
    p.x *= CardAspect();
    return p;
}

float2 Voro(float2 p, out float2 cellId)
{
    float2 ip = floor(p);
    float2 fp = frac(p);
    float nearest = 8.0;
    float secondNearest = 8.0;
    cellId = ip;

    for (int y = -1; y <= 1; y++)
    {
        for (int x = -1; x <= 1; x++)
        {
            float2 cell = float2((float)x, (float)y);
            float2 pointOffset = Hash22(ip + cell);
            float2 delta = cell + pointOffset - fp;
            float distanceSquared = dot(delta, delta);
            if (distanceSquared < nearest)
            {
                secondNearest = nearest;
                nearest = distanceSquared;
                cellId = ip + cell;
            }
            else if (distanceSquared < secondNearest)
            {
                secondNearest = distanceSquared;
            }
        }
    }

    return sqrt(float2(nearest, secondNearest));
}

float CrackLayer(float2 p, float scale, float width, float branchCutoff, float salt, out float2 id)
{
    float2 edge = Voro(p * max(scale, 0.0001) + SeedOffset(salt), id);
    float branch = step(branchCutoff, Hash21(id + SeedOffset(salt + 0.7)));
    return (1.0 - smoothstep(0.0, max(width, 0.0001), edge.y - edge.x)) * branch;
}

float CrackNetwork(float2 q, out float core, out float rim, out float2 crackId)
{
    float2 p = CardSpace(q);
    float2 idA;
    float2 idB;
    float2 idC;
    float primary = CrackLayer(p, PRIMARY_CRACK_SCALE, PRIMARY_CRACK_WIDTH, CRACK_BRANCH_CUTOFF, 1.0, idA);
    float secondary = CrackLayer(
        p + (float2(Fbm(p * 2.0 + SeedOffset(2.0)), Fbm(p * 2.0 + SeedOffset(3.0))) - 0.5) * 0.08,
        SECONDARY_CRACK_SCALE,
        SECONDARY_CRACK_WIDTH,
        CRACK_BRANCH_CUTOFF + 0.13,
        4.0,
        idB);
    float hair = CrackLayer(p, HAIRLINE_CRACK_SCALE, HAIRLINE_CRACK_WIDTH, CRACK_BRANCH_CUTOFF + 0.24, 5.0, idC);
    float network = saturate(primary + secondary * 0.42 + hair * 0.16);
    core = smoothstep(0.42, 1.0, network);
    rim = smoothstep(0.76, 1.0, network);
    crackId = idA;
    return network;
}

float Sparkle(float2 q, float2 crackId, float crackMask)
{
    float dotScale = 74.0;
    float2 p = q * dotScale + SeedOffset(17.0);
    float2 cell = floor(p);
    float2 f = frac(p) - 0.5;
    float rnd = Hash21(cell + crackId * 1.31 + SeedOffset(18.0));
    float sparse = smoothstep(0.985, 1.0, rnd);
    float dotShape = 1.0 - smoothstep(0.03, 0.22, length(f));
    float twinkle = 0.45 + 0.55 * sin(iTime * ARCANE_SPARK_SPEED + rnd * TWO_PI);
    return sparse * dotShape * twinkle * crackMask * ARCANE_SPARK_AMOUNT;
}

float BubbleMask(float2 q, float ooze)
{
    float2 p = CardSpace(q) * max(BUBBLE_SCALE, 0.0001) + SeedOffset(13.0);
    float2 id;
    float2 d = Voro(p + float2(0.0, iTime * BUBBLE_SPEED), id);
    float radius = lerp(BUBBLE_SIZE_MIN, BUBBLE_SIZE_MAX, Hash21(id + SeedOffset(15.0)));
    float rim = 1.0 - smoothstep(0.0, 0.035, abs(d.x - radius));
    return rim * smoothstep(0.18, 0.72, ooze) * BUBBLE_AMOUNT;
}

float MistMask(float2 q, float ooze, float sd)
{
    float2 p = CardSpace(q);
    float2 flow = float2(MIST_SIDE_DRIFT * iTime, -MIST_RISE_SPEED * iTime);
    float smoke = Fbm(p * MIST_SCALE + flow + Fbm(p * MIST_SCALE * 0.6 + iTime * 0.1) * MIST_SWIRL_STRENGTH);
    float source = smoothstep(0.0, 0.34, ooze);
    float insideMask = 1.0 - smoothstep(-0.62, 0.0, sd);
    return source * smoothstep(0.32, 0.82, smoke) * insideMask * MIST_INTENSITY;
}

float ChangedMask(float2 textureUV)
{
    float3 current = tex2Dlod(TextureSampler, float4(textureUV, 0.0, 0.0)).rgb;
    float3 background = tex2Dlod(BackgroundSampler, float4(textureUV, 0.0, 0.0)).rgb;
    float delta = max(max(abs(current.r - background.r), abs(current.g - background.g)), abs(current.b - background.b));
    return smoothstep(0.02, 0.08, delta);
}

float4 SpritePixelShader(VSOutput input) : COLOR0
{
    float2 resolution = max(iResolution, float2(1.0, 1.0));
    float2 textureUV = input.TexCoord;
    float4 source = tex2Dlod(TextureSampler, float4(textureUV, 0.0, 0.0));
    float cardMask = ChangedMask(textureUV);
    float sd = -cardMask;
    float3 color = source.rgb;

    if (cardMask > 0.001)
    {
        float2 q = textureUV;
        float3 card = source.rgb;
        float luma = dot(card, float3(0.299, 0.587, 0.114));
        card = lerp(card, float3(luma, luma, luma), saturate(CARD_DESATURATION));

        float2 centered = q - 0.5;
        float centerDist = length(float2(centered.x * CardAspect(), centered.y));
        float edgeDark = 1.0 - smoothstep(0.0, max(CARD_CENTER_PRESERVE, 0.0001), centerDist);
        float3 tint = lerp(CARD_SHADOW_TINT, CARD_SICKLY_TINT, edgeDark);
        card = lerp(card, card * tint + tint * 0.48, saturate(CARD_TINT_STRENGTH));
        card = lerp(card, float3(0.48, 0.04, 0.70), 0.62);
        card *= 1.0 - CARD_EDGE_DARKEN * (1.0 - edgeDark);

        float core;
        float rim;
        float2 crackId;
        float cracks = CrackNetwork(q, core, rim, crackId);
        float2 p = CardSpace(q);
        float flowTime = iTime * OOZE_FLOW_SPEED;
        float oozeNoise = Fbm(p * 8.5 + float2(flowTime, -flowTime * 0.7) + SeedOffset(6.0));
        float oozeDetail = Fbm(p * 23.0 + float2(-flowTime * 3.0, flowTime * 2.2) + SeedOffset(9.0));
        float ooze = smoothstep(0.20, 0.78, cracks + oozeNoise * OOZE_SWELL_AMOUNT + oozeDetail * 0.12);
        float oozeCore = smoothstep(0.64, 1.08, cracks + oozeNoise * OOZE_SWELL_AMOUNT);
        float flickerSeed = Hash21(crackId + floor(iTime * 0.75) + SeedOffset(23.0));
        float flicker = 1.0 - CRACK_FLICKER_DEPTH + CRACK_FLICKER_DEPTH * sin(iTime * CRACK_FLICKER_SPEED + flickerSeed * TWO_PI);
        float halo = smoothstep(0.0, max(HALO_WIDTH, 0.0001), ooze) * (1.0 - oozeCore * 0.35);
        float current = (1.0 - smoothstep(0.18, 0.42, abs(Fbm(p * 11.0 + iTime * CURRENT_SPEED) - Fbm(p * 18.7 - iTime * CURRENT_SPEED)))) * ooze * CURRENT_OPACITY;
        float bubbles = BubbleMask(q, ooze);
        float mist = MistMask(q, ooze, sd) * cardMask;

        card = lerp(card, float3(0.055, 0.010, 0.105), saturate(ooze * OOZE_EDGE_SHADOW));
        card *= 1.0 - CRACK_DARKEN * oozeCore;
        card += OUTER_PURPLE * halo * HALO_BRIGHTNESS;
        card += OUTER_PURPLE * cracks * 1.1;
        card += INNER_PURPLE * ooze * RIM_BRIGHTNESS * flicker;
        card += CORE_PURPLE * oozeCore * CORE_BRIGHTNESS * flicker;
        card += CORE_PURPLE * current;
        card += CORE_PURPLE * Sparkle(q, crackId, oozeCore);
        card += BUBBLE_RIM_COLOR * bubbles * flicker;
        card += float3(1.0, 0.72, 1.0) * bubbles * BUBBLE_HIGHLIGHT;
        card += float3(OOZE_SURFACE_SHINE, OOZE_SURFACE_SHINE, OOZE_SURFACE_SHINE) * oozeDetail * ooze * 0.25;
        card += lerp(MIST_COLOR_LOW, MIST_COLOR_HIGH, saturate(mist)) * mist;

        float2 vignetteUv = q - 0.5;
        card *= 1.0 - VIGNETTE_STRENGTH * dot(vignetteUv, vignetteUv);
        float grain = Hash21(floor(textureUV * resolution) + frac(iTime * 5.71) * 147.0 + SeedOffset(25.0)) - 0.5;
        card += grain * GRAIN_AMOUNT;
        card *= EXPOSURE;
        card = card / (1.0 + card);
        color = lerp(source.rgb, saturate(card), cardMask);
    }

    return float4(saturate(color), source.a) * input.Color;
}

technique SpriteDrawing
{
    pass P0
    {
        VertexShader = compile vs_3_0 SpriteVertexShader();
        PixelShader = compile ps_3_0 SpritePixelShader();
    }
}
