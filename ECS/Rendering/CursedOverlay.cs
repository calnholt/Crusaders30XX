using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Rendering;

public sealed class CursedOverlay
{
    private readonly Effect _effect;

    public CursedOverlay(Effect effect)
    {
        _effect = effect;
    }

    public bool IsAvailable => _effect != null;
    public Texture2D BackgroundTexture { get; set; }
    public float Time { get; set; }
    public Vector2 CardCenter { get; set; }
    public Vector2 CardSize { get; set; }
    public float CardRotation { get; set; }
    public float CardRadius { get; set; } = 0.035f;
    public float EffectSeed { get; set; } = 1f;
    public Vector3 CardShadowTint { get; set; } = new(0.080f, 0.035f, 0.125f);
    public Vector3 CardSicklyTint { get; set; } = new(0.180f, 0.055f, 0.270f);
    public float CardDesaturation { get; set; } = 0.40f;
    public float CardTintStrength { get; set; } = 0.34f;
    public float CardEdgeDarken { get; set; } = 0.34f;
    public float CardCenterPreserve { get; set; } = 0.62f;
    public float PrimaryCrackScale { get; set; } = 5.4f;
    public float SecondaryCrackScale { get; set; } = 10.5f;
    public float HairlineCrackScale { get; set; } = 18f;
    public float PrimaryCrackWidth { get; set; } = 0.105f;
    public float SecondaryCrackWidth { get; set; } = 0.068f;
    public float HairlineCrackWidth { get; set; } = 0.028f;
    public float CrackBranchCutoff { get; set; } = 0.42f;
    public float CrackDarken { get; set; } = 0.58f;
    public float CrackFlickerSpeed { get; set; } = 3.40f;
    public float CrackFlickerDepth { get; set; } = 0.20f;
    public Vector3 CorePurple { get; set; } = new(0.98f, 0.22f, 1f);
    public Vector3 InnerPurple { get; set; } = new(0.55f, 0.08f, 0.92f);
    public Vector3 OuterPurple { get; set; } = new(0.20f, 0.04f, 0.42f);
    public float CoreBrightness { get; set; } = 1.26f;
    public float RimBrightness { get; set; } = 0.62f;
    public float HaloBrightness { get; set; } = 0.34f;
    public float HaloWidth { get; set; } = 0.52f;
    public float OozeSwellAmount { get; set; } = 0.38f;
    public float OozeSwirlStrength { get; set; } = 0.18f;
    public float OozeFlowSpeed { get; set; } = 0.16f;
    public float OozeSurfaceShine { get; set; } = 0.52f;
    public float OozeEdgeShadow { get; set; } = 0.36f;
    public float ArcaneSparkAmount { get; set; } = 0.18f;
    public float ArcaneSparkSpeed { get; set; } = 2.10f;
    public float BubbleAmount { get; set; } = 0.90f;
    public float BubbleScale { get; set; } = 14f;
    public float BubbleSpeed { get; set; } = 0.42f;
    public float BubbleSizeMin { get; set; } = 0.055f;
    public float BubbleSizeMax { get; set; } = 0.135f;
    public float BubbleHighlight { get; set; } = 0.58f;
    public Vector3 BubbleRimColor { get; set; } = new(1f, 0.35f, 1f);
    public float MistIntensity { get; set; } = 0.52f;
    public float MistScale { get; set; } = 5.50f;
    public float MistRiseSpeed { get; set; } = 0.055f;
    public float MistSideDrift { get; set; } = 0.020f;
    public float MistSwirlStrength { get; set; } = 1.45f;
    public Vector3 MistColorLow { get; set; } = new(0.20f, 0.05f, 0.34f);
    public Vector3 MistColorHigh { get; set; } = new(0.62f, 0.16f, 0.95f);
    public float CurrentOpacity { get; set; } = 0.26f;
    public float CurrentSpeed { get; set; } = 0.18f;
    public float VignetteStrength { get; set; } = 0.42f;
    public float GrainAmount { get; set; } = 0.025f;
    public float Exposure { get; set; } = 1.08f;
    public float TimeSpeed { get; set; } = 1f;

    public void Begin(SpriteBatch spriteBatch)
    {
        if (_effect == null) return;

        _effect.CurrentTechnique = _effect.Techniques["SpriteDrawing"];
        Viewport viewport = spriteBatch.GraphicsDevice.Viewport;
        Matrix projection = Matrix.CreateOrthographicOffCenter(0, viewport.Width, viewport.Height, 0, 0, 1);

        Set("MatrixTransform", projection);
        Set("iResolution", new Vector2(viewport.Width, viewport.Height));
        Set("iTime", Time * TimeSpeed);
        Set("BackgroundTexture", BackgroundTexture);
        Set("CARD_CENTER", CardCenter);
        Set("CARD_SIZE", CardSize);
        Set("CARD_ROTATION", CardRotation);
        Set("CARD_RADIUS", CardRadius);
        Set("EFFECT_SEED", EffectSeed);
        Set("CARD_SHADOW_TINT", CardShadowTint);
        Set("CARD_SICKLY_TINT", CardSicklyTint);
        Set("CARD_DESATURATION", CardDesaturation);
        Set("CARD_TINT_STRENGTH", CardTintStrength);
        Set("CARD_EDGE_DARKEN", CardEdgeDarken);
        Set("CARD_CENTER_PRESERVE", CardCenterPreserve);
        Set("PRIMARY_CRACK_SCALE", PrimaryCrackScale);
        Set("SECONDARY_CRACK_SCALE", SecondaryCrackScale);
        Set("HAIRLINE_CRACK_SCALE", HairlineCrackScale);
        Set("PRIMARY_CRACK_WIDTH", PrimaryCrackWidth);
        Set("SECONDARY_CRACK_WIDTH", SecondaryCrackWidth);
        Set("HAIRLINE_CRACK_WIDTH", HairlineCrackWidth);
        Set("CRACK_BRANCH_CUTOFF", CrackBranchCutoff);
        Set("CRACK_DARKEN", CrackDarken);
        Set("CRACK_FLICKER_SPEED", CrackFlickerSpeed);
        Set("CRACK_FLICKER_DEPTH", CrackFlickerDepth);
        Set("CORE_PURPLE", CorePurple);
        Set("INNER_PURPLE", InnerPurple);
        Set("OUTER_PURPLE", OuterPurple);
        Set("CORE_BRIGHTNESS", CoreBrightness);
        Set("RIM_BRIGHTNESS", RimBrightness);
        Set("HALO_BRIGHTNESS", HaloBrightness);
        Set("HALO_WIDTH", HaloWidth);
        Set("OOZE_SWELL_AMOUNT", OozeSwellAmount);
        Set("OOZE_SWIRL_STRENGTH", OozeSwirlStrength);
        Set("OOZE_FLOW_SPEED", OozeFlowSpeed);
        Set("OOZE_SURFACE_SHINE", OozeSurfaceShine);
        Set("OOZE_EDGE_SHADOW", OozeEdgeShadow);
        Set("ARCANE_SPARK_AMOUNT", ArcaneSparkAmount);
        Set("ARCANE_SPARK_SPEED", ArcaneSparkSpeed);
        Set("BUBBLE_AMOUNT", BubbleAmount);
        Set("BUBBLE_SCALE", BubbleScale);
        Set("BUBBLE_SPEED", BubbleSpeed);
        Set("BUBBLE_SIZE_MIN", BubbleSizeMin);
        Set("BUBBLE_SIZE_MAX", BubbleSizeMax);
        Set("BUBBLE_HIGHLIGHT", BubbleHighlight);
        Set("BUBBLE_RIM_COLOR", BubbleRimColor);
        Set("MIST_INTENSITY", MistIntensity);
        Set("MIST_SCALE", MistScale);
        Set("MIST_RISE_SPEED", MistRiseSpeed);
        Set("MIST_SIDE_DRIFT", MistSideDrift);
        Set("MIST_SWIRL_STRENGTH", MistSwirlStrength);
        Set("MIST_COLOR_LOW", MistColorLow);
        Set("MIST_COLOR_HIGH", MistColorHigh);
        Set("CURRENT_OPACITY", CurrentOpacity);
        Set("CURRENT_SPEED", CurrentSpeed);
        Set("VIGNETTE_STRENGTH", VignetteStrength);
        Set("GRAIN_AMOUNT", GrainAmount);
        Set("EXPOSURE", Exposure);
        Set("TIME_SPEED", TimeSpeed);

        spriteBatch.Begin(
            SpriteSortMode.Immediate,
            BlendState.Opaque,
            SamplerState.LinearClamp,
            DepthStencilState.None,
            RasterizerState.CullNone,
            _effect);
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D source)
    {
        if (_effect == null || source == null || BackgroundTexture == null) return;
        spriteBatch.Draw(source, spriteBatch.GraphicsDevice.Viewport.Bounds, Color.White);
    }

    public void End(SpriteBatch spriteBatch)
    {
        if (_effect == null) return;
        spriteBatch.End();
    }

    private void Set(string parameterName, float value) => _effect.Parameters[parameterName]?.SetValue(value);
    private void Set(string parameterName, Texture2D value) => _effect.Parameters[parameterName]?.SetValue(value);
    private void Set(string parameterName, Vector2 value) => _effect.Parameters[parameterName]?.SetValue(value);
    private void Set(string parameterName, Vector3 value) => _effect.Parameters[parameterName]?.SetValue(value);
    private void Set(string parameterName, Matrix value) => _effect.Parameters[parameterName]?.SetValue(value);
}
