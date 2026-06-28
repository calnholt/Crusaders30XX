using System;
using System.Collections.Generic;
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

[DebugTab("Cursed Display")]
public sealed class CursedDisplaySystem : Core.System
{
    private const int PreRenderPriority = 100;
    private const int PostRenderPriority = -65;

    private readonly GraphicsDevice _graphicsDevice;
    private readonly SpriteBatch _spriteBatch;
    private readonly ContentManager _content;

    private Effect _effect;
    private CursedOverlay _overlay;
    private RenderTarget2D _beforeCardTarget;
    private RenderTarget2D _afterCardTarget;
    private bool _failed;
    private bool _hasCapture;
    private Entity _capturedCard;
    private Vector2 _capturedPosition;
    private float _capturedScale = 1f;
    private float _capturedRotation;
    private float _timeSeconds;

    [DebugEditable(DisplayName = "Card Radius", Step = 0.01f, Min = 0f, Max = 0.2f)]
    public float CardRadius { get; set; } = 0.035f;

    [DebugEditable(DisplayName = "Effect Seed", Step = 0.01f, Min = -100f, Max = 100f)]
    public float EffectSeed { get; set; } = 1f;

    [DebugEditable(DisplayName = "Desaturation", Step = 0.01f, Min = 0f, Max = 1f)]
    public float CardDesaturation { get; set; } = 0.40f;

    [DebugEditable(DisplayName = "Tint Strength", Step = 0.01f, Min = 0f, Max = 1f)]
    public float CardTintStrength { get; set; } = 0.34f;

    [DebugEditable(DisplayName = "Edge Darken", Step = 0.01f, Min = 0f, Max = 1f)]
    public float CardEdgeDarken { get; set; } = 0.34f;

    [DebugEditable(DisplayName = "Center Preserve", Step = 0.01f, Min = 0.01f, Max = 2f)]
    public float CardCenterPreserve { get; set; } = 0.62f;

    [DebugEditable(DisplayName = "Shadow Tint R", Step = 0.01f, Min = 0f, Max = 1f)]
    public float CardShadowTintR { get; set; } = 0.080f;

    [DebugEditable(DisplayName = "Shadow Tint G", Step = 0.01f, Min = 0f, Max = 1f)]
    public float CardShadowTintG { get; set; } = 0.035f;

    [DebugEditable(DisplayName = "Shadow Tint B", Step = 0.01f, Min = 0f, Max = 1f)]
    public float CardShadowTintB { get; set; } = 0.125f;

    [DebugEditable(DisplayName = "Sickly Tint R", Step = 0.01f, Min = 0f, Max = 1f)]
    public float CardSicklyTintR { get; set; } = 0.180f;

    [DebugEditable(DisplayName = "Sickly Tint G", Step = 0.01f, Min = 0f, Max = 1f)]
    public float CardSicklyTintG { get; set; } = 0.055f;

    [DebugEditable(DisplayName = "Sickly Tint B", Step = 0.01f, Min = 0f, Max = 1f)]
    public float CardSicklyTintB { get; set; } = 0.270f;

    [DebugEditable(DisplayName = "Primary Crack Scale", Step = 0.01f, Min = 0.01f, Max = 50f)]
    public float PrimaryCrackScale { get; set; } = 5.4f;

    [DebugEditable(DisplayName = "Secondary Crack Scale", Step = 0.01f, Min = 0.01f, Max = 80f)]
    public float SecondaryCrackScale { get; set; } = 10.5f;

    [DebugEditable(DisplayName = "Hairline Crack Scale", Step = 0.01f, Min = 0.01f, Max = 120f)]
    public float HairlineCrackScale { get; set; } = 18f;

    [DebugEditable(DisplayName = "Primary Crack Width", Step = 0.001f, Min = 0.001f, Max = 0.5f)]
    public float PrimaryCrackWidth { get; set; } = 0.105f;

    [DebugEditable(DisplayName = "Secondary Crack Width", Step = 0.001f, Min = 0.001f, Max = 0.5f)]
    public float SecondaryCrackWidth { get; set; } = 0.068f;

    [DebugEditable(DisplayName = "Hairline Crack Width", Step = 0.001f, Min = 0.001f, Max = 0.3f)]
    public float HairlineCrackWidth { get; set; } = 0.028f;

    [DebugEditable(DisplayName = "Branch Cutoff", Step = 0.01f, Min = 0f, Max = 1f)]
    public float CrackBranchCutoff { get; set; } = 0.42f;

    [DebugEditable(DisplayName = "Crack Darken", Step = 0.01f, Min = 0f, Max = 2f)]
    public float CrackDarken { get; set; } = 0.58f;

    [DebugEditable(DisplayName = "Flicker Speed", Step = 0.01f, Min = 0f, Max = 20f)]
    public float CrackFlickerSpeed { get; set; } = 3.40f;

    [DebugEditable(DisplayName = "Flicker Depth", Step = 0.01f, Min = 0f, Max = 1f)]
    public float CrackFlickerDepth { get; set; } = 0.20f;

    [DebugEditable(DisplayName = "Core Purple R", Step = 0.01f, Min = 0f, Max = 3f)]
    public float CorePurpleR { get; set; } = 0.98f;

    [DebugEditable(DisplayName = "Core Purple G", Step = 0.01f, Min = 0f, Max = 3f)]
    public float CorePurpleG { get; set; } = 0.22f;

    [DebugEditable(DisplayName = "Core Purple B", Step = 0.01f, Min = 0f, Max = 3f)]
    public float CorePurpleB { get; set; } = 1f;

    [DebugEditable(DisplayName = "Inner Purple R", Step = 0.01f, Min = 0f, Max = 3f)]
    public float InnerPurpleR { get; set; } = 0.55f;

    [DebugEditable(DisplayName = "Inner Purple G", Step = 0.01f, Min = 0f, Max = 3f)]
    public float InnerPurpleG { get; set; } = 0.08f;

    [DebugEditable(DisplayName = "Inner Purple B", Step = 0.01f, Min = 0f, Max = 3f)]
    public float InnerPurpleB { get; set; } = 0.92f;

    [DebugEditable(DisplayName = "Outer Purple R", Step = 0.01f, Min = 0f, Max = 3f)]
    public float OuterPurpleR { get; set; } = 0.20f;

    [DebugEditable(DisplayName = "Outer Purple G", Step = 0.01f, Min = 0f, Max = 3f)]
    public float OuterPurpleG { get; set; } = 0.04f;

    [DebugEditable(DisplayName = "Outer Purple B", Step = 0.01f, Min = 0f, Max = 3f)]
    public float OuterPurpleB { get; set; } = 0.42f;

    [DebugEditable(DisplayName = "Core Brightness", Step = 0.01f, Min = 0f, Max = 5f)]
    public float CoreBrightness { get; set; } = 1.26f;

    [DebugEditable(DisplayName = "Rim Brightness", Step = 0.01f, Min = 0f, Max = 5f)]
    public float RimBrightness { get; set; } = 0.62f;

    [DebugEditable(DisplayName = "Halo Brightness", Step = 0.01f, Min = 0f, Max = 5f)]
    public float HaloBrightness { get; set; } = 0.34f;

    [DebugEditable(DisplayName = "Halo Width", Step = 0.01f, Min = 0.001f, Max = 2f)]
    public float HaloWidth { get; set; } = 0.52f;

    [DebugEditable(DisplayName = "Ooze Swell", Step = 0.01f, Min = 0f, Max = 2f)]
    public float OozeSwellAmount { get; set; } = 0.38f;

    [DebugEditable(DisplayName = "Ooze Swirl", Step = 0.01f, Min = 0f, Max = 2f)]
    public float OozeSwirlStrength { get; set; } = 0.18f;

    [DebugEditable(DisplayName = "Ooze Flow Speed", Step = 0.01f, Min = 0f, Max = 5f)]
    public float OozeFlowSpeed { get; set; } = 0.16f;

    [DebugEditable(DisplayName = "Ooze Shine", Step = 0.01f, Min = 0f, Max = 3f)]
    public float OozeSurfaceShine { get; set; } = 0.52f;

    [DebugEditable(DisplayName = "Ooze Edge Shadow", Step = 0.01f, Min = 0f, Max = 2f)]
    public float OozeEdgeShadow { get; set; } = 0.36f;

    [DebugEditable(DisplayName = "Spark Amount", Step = 0.01f, Min = 0f, Max = 2f)]
    public float ArcaneSparkAmount { get; set; } = 0.18f;

    [DebugEditable(DisplayName = "Spark Speed", Step = 0.01f, Min = 0f, Max = 10f)]
    public float ArcaneSparkSpeed { get; set; } = 2.10f;

    [DebugEditable(DisplayName = "Bubble Amount", Step = 0.01f, Min = 0f, Max = 2f)]
    public float BubbleAmount { get; set; } = 0.90f;

    [DebugEditable(DisplayName = "Bubble Scale", Step = 0.01f, Min = 0.01f, Max = 80f)]
    public float BubbleScale { get; set; } = 14f;

    [DebugEditable(DisplayName = "Bubble Speed", Step = 0.01f, Min = 0f, Max = 5f)]
    public float BubbleSpeed { get; set; } = 0.42f;

    [DebugEditable(DisplayName = "Bubble Size Min", Step = 0.001f, Min = 0.001f, Max = 0.5f)]
    public float BubbleSizeMin { get; set; } = 0.055f;

    [DebugEditable(DisplayName = "Bubble Size Max", Step = 0.001f, Min = 0.001f, Max = 0.5f)]
    public float BubbleSizeMax { get; set; } = 0.135f;

    [DebugEditable(DisplayName = "Bubble Highlight", Step = 0.01f, Min = 0f, Max = 2f)]
    public float BubbleHighlight { get; set; } = 0.58f;

    [DebugEditable(DisplayName = "Mist Intensity", Step = 0.01f, Min = 0f, Max = 2f)]
    public float MistIntensity { get; set; } = 0.52f;

    [DebugEditable(DisplayName = "Mist Scale", Step = 0.01f, Min = 0.01f, Max = 50f)]
    public float MistScale { get; set; } = 5.50f;

    [DebugEditable(DisplayName = "Mist Rise Speed", Step = 0.001f, Min = 0f, Max = 1f)]
    public float MistRiseSpeed { get; set; } = 0.055f;

    [DebugEditable(DisplayName = "Mist Side Drift", Step = 0.001f, Min = -1f, Max = 1f)]
    public float MistSideDrift { get; set; } = 0.020f;

    [DebugEditable(DisplayName = "Mist Swirl", Step = 0.01f, Min = 0f, Max = 5f)]
    public float MistSwirlStrength { get; set; } = 1.45f;

    [DebugEditable(DisplayName = "Current Opacity", Step = 0.01f, Min = 0f, Max = 2f)]
    public float CurrentOpacity { get; set; } = 0.26f;

    [DebugEditable(DisplayName = "Current Speed", Step = 0.01f, Min = 0f, Max = 5f)]
    public float CurrentSpeed { get; set; } = 0.18f;

    [DebugEditable(DisplayName = "Vignette", Step = 0.01f, Min = 0f, Max = 2f)]
    public float VignetteStrength { get; set; } = 0.42f;

    [DebugEditable(DisplayName = "Grain", Step = 0.001f, Min = 0f, Max = 0.2f)]
    public float GrainAmount { get; set; } = 0.025f;

    [DebugEditable(DisplayName = "Exposure", Step = 0.01f, Min = 0.01f, Max = 5f)]
    public float Exposure { get; set; } = 1.08f;

    [DebugEditable(DisplayName = "Time Speed", Step = 0.01f, Min = 0f, Max = 5f)]
    public float TimeSpeed { get; set; } = 1f;

    public CursedDisplaySystem(
        EntityManager entityManager,
        GraphicsDevice graphicsDevice,
        SpriteBatch spriteBatch,
        ContentManager content)
        : base(entityManager)
    {
        _graphicsDevice = graphicsDevice;
        _spriteBatch = spriteBatch;
        _content = content;

        EventManager.Subscribe<CardBaseRenderStartedEvent>(OnCardBaseRenderStarted, PreRenderPriority);
        // EventManager.Subscribe<CardBaseRenderCompletedEvent>(
            // evt => FrameProfiler.Measure("CursedDisplaySystem.OnCardBaseRenderCompletedEvent", () => OnCardBaseRenderCompleted(evt)),
            // PostRenderPriority);
    }

    protected override IEnumerable<Entity> GetRelevantEntities()
    {
        return EntityManager.GetEntitiesWithComponent<Cursed>();
    }

    public override void Update(GameTime gameTime)
    {
        if (!ShaderRuntimeOptions.ShadersEnabled || _failed) return;

        _timeSeconds += MathHelper.Max(0f, (float)gameTime.ElapsedGameTime.TotalSeconds);
        if (_overlay == null && HasCursedCards()) EnsureLoaded();
    }

    protected override void UpdateEntity(Entity entity, GameTime gameTime)
    {
    }

    private void OnCardBaseRenderStarted(CardBaseRenderStartedEvent evt)
    {
        BeginCursedRender(evt.Card, evt.Position, evt.Scale, evt.Rotation);
    }

    private void OnCardBaseRenderCompleted(CardBaseRenderCompletedEvent evt)
    {
        EndCursedRender(evt.Card);
    }

    private void BeginCursedRender(Entity card, Vector2 position, float scale, float rotation)
    {
        _hasCapture = false;
        _capturedCard = null;

        if (!ShouldRender(card) || !EnsureLoaded() || !EnsureTargets()) return;
        if (!SpriteBatchRenderTargetCompositor.TryGetPrimaryRenderTarget(
                _graphicsDevice,
                out var currentTargets,
                out var currentTarget)) return;

        var state = SpriteBatchRenderTargetCompositor.CaptureState(_graphicsDevice);
        _spriteBatch.End();
        SpriteBatchRenderTargetCompositor.Copy(_graphicsDevice, _spriteBatch, currentTarget, _beforeCardTarget);
        SpriteBatchRenderTargetCompositor.RestoreRenderTargets(_graphicsDevice, currentTargets);
        SpriteBatchRenderTargetCompositor.RestoreSpriteBatch(_graphicsDevice, _spriteBatch, state);

        _hasCapture = true;
        _capturedCard = card;
        _capturedPosition = position;
        _capturedScale = Math.Max(0.001f, scale);
        _capturedRotation = rotation;
    }

    private void EndCursedRender(Entity card)
    {
        if (!_hasCapture || _capturedCard != card)
        {
            return;
        }

        _hasCapture = false;
        _capturedCard = null;

        if (!ShouldRender(card) || _overlay == null || _beforeCardTarget == null || _afterCardTarget == null)
        {
            return;
        }

        if (!SpriteBatchRenderTargetCompositor.TryGetPrimaryRenderTarget(
                _graphicsDevice,
                out var currentTargets,
                out var currentTarget)) return;

        var state = SpriteBatchRenderTargetCompositor.CaptureState(_graphicsDevice);
        _spriteBatch.End();
        SpriteBatchRenderTargetCompositor.Copy(_graphicsDevice, _spriteBatch, currentTarget, _afterCardTarget);

        ConfigureOverlay(card, _capturedPosition, _capturedScale, _capturedRotation);
        _overlay.BackgroundTexture = _beforeCardTarget;
        SpriteBatchRenderTargetCompositor.RestoreRenderTargets(_graphicsDevice, currentTargets);
        _graphicsDevice.Clear(Color.Transparent);
        _overlay.Begin(_spriteBatch);
        _overlay.Draw(_spriteBatch, _afterCardTarget);
        _overlay.End(_spriteBatch);
        SpriteBatchRenderTargetCompositor.RestoreSpriteBatch(_graphicsDevice, _spriteBatch, state);
    }

    private void ConfigureOverlay(Entity card, Vector2 position, float scale, float rotation)
    {
        CardVisualGeometry geometry = CardGeometryService.GetVisualGeometry(
            EntityManager,
            card,
            position,
            Math.Max(0.001f, scale),
            rotation);

        float bubbleSizeMin = Math.Max(0.001f, BubbleSizeMin);
        float bubbleSizeMax = Math.Max(bubbleSizeMin, BubbleSizeMax);

        _overlay.Time = _timeSeconds;
        _overlay.CardCenter = geometry.Center;
        _overlay.CardSize = new Vector2(Math.Max(1f, geometry.Bounds.Width), Math.Max(1f, geometry.Bounds.Height));
        _overlay.CardRotation = rotation;
        _overlay.CardRadius = Math.Max(0f, CardRadius);
        _overlay.EffectSeed = EffectSeed;
        _overlay.CardShadowTint = new Vector3(CardShadowTintR, CardShadowTintG, CardShadowTintB);
        _overlay.CardSicklyTint = new Vector3(CardSicklyTintR, CardSicklyTintG, CardSicklyTintB);
        _overlay.CardDesaturation = MathHelper.Clamp(CardDesaturation, 0f, 1f);
        _overlay.CardTintStrength = MathHelper.Clamp(CardTintStrength, 0f, 1f);
        _overlay.CardEdgeDarken = Math.Max(0f, CardEdgeDarken);
        _overlay.CardCenterPreserve = Math.Max(0.001f, CardCenterPreserve);
        _overlay.PrimaryCrackScale = Math.Max(0.001f, PrimaryCrackScale);
        _overlay.SecondaryCrackScale = Math.Max(0.001f, SecondaryCrackScale);
        _overlay.HairlineCrackScale = Math.Max(0.001f, HairlineCrackScale);
        _overlay.PrimaryCrackWidth = Math.Max(0.001f, PrimaryCrackWidth);
        _overlay.SecondaryCrackWidth = Math.Max(0.001f, SecondaryCrackWidth);
        _overlay.HairlineCrackWidth = Math.Max(0.001f, HairlineCrackWidth);
        _overlay.CrackBranchCutoff = MathHelper.Clamp(CrackBranchCutoff, 0f, 1f);
        _overlay.CrackDarken = Math.Max(0f, CrackDarken);
        _overlay.CrackFlickerSpeed = Math.Max(0f, CrackFlickerSpeed);
        _overlay.CrackFlickerDepth = MathHelper.Clamp(CrackFlickerDepth, 0f, 1f);
        _overlay.CorePurple = new Vector3(CorePurpleR, CorePurpleG, CorePurpleB);
        _overlay.InnerPurple = new Vector3(InnerPurpleR, InnerPurpleG, InnerPurpleB);
        _overlay.OuterPurple = new Vector3(OuterPurpleR, OuterPurpleG, OuterPurpleB);
        _overlay.CoreBrightness = Math.Max(0f, CoreBrightness);
        _overlay.RimBrightness = Math.Max(0f, RimBrightness);
        _overlay.HaloBrightness = Math.Max(0f, HaloBrightness);
        _overlay.HaloWidth = Math.Max(0.001f, HaloWidth);
        _overlay.OozeSwellAmount = Math.Max(0f, OozeSwellAmount);
        _overlay.OozeSwirlStrength = Math.Max(0f, OozeSwirlStrength);
        _overlay.OozeFlowSpeed = Math.Max(0f, OozeFlowSpeed);
        _overlay.OozeSurfaceShine = Math.Max(0f, OozeSurfaceShine);
        _overlay.OozeEdgeShadow = Math.Max(0f, OozeEdgeShadow);
        _overlay.ArcaneSparkAmount = Math.Max(0f, ArcaneSparkAmount);
        _overlay.ArcaneSparkSpeed = Math.Max(0f, ArcaneSparkSpeed);
        _overlay.BubbleAmount = Math.Max(0f, BubbleAmount);
        _overlay.BubbleScale = Math.Max(0.001f, BubbleScale);
        _overlay.BubbleSpeed = Math.Max(0f, BubbleSpeed);
        _overlay.BubbleSizeMin = bubbleSizeMin;
        _overlay.BubbleSizeMax = bubbleSizeMax;
        _overlay.BubbleHighlight = Math.Max(0f, BubbleHighlight);
        _overlay.MistIntensity = Math.Max(0f, MistIntensity);
        _overlay.MistScale = Math.Max(0.001f, MistScale);
        _overlay.MistRiseSpeed = Math.Max(0f, MistRiseSpeed);
        _overlay.MistSideDrift = MistSideDrift;
        _overlay.MistSwirlStrength = Math.Max(0f, MistSwirlStrength);
        _overlay.CurrentOpacity = Math.Max(0f, CurrentOpacity);
        _overlay.CurrentSpeed = Math.Max(0f, CurrentSpeed);
        _overlay.VignetteStrength = Math.Max(0f, VignetteStrength);
        _overlay.GrainAmount = Math.Max(0f, GrainAmount);
        _overlay.Exposure = Math.Max(0.001f, Exposure);
        _overlay.TimeSpeed = Math.Max(0f, TimeSpeed);
    }

    private bool ShouldRender(Entity card)
    {
        return ShaderRuntimeOptions.ShadersEnabled &&
            !_failed &&
            card?.GetComponent<Cursed>() != null;
    }

    private bool HasCursedCards()
    {
        foreach (var _ in EntityManager.GetEntitiesWithComponent<Cursed>()) return true;
        return false;
    }

    private bool EnsureLoaded()
    {
        if (!ShaderRuntimeOptions.ShadersEnabled || _failed) return false;
        if (_effect == null)
        {
            try
            {
                _effect = _content.Load<Effect>("Shaders/Cursed");
            }
            catch (Exception exception)
            {
                LoggingService.Append("CursedDisplaySystem.EnsureLoaded", new JsonObject
                {
                    ["error"] = "Failed to load shader",
                    ["exception"] = exception.Message
                });
                _failed = true;
                return false;
            }
        }

        _overlay ??= new CursedOverlay(_effect);
        return _overlay.IsAvailable;
    }

    private bool EnsureTargets()
    {
        Rectangle bounds = _graphicsDevice.Viewport.Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0) return false;
        if (_beforeCardTarget != null &&
            _beforeCardTarget.Width == bounds.Width &&
            _beforeCardTarget.Height == bounds.Height &&
            _afterCardTarget != null &&
            _afterCardTarget.Width == bounds.Width &&
            _afterCardTarget.Height == bounds.Height)
        {
            return true;
        }

        _beforeCardTarget?.Dispose();
        _afterCardTarget?.Dispose();
        _beforeCardTarget = new RenderTarget2D(
            _graphicsDevice,
            bounds.Width,
            bounds.Height,
            false,
            SurfaceFormat.Color,
            DepthFormat.None);
        _afterCardTarget = new RenderTarget2D(
            _graphicsDevice,
            bounds.Width,
            bounds.Height,
            false,
            SurfaceFormat.Color,
            DepthFormat.None);
        return true;
    }
}
