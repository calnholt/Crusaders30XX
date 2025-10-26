using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Crusaders30XX.ECS.Rendering;

public class CircularMaskOverlay
{
    private readonly Effect _effect;
    private readonly Texture2D _whitePixel;
    private readonly Texture2D _noiseTex;

    public bool IsAvailable => _effect != null;

    public float RadiusPx { get; set; } = 300f;
    public float FeatherPx { get; set; } = 6f;
    public Vector2 CenterPx { get; set; } = Vector2.Zero; // legacy single-center
    public IReadOnlyList<Vector2> CentersPx { get; set; } = Array.Empty<Vector2>();
    public float GlobalAlphaMin { get; set; } = 0.6f;
    public float GlobalAlphaMax { get; set; } = 0.75f;
    public float EaseSpeed { get; set; } = 0.5f; // cycles per second
    public float TimeSeconds { get; set; } = 0f;
    public float DistortAmplitudePx { get; set; } = 8f;
    public float DistortSpatialFreq { get; set; } = 0.005f;
    public float DistortSpeed { get; set; } = 0.5f;
    public float CameraOriginYPx { get; set; } = 0f;

    // Domain-warp parameters
    public float NoiseScale { get; set; } = 0.004f;
    public float WarpAmountPx { get; set; } = 12f;
    public float WarpSpeed { get; set; } = 0.7f;

    public CircularMaskOverlay(GraphicsDevice device, Effect effect)
    {
        _effect = effect;
        _whitePixel = new Texture2D(device, 1, 1, false, SurfaceFormat.Color);
        _whitePixel.SetData(new[] { Color.White });
        // Small tileable noise texture for domain warp (RGBA random)
        var rng = new Random(1337);
        int n = 256;
        var noiseData = new Color[n * n];
        for (int i = 0; i < n * n; i++)
        {
            byte r = (byte)rng.Next(256);
            byte g = (byte)rng.Next(256);
            byte b = (byte)rng.Next(256);
            noiseData[i] = new Color(r, g, b, (byte)255);
        }
        _noiseTex = new Texture2D(device, n, n, false, SurfaceFormat.Color);
        _noiseTex.SetData(noiseData);
    }

    public void Begin(SpriteBatch spriteBatch)
    {
        if (_effect == null) return;
        _effect.CurrentTechnique = _effect.Techniques["SpriteDrawing"];

        Viewport vp = spriteBatch.GraphicsDevice.Viewport;
        Matrix projection = Matrix.CreateOrthographicOffCenter(0, vp.Width, vp.Height, 0, 0, 1);

        var pMatrix = _effect.Parameters["MatrixTransform"]; if (pMatrix != null) pMatrix.SetValue(projection);
        var pViewport = _effect.Parameters["ViewportSize"]; if (pViewport != null) pViewport.SetValue(new Vector2(vp.Width, vp.Height));
        var pFeather = _effect.Parameters["FeatherPx"]; if (pFeather != null) pFeather.SetValue(FeatherPx);
        var pTime = _effect.Parameters["iTime"]; if (pTime != null) pTime.SetValue(TimeSeconds);
        var pEaseSpeed = _effect.Parameters["EaseSpeed"]; if (pEaseSpeed != null) pEaseSpeed.SetValue(EaseSpeed);
        var pGMin = _effect.Parameters["GlobalAlphaMin"]; if (pGMin != null) pGMin.SetValue(GlobalAlphaMin);
        var pGMax = _effect.Parameters["GlobalAlphaMax"]; if (pGMax != null) pGMax.SetValue(GlobalAlphaMax);
        var pDAmp = _effect.Parameters["DistortAmplitudePx"]; if (pDAmp != null) pDAmp.SetValue(DistortAmplitudePx);
        var pDFreq = _effect.Parameters["DistortSpatialFreq"]; if (pDFreq != null) pDFreq.SetValue(DistortSpatialFreq);
        var pDSpeed = _effect.Parameters["DistortSpeed"]; if (pDSpeed != null) pDSpeed.SetValue(DistortSpeed);
        var pCamY = _effect.Parameters["CameraOriginYPx"]; if (pCamY != null) pCamY.SetValue(CameraOriginYPx);

        // Domain-warp parameters
        var pNoiseScale = _effect.Parameters["NoiseScale"]; if (pNoiseScale != null) pNoiseScale.SetValue(NoiseScale);
        var pWarpAmt = _effect.Parameters["WarpAmountPx"]; if (pWarpAmt != null) pWarpAmt.SetValue(WarpAmountPx);
        var pWarpSpeed = _effect.Parameters["WarpSpeed"]; if (pWarpSpeed != null) pWarpSpeed.SetValue(WarpSpeed);

        // Prefer multi-mask path when CentersPx is set; fall back to single center otherwise
        int num = CentersPx?.Count ?? 0;
        var pNum = _effect.Parameters["NumMasks"]; if (pNum != null) pNum.SetValue(num);
        if (num > 0)
        {
            var pCenters = _effect.Parameters["MaskCenters"]; if (pCenters != null) pCenters.SetValue(CentersPx.ToArray());
            // Use a common radius for all masks unless the shader later supports per-POI radii
            var radii = Enumerable.Repeat(RadiusPx, num).ToArray();
            var pRadii = _effect.Parameters["MaskRadii"]; if (pRadii != null) pRadii.SetValue(radii);
        }
        else
        {
            var pCenter = _effect.Parameters["MaskCenterPx"]; if (pCenter != null) pCenter.SetValue(CenterPx);
            var pRadius = _effect.Parameters["MaskRadiusPx"]; if (pRadius != null) pRadius.SetValue(RadiusPx);
        }

        spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.LinearClamp,
            DepthStencilState.None,
            RasterizerState.CullNone,
            _effect
        );

        // Bind noise texture to sampler1
        var pNoiseTex = _effect.Parameters["NoiseTex"]; if (pNoiseTex != null) pNoiseTex.SetValue(_noiseTex);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (_effect == null) return;
        Rectangle bounds = spriteBatch.GraphicsDevice.Viewport.Bounds;
        spriteBatch.Draw(_whitePixel, bounds, Color.White);
    }

    // Draws a provided scene texture full-screen, which the shader will warp/darken outside holes
    public void Draw(SpriteBatch spriteBatch, Texture2D sceneTexture)
    {
        if (_effect == null) return;
        if (sceneTexture == null) return;
        Rectangle bounds = spriteBatch.GraphicsDevice.Viewport.Bounds;
        spriteBatch.Draw(sceneTexture, bounds, Color.White);
    }

    public void End(SpriteBatch spriteBatch)
    {
        if (_effect == null) return;
        spriteBatch.End();
    }
}


