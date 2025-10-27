using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems;

public class ShockwaveDisplaySystem : Core.System
{
    private readonly GraphicsDevice _gd;
    private readonly SpriteBatch _sb;
    private readonly ContentManager _content;

    private Effect _effect;
    private ShockwaveOverlay _overlay;
    private float _timeSeconds;

    private struct ActiveWave
    {
        public ShockwaveEvent Evt;
        public float StartTime;
    }

    private readonly List<ActiveWave> _waves = new();

    public bool HasActiveWaves => _waves.Count > 0;

    public ShockwaveDisplaySystem(EntityManager em, GraphicsDevice gd, SpriteBatch sb, ContentManager content)
        : base(em)
    {
        _gd = gd;
        _sb = sb;
        _content = content;
    }

    protected override IEnumerable<Entity> GetRelevantEntities() => Enumerable.Empty<Entity>();

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        _timeSeconds += MathHelper.Max(0f, (float)gameTime.ElapsedGameTime.TotalSeconds);
        if (_overlay == null) EnsureLoaded();
        if (_overlay == null) return;

        float now = _timeSeconds;
        for (int i = _waves.Count - 1; i >= 0; i--)
        {
            var w = _waves[i];
            float tNorm = (now - w.StartTime) / MathHelper.Max(1e-4f, w.Evt.DurationSec);
            if (tNorm >= 1f) _waves.RemoveAt(i);
        }
    }

    public void Emit(ShockwaveEvent e)
    {
        EnsureLoaded();
        if (_overlay == null) return;
        _waves.Add(new ActiveWave { Evt = e, StartTime = _timeSeconds });
    }

    private void EnsureLoaded()
    {
        if (_effect == null)
        {
            try { _effect = _content.Load<Effect>("Shaders/Shockwave"); }
            catch { _effect = null; }
        }
        if (_effect != null && _overlay == null)
        {
            _overlay = new ShockwaveOverlay(_effect);
        }
    }

    // Composites all active waves over sceneSrc and presents to backbuffer using ping-pong render targets
    public void Composite(Texture2D sceneSrc, RenderTarget2D ppA, RenderTarget2D ppB)
    {
        if (_overlay == null || sceneSrc == null)
        {
            // Fallback: blit original scene
            _sb.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);
            _sb.Draw(sceneSrc, _gd.Viewport.Bounds, Color.White);
            _sb.End();
            return;
        }

        // Nothing to do
        if (_waves.Count == 0)
        {
            _sb.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);
            _sb.Draw(sceneSrc, _gd.Viewport.Bounds, Color.White);
            _sb.End();
            return;
        }

        // Start with the original scene in src
        Texture2D src = sceneSrc;
        RenderTarget2D dst;
        bool useA = true;

        float now = _timeSeconds;
        foreach (var w in _waves)
        {
            dst = useA ? ppA : ppB;
            useA = !useA;

            _gd.SetRenderTarget(dst);
            _gd.Clear(Color.Black);

            float tNorm = MathHelper.Clamp((now - w.StartTime) / MathHelper.Max(1e-4f, w.Evt.DurationSec), 0f, 1f);

            _overlay.CenterPx = w.Evt.CenterPx;
            _overlay.TimeNorm = tNorm;
            _overlay.MaxRadiusPx = w.Evt.MaxRadiusPx;
            _overlay.RippleWidthPx = w.Evt.RippleWidthPx;
            _overlay.Strength = w.Evt.Strength;
            _overlay.ChromaticAberrationAmp = w.Evt.ChromaticAberrationAmp;
            _overlay.ChromaticAberrationFreq = w.Evt.ChromaticAberrationFreq;
            _overlay.ShadingIntensity = w.Evt.ShadingIntensity;

            _overlay.Begin(_sb);
            _overlay.Draw(_sb, src);
            _overlay.End(_sb);

            src = dst;
        }

        _gd.SetRenderTarget(null);
        _sb.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);
        _sb.Draw(src, _gd.Viewport.Bounds, Color.White);
        _sb.End();
    }

    protected override void UpdateEntity(Entity entity, GameTime gameTime)
    {
        throw new System.NotImplementedException();
    }
}


