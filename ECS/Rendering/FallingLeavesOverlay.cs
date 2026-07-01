using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Rendering;

public class FallingLeavesOverlay
{
    private readonly Effect _bufferAEffect;
    private readonly Effect _bufferBEffect;
    private readonly Effect _imageEffect;

    public bool IsAvailable => _bufferAEffect != null && _bufferBEffect != null && _imageEffect != null;

    public float Time { get; set; }
    public float LeafCount { get; set; } = 80f;
    public Vector3 LeafGreenDark { get; set; } = new(0.04f, 0.22f, 0.03f);
    public Vector3 LeafGreenLight { get; set; } = new(0.45f, 0.80f, 0.18f);
    public float LeafHueJitter { get; set; } = 0.10f;
    public float LeafBrightness { get; set; } = 1f;
    public float ScrollX { get; set; } = -0.06f;
    public float ScrollY { get; set; } = 0.05f;
    public Vector2 Spread { get; set; } = new(15f, 13f);
    public Vector2 SpinRate { get; set; } = new(0.2f, 0.3f);
    public float LeafRadiusMin { get; set; } = 0.7f;
    public float LeafRadiusVariation { get; set; } = 0.6f;
    public float BackgroundBrightness { get; set; } = 1f;
    public Vector3 BackgroundSkyLow { get; set; } = new(0.02f, 0.05f, 0.08f);
    public Vector3 BackgroundSkyHigh { get; set; } = new(0.10f, 0.16f, 0.20f);
    public Vector3 LightDir { get; set; } = new(0.57735026919f, 0.57735026919f, 0.57735026919f);
    public float GlarePower { get; set; } = 16f;
    public float FogAmount { get; set; } = 0.8f;
    public float FogFloor { get; set; } = 0.004f;

    public float BlurStrength { get; set; } = 0.05f;
    public Vector2 FocusA { get; set; } = new(0.6f, 0.6f);
    public Vector2 FocusB { get; set; } = new(0.5f, 0.5f);
    public Vector2 BokehAspect { get; set; } = new(9f / 16f, 1f);

    public float BloomRadius { get; set; } = 0.04f;
    public float BloomLod { get; set; } = 3f;
    public float BloomPower { get; set; } = 2f;
    public float RadialAmount { get; set; } = 0.2f;
    public float RadialLength { get; set; } = 0.3f;
    public Vector2 RadialTarget { get; set; } = new(1f, 1f);
    public float Saturation { get; set; } = -0.6f;
    public Vector3 ColorGrade { get; set; } = new(0.84f, 1f, 0.9f);
    public float Vignette { get; set; } = 0.1f;

    public FallingLeavesOverlay(Effect bufferAEffect, Effect bufferBEffect, Effect imageEffect)
    {
        _bufferAEffect = bufferAEffect;
        _bufferBEffect = bufferBEffect;
        _imageEffect = imageEffect;
    }

    public void BeginBufferA(SpriteBatch spriteBatch)
    {
        if (_bufferAEffect == null) return;

        Begin(spriteBatch, _bufferAEffect);
        _bufferAEffect.Parameters["Time"]?.SetValue(Time);
        _bufferAEffect.Parameters["LeafCount"]?.SetValue(LeafCount);
        _bufferAEffect.Parameters["LeafGreenDark"]?.SetValue(LeafGreenDark);
        _bufferAEffect.Parameters["LeafGreenLight"]?.SetValue(LeafGreenLight);
        _bufferAEffect.Parameters["LeafHueJitter"]?.SetValue(LeafHueJitter);
        _bufferAEffect.Parameters["LeafBrightness"]?.SetValue(LeafBrightness);
        _bufferAEffect.Parameters["ScrollX"]?.SetValue(ScrollX);
        _bufferAEffect.Parameters["ScrollY"]?.SetValue(ScrollY);
        _bufferAEffect.Parameters["Spread"]?.SetValue(Spread);
        _bufferAEffect.Parameters["SpinRate"]?.SetValue(SpinRate);
        _bufferAEffect.Parameters["LeafRadiusMin"]?.SetValue(LeafRadiusMin);
        _bufferAEffect.Parameters["LeafRadiusVariation"]?.SetValue(LeafRadiusVariation);
        _bufferAEffect.Parameters["BackgroundBrightness"]?.SetValue(BackgroundBrightness);
        _bufferAEffect.Parameters["BackgroundSkyLow"]?.SetValue(BackgroundSkyLow);
        _bufferAEffect.Parameters["BackgroundSkyHigh"]?.SetValue(BackgroundSkyHigh);
        _bufferAEffect.Parameters["LightDir"]?.SetValue(LightDir);
        _bufferAEffect.Parameters["GlarePower"]?.SetValue(GlarePower);
        _bufferAEffect.Parameters["FogAmount"]?.SetValue(FogAmount);
        _bufferAEffect.Parameters["FogFloor"]?.SetValue(FogFloor);
    }

    public void BeginBufferB(SpriteBatch spriteBatch)
    {
        if (_bufferBEffect == null) return;

        Begin(spriteBatch, _bufferBEffect);
        _bufferBEffect.Parameters["BlurStrength"]?.SetValue(BlurStrength);
        _bufferBEffect.Parameters["FocusA"]?.SetValue(FocusA);
        _bufferBEffect.Parameters["FocusB"]?.SetValue(FocusB);
        _bufferBEffect.Parameters["BokehAspect"]?.SetValue(BokehAspect);
    }

    public void BeginImage(SpriteBatch spriteBatch)
    {
        if (_imageEffect == null) return;

        Begin(spriteBatch, _imageEffect);
        _imageEffect.Parameters["BloomRadius"]?.SetValue(BloomRadius);
        _imageEffect.Parameters["BloomLod"]?.SetValue(BloomLod);
        _imageEffect.Parameters["BloomPower"]?.SetValue(BloomPower);
        _imageEffect.Parameters["RadialAmount"]?.SetValue(RadialAmount);
        _imageEffect.Parameters["RadialLength"]?.SetValue(RadialLength);
        _imageEffect.Parameters["RadialTarget"]?.SetValue(RadialTarget);
        _imageEffect.Parameters["Saturation"]?.SetValue(Saturation);
        _imageEffect.Parameters["ColorGrade"]?.SetValue(ColorGrade);
        _imageEffect.Parameters["Vignette"]?.SetValue(Vignette);
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D source)
    {
        if (source == null) return;
        spriteBatch.Draw(source, spriteBatch.GraphicsDevice.Viewport.Bounds, Color.White);
    }

    public void End(SpriteBatch spriteBatch)
    {
        spriteBatch.End();
    }

    private static void Begin(SpriteBatch spriteBatch, Effect effect)
    {
        effect.CurrentTechnique = effect.Techniques["SpriteDrawing"];

        Viewport viewport = spriteBatch.GraphicsDevice.Viewport;
        Matrix projection = Matrix.CreateOrthographicOffCenter(
            0,
            viewport.Width,
            viewport.Height,
            0,
            0,
            1
        );

        effect.Parameters["MatrixTransform"]?.SetValue(projection);
        effect.Parameters["ViewportSize"]?.SetValue(new Vector2(viewport.Width, viewport.Height));

        spriteBatch.Begin(
            SpriteSortMode.Immediate,
            BlendState.Opaque,
            SamplerState.LinearClamp,
            DepthStencilState.None,
            RasterizerState.CullNone,
            effect
        );
    }
}
