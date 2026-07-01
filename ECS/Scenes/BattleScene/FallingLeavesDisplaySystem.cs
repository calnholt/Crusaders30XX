using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Rendering;
using Crusaders30XX.ECS.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems;

[DebugTab("Falling Leaves")]
public class FallingLeavesDisplaySystem : Core.System
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly SpriteBatch _spriteBatch;
    private readonly ContentManager _content;
    private FallingLeavesOverlay _overlay;
    private RenderTarget2D _bufferA;
    private RenderTarget2D _bufferB;
    private bool _failed;
    private bool _isActive;
    private float _timeSeconds;

    public bool CanComposite =>
        ShaderRuntimeOptions.ShadersEnabled &&
        _isActive &&
        !_failed &&
        _overlay?.IsAvailable == true;

    [DebugEditable(DisplayName = "Time Scale", Step = 0.01f, Min = 0f, Max = 5f)]
    public float TimeScale { get; set; } = 1f;

    [DebugEditable(DisplayName = "Leaf Count", Step = 1f, Min = 1f, Max = 120f)]
    public int LeafCount { get; set; } = 80;

    [DebugEditable(DisplayName = "Dark Leaf R", Step = 0.01f, Min = 0f, Max = 1f)]
    public float LeafGreenDarkR { get; set; } = 0.04f;

    [DebugEditable(DisplayName = "Dark Leaf G", Step = 0.01f, Min = 0f, Max = 1f)]
    public float LeafGreenDarkG { get; set; } = 0.22f;

    [DebugEditable(DisplayName = "Dark Leaf B", Step = 0.01f, Min = 0f, Max = 1f)]
    public float LeafGreenDarkB { get; set; } = 0.03f;

    [DebugEditable(DisplayName = "Light Leaf R", Step = 0.01f, Min = 0f, Max = 1f)]
    public float LeafGreenLightR { get; set; } = 0.45f;

    [DebugEditable(DisplayName = "Light Leaf G", Step = 0.01f, Min = 0f, Max = 1f)]
    public float LeafGreenLightG { get; set; } = 0.80f;

    [DebugEditable(DisplayName = "Light Leaf B", Step = 0.01f, Min = 0f, Max = 1f)]
    public float LeafGreenLightB { get; set; } = 0.18f;

    [DebugEditable(DisplayName = "Hue Jitter", Step = 0.01f, Min = 0f, Max = 0.25f)]
    public float LeafHueJitter { get; set; } = 0.10f;

    [DebugEditable(DisplayName = "Leaf Brightness", Step = 0.01f, Min = 0f, Max = 3f)]
    public float LeafBrightness { get; set; } = 1f;

    [DebugEditable(DisplayName = "Scroll X", Step = 0.001f, Min = -0.5f, Max = 0.5f)]
    public float ScrollX { get; set; } = -0.06f;

    [DebugEditable(DisplayName = "Scroll Y", Step = 0.001f, Min = -0.5f, Max = 0.5f)]
    public float ScrollY { get; set; } = 0.05f;

    [DebugEditable(DisplayName = "Spread X", Step = 0.01f, Min = 0.01f, Max = 40f)]
    public float SpreadX { get; set; } = 15f;

    [DebugEditable(DisplayName = "Spread Y", Step = 0.01f, Min = 0.01f, Max = 40f)]
    public float SpreadY { get; set; } = 13f;

    [DebugEditable(DisplayName = "Spin X", Step = 0.01f, Min = -2f, Max = 2f)]
    public float SpinRateX { get; set; } = 0.2f;

    [DebugEditable(DisplayName = "Spin Y", Step = 0.01f, Min = -2f, Max = 2f)]
    public float SpinRateY { get; set; } = 0.3f;

    [DebugEditable(DisplayName = "Leaf Radius Min", Step = 0.01f, Min = 0.01f, Max = 3f)]
    public float LeafRadiusMin { get; set; } = 0.7f;

    [DebugEditable(DisplayName = "Leaf Radius Var", Step = 0.01f, Min = 0f, Max = 3f)]
    public float LeafRadiusVariation { get; set; } = 0.6f;

    [DebugEditable(DisplayName = "Background Brightness", Step = 0.01f, Min = 0f, Max = 3f)]
    public float BackgroundBrightness { get; set; } = 1f;

    [DebugEditable(DisplayName = "Sky Low R", Step = 0.01f, Min = 0f, Max = 1f)]
    public float BackgroundSkyLowR { get; set; } = 0.02f;

    [DebugEditable(DisplayName = "Sky Low G", Step = 0.01f, Min = 0f, Max = 1f)]
    public float BackgroundSkyLowG { get; set; } = 0.05f;

    [DebugEditable(DisplayName = "Sky Low B", Step = 0.01f, Min = 0f, Max = 1f)]
    public float BackgroundSkyLowB { get; set; } = 0.08f;

    [DebugEditable(DisplayName = "Sky High R", Step = 0.01f, Min = 0f, Max = 1f)]
    public float BackgroundSkyHighR { get; set; } = 0.10f;

    [DebugEditable(DisplayName = "Sky High G", Step = 0.01f, Min = 0f, Max = 1f)]
    public float BackgroundSkyHighG { get; set; } = 0.16f;

    [DebugEditable(DisplayName = "Sky High B", Step = 0.01f, Min = 0f, Max = 1f)]
    public float BackgroundSkyHighB { get; set; } = 0.20f;

    [DebugEditable(DisplayName = "Light Dir X", Step = 0.01f, Min = -1f, Max = 1f)]
    public float LightDirX { get; set; } = 0.57735026919f;

    [DebugEditable(DisplayName = "Light Dir Y", Step = 0.01f, Min = -1f, Max = 1f)]
    public float LightDirY { get; set; } = 0.57735026919f;

    [DebugEditable(DisplayName = "Light Dir Z", Step = 0.01f, Min = -1f, Max = 1f)]
    public float LightDirZ { get; set; } = 0.57735026919f;

    [DebugEditable(DisplayName = "Glare Power", Step = 0.01f, Min = 0.01f, Max = 64f)]
    public float GlarePower { get; set; } = 16f;

    [DebugEditable(DisplayName = "Fog Amount", Step = 0.01f, Min = 0f, Max = 3f)]
    public float FogAmount { get; set; } = 0.8f;

    [DebugEditable(DisplayName = "Fog Floor", Step = 0.001f, Min = 0f, Max = 0.1f)]
    public float FogFloor { get; set; } = 0.004f;

    [DebugEditable(DisplayName = "Blur Strength", Step = 0.001f, Min = 0f, Max = 0.2f)]
    public float BlurStrength { get; set; } = 0.05f;

    [DebugEditable(DisplayName = "Focus A X", Step = 0.01f, Min = 0f, Max = 1f)]
    public float FocusAX { get; set; } = 0.6f;

    [DebugEditable(DisplayName = "Focus A Y", Step = 0.01f, Min = 0f, Max = 1f)]
    public float FocusAY { get; set; } = 0.6f;

    [DebugEditable(DisplayName = "Focus B X", Step = 0.01f, Min = 0f, Max = 1f)]
    public float FocusBX { get; set; } = 0.5f;

    [DebugEditable(DisplayName = "Focus B Y", Step = 0.01f, Min = 0f, Max = 1f)]
    public float FocusBY { get; set; } = 0.5f;

    [DebugEditable(DisplayName = "Bokeh Aspect X", Step = 0.01f, Min = 0.01f, Max = 4f)]
    public float BokehAspectX { get; set; } = 9f / 16f;

    [DebugEditable(DisplayName = "Bokeh Aspect Y", Step = 0.01f, Min = 0.01f, Max = 4f)]
    public float BokehAspectY { get; set; } = 1f;

    [DebugEditable(DisplayName = "Bloom Radius", Step = 0.001f, Min = 0f, Max = 0.2f)]
    public float BloomRadius { get; set; } = 0.04f;

    [DebugEditable(DisplayName = "Bloom LOD", Step = 0.01f, Min = 0f, Max = 6f)]
    public float BloomLod { get; set; } = 3f;

    [DebugEditable(DisplayName = "Bloom Power", Step = 0.01f, Min = 0.01f, Max = 8f)]
    public float BloomPower { get; set; } = 2f;

    [DebugEditable(DisplayName = "Radial Amount", Step = 0.01f, Min = 0f, Max = 2f)]
    public float RadialAmount { get; set; } = 0.2f;

    [DebugEditable(DisplayName = "Radial Length", Step = 0.01f, Min = 0f, Max = 1f)]
    public float RadialLength { get; set; } = 0.3f;

    [DebugEditable(DisplayName = "Radial Target X", Step = 0.01f, Min = 0f, Max = 1f)]
    public float RadialTargetX { get; set; } = 1f;

    [DebugEditable(DisplayName = "Radial Target Y", Step = 0.01f, Min = 0f, Max = 1f)]
    public float RadialTargetY { get; set; } = 1f;

    [DebugEditable(DisplayName = "Saturation", Step = 0.01f, Min = -2f, Max = 2f)]
    public float Saturation { get; set; } = -0.6f;

    [DebugEditable(DisplayName = "Grade R", Step = 0.01f, Min = 0.01f, Max = 4f)]
    public float ColorGradeR { get; set; } = 0.84f;

    [DebugEditable(DisplayName = "Grade G", Step = 0.01f, Min = 0.01f, Max = 4f)]
    public float ColorGradeG { get; set; } = 1f;

    [DebugEditable(DisplayName = "Grade B", Step = 0.01f, Min = 0.01f, Max = 4f)]
    public float ColorGradeB { get; set; } = 0.9f;

    [DebugEditable(DisplayName = "Vignette", Step = 0.01f, Min = 0f, Max = 2f)]
    public float Vignette { get; set; } = 0.1f;

    public FallingLeavesDisplaySystem(
        EntityManager entityManager,
        GraphicsDevice graphicsDevice,
        SpriteBatch spriteBatch,
        ContentManager content
    ) : base(entityManager)
    {
        _graphicsDevice = graphicsDevice;
        _spriteBatch = spriteBatch;
        _content = content;
    }

    protected override IEnumerable<Entity> GetRelevantEntities()
    {
        return Array.Empty<Entity>();
    }

    protected override void UpdateEntity(Entity entity, GameTime gameTime)
    {
    }

    public override void Update(GameTime gameTime)
    {
        bool wasActive = _isActive;
        _isActive = IsJungleBattleActive();

        if (!wasActive && _isActive)
        {
            _timeSeconds = 0f;
        }

        if (_isActive && ShaderRuntimeOptions.ShadersEnabled)
        {
            EnsureLoaded();
            _timeSeconds += MathHelper.Max(0f, (float)gameTime.ElapsedGameTime.TotalSeconds) *
                MathHelper.Max(0f, TimeScale);
        }

        base.Update(gameTime);
    }

    public void Composite(Texture2D source, RenderTarget2D destination)
    {
        if (!CanComposite || source == null || destination == null) return;

        EnsurePassTargets(destination.Width, destination.Height);
        if (_bufferA == null || _bufferB == null) return;

        ConfigureOverlay();

        _graphicsDevice.SetRenderTarget(_bufferA);
        _graphicsDevice.Clear(Color.Black);
        _overlay.BeginBufferA(_spriteBatch);
        _overlay.Draw(_spriteBatch, source);
        _overlay.End(_spriteBatch);

        _graphicsDevice.SetRenderTarget(_bufferB);
        _graphicsDevice.Clear(Color.Black);
        _overlay.BeginBufferB(_spriteBatch);
        _overlay.Draw(_spriteBatch, _bufferA);
        _overlay.End(_spriteBatch);

        _graphicsDevice.SetRenderTarget(destination);
        _graphicsDevice.Clear(Color.Black);
        _overlay.BeginImage(_spriteBatch);
        _overlay.Draw(_spriteBatch, _bufferB);
        _overlay.End(_spriteBatch);
    }

    private bool IsJungleBattleActive()
    {
        SceneState scene = EntityManager.GetEntitiesWithComponent<SceneState>()
            .FirstOrDefault()
            ?.GetComponent<SceneState>();
        if (scene?.Current != SceneId.Battle) return false;

        Battlefield battlefield = EntityManager.GetEntitiesWithComponent<Battlefield>()
            .FirstOrDefault()
            ?.GetComponent<Battlefield>();
        return battlefield?.Location == BattleLocation.Jungle;
    }

    private void EnsureLoaded()
    {
        if (_failed || _overlay != null) return;

        try
        {
            Effect bufferAEffect = _content.Load<Effect>("Shaders/FallingLeavesBufferA");
            Effect bufferBEffect = _content.Load<Effect>("Shaders/FallingLeavesBufferB");
            Effect imageEffect = _content.Load<Effect>("Shaders/FallingLeavesImage");
            _overlay = new FallingLeavesOverlay(bufferAEffect, bufferBEffect, imageEffect);
        }
        catch (Exception ex)
        {
            _failed = true;
            LoggingService.Append(
                "FallingLeavesDisplaySystem.EnsureLoaded",
                new JsonObject
                {
                    ["error"] = "Failed to load shader",
                    ["exception"] = ex.Message
                }
            );
        }
    }

    private void EnsurePassTargets(int width, int height)
    {
        if (width <= 0 || height <= 0) return;
        if (_bufferA != null && _bufferA.Width == width && _bufferA.Height == height &&
            _bufferB != null && _bufferB.Width == width && _bufferB.Height == height)
        {
            return;
        }

        _bufferA?.Dispose();
        _bufferB?.Dispose();
        _bufferA = new RenderTarget2D(_graphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.None);
        _bufferB = new RenderTarget2D(_graphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.None);
    }

    private void ConfigureOverlay()
    {
        _overlay.Time = _timeSeconds;
        _overlay.LeafCount = MathHelper.Clamp(LeafCount, 1, 120);
        _overlay.LeafGreenDark = new Vector3(LeafGreenDarkR, LeafGreenDarkG, LeafGreenDarkB);
        _overlay.LeafGreenLight = new Vector3(LeafGreenLightR, LeafGreenLightG, LeafGreenLightB);
        _overlay.LeafHueJitter = Math.Max(0f, LeafHueJitter);
        _overlay.LeafBrightness = Math.Max(0f, LeafBrightness);
        _overlay.ScrollX = ScrollX;
        _overlay.ScrollY = ScrollY;
        _overlay.Spread = new Vector2(Math.Max(0.001f, SpreadX), Math.Max(0.001f, SpreadY));
        _overlay.SpinRate = new Vector2(SpinRateX, SpinRateY);
        _overlay.LeafRadiusMin = Math.Max(0.001f, LeafRadiusMin);
        _overlay.LeafRadiusVariation = Math.Max(0f, LeafRadiusVariation);
        _overlay.BackgroundBrightness = Math.Max(0f, BackgroundBrightness);
        _overlay.BackgroundSkyLow = new Vector3(BackgroundSkyLowR, BackgroundSkyLowG, BackgroundSkyLowB);
        _overlay.BackgroundSkyHigh = new Vector3(BackgroundSkyHighR, BackgroundSkyHighG, BackgroundSkyHighB);
        _overlay.LightDir = new Vector3(LightDirX, LightDirY, LightDirZ);
        _overlay.GlarePower = Math.Max(0.001f, GlarePower);
        _overlay.FogAmount = Math.Max(0f, FogAmount);
        _overlay.FogFloor = Math.Max(0f, FogFloor);

        _overlay.BlurStrength = Math.Max(0f, BlurStrength);
        _overlay.FocusA = new Vector2(FocusAX, FocusAY);
        _overlay.FocusB = new Vector2(FocusBX, FocusBY);
        _overlay.BokehAspect = new Vector2(Math.Max(0.001f, BokehAspectX), Math.Max(0.001f, BokehAspectY));

        _overlay.BloomRadius = Math.Max(0f, BloomRadius);
        _overlay.BloomLod = Math.Max(0f, BloomLod);
        _overlay.BloomPower = Math.Max(0.001f, BloomPower);
        _overlay.RadialAmount = Math.Max(0f, RadialAmount);
        _overlay.RadialLength = Math.Max(0f, RadialLength);
        _overlay.RadialTarget = new Vector2(RadialTargetX, RadialTargetY);
        _overlay.Saturation = Saturation;
        _overlay.ColorGrade = new Vector3(
            Math.Max(0.001f, ColorGradeR),
            Math.Max(0.001f, ColorGradeG),
            Math.Max(0.001f, ColorGradeB)
        );
        _overlay.Vignette = Math.Max(0f, Vignette);
    }
}
