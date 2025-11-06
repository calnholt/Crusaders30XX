using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems;

[DebugTab("Poison System")]
public class PoisonDamageDisplaySystem : Core.System
{
    private readonly GraphicsDevice _gd;
    private readonly SpriteBatch _sb;
    private readonly ContentManager _content;

    private Effect _effect;
    private PoisonOverlay _overlay;
    private float _timeSeconds;

    private struct ActivePoison
    {
        public PoisonDamageEvent Evt;
        public float StartTime;
    }

    private ActivePoison? _activePoison;

    public bool HasActivePoison => _activePoison.HasValue;

    public PoisonDamageDisplaySystem(EntityManager em, GraphicsDevice gd, SpriteBatch sb, ContentManager content)
        : base(em)
    {
        _gd = gd;
        _sb = sb;
        _content = content;
        EventManager.Subscribe<PoisonDamageEvent>(OnPoisonDamageEvent);
    }

    protected override IEnumerable<Entity> GetRelevantEntities() => Enumerable.Empty<Entity>();

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        _timeSeconds += MathHelper.Max(0f, (float)gameTime.ElapsedGameTime.TotalSeconds);
        if (_overlay == null) EnsureLoaded();
        if (_overlay == null) return;

        if (_activePoison.HasValue)
        {
            var p = _activePoison.Value;
            float tNorm = (_timeSeconds - p.StartTime) / MathHelper.Max(1e-4f, p.Evt.DurationSec);
            if (tNorm >= 1f)
            {
                _activePoison = null;
            }
        }
    }

    private void OnPoisonDamageEvent(PoisonDamageEvent e)
    {
        EnsureLoaded();
        if (_overlay == null) return;
        // Single poison effect - replaces previous
        _activePoison = new ActivePoison { Evt = e, StartTime = _timeSeconds };
    }

    private void EnsureLoaded()
    {
        if (_effect == null)
        {
            try { _effect = _content.Load<Effect>("Shaders/Poison"); }
            catch { _effect = null; }
        }
        if (_effect != null && _overlay == null)
        {
            _overlay = new PoisonOverlay(_effect);
        }
    }

    // Composites poison effect over sceneSrc and outputs to finalTarget (null = backbuffer)
    public void Composite(Texture2D sceneSrc, RenderTarget2D tempOutput, RenderTarget2D finalTarget = null)
    {
        if (_overlay == null || sceneSrc == null || !_activePoison.HasValue)
        {
            // Fallback: blit original scene directly to finalTarget
            _gd.SetRenderTarget(finalTarget);
            _sb.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);
            _sb.Draw(sceneSrc, _gd.Viewport.Bounds, Color.White);
            _sb.End();
            return;
        }

        var p = _activePoison.Value;
        float tNorm = MathHelper.Clamp((_timeSeconds - p.StartTime) / MathHelper.Max(1e-4f, p.Evt.DurationSec), 0f, 1f);

        // Apply event overrides to overlay, or use defaults
        _overlay.TimeNorm = tNorm;
        if (p.Evt.AttackDuration.HasValue) _overlay.AttackDuration = p.Evt.AttackDuration.Value;
        if (p.Evt.DecayRate.HasValue) _overlay.DecayRate = p.Evt.DecayRate.Value;
        if (p.Evt.ShakeFrequency.HasValue) _overlay.ShakeFrequency = p.Evt.ShakeFrequency.Value;
        if (p.Evt.ShakeAmplitude.HasValue) _overlay.ShakeAmplitude = p.Evt.ShakeAmplitude.Value;
        if (p.Evt.WaveFrequency.HasValue) _overlay.WaveFrequency = p.Evt.WaveFrequency.Value;
        if (p.Evt.WaveSpeed.HasValue) _overlay.WaveSpeed = p.Evt.WaveSpeed.Value;
        if (p.Evt.WaveAmplitude.HasValue) _overlay.WaveAmplitude = p.Evt.WaveAmplitude.Value;
        if (p.Evt.VignetteStart.HasValue) _overlay.VignetteStart = p.Evt.VignetteStart.Value;
        if (p.Evt.VignetteIntensity.HasValue) _overlay.VignetteIntensity = p.Evt.VignetteIntensity.Value;
        if (p.Evt.PoisonTint.HasValue) _overlay.PoisonTint = p.Evt.PoisonTint.Value;
        if (p.Evt.PoisonMixAmount.HasValue) _overlay.PoisonMixAmount = p.Evt.PoisonMixAmount.Value;
        if (p.Evt.DesaturationAmount.HasValue) _overlay.DesaturationAmount = p.Evt.DesaturationAmount.Value;

        // Render poison effect to temp output
        _gd.SetRenderTarget(tempOutput);
        _gd.Clear(Color.Black);

        _overlay.Begin(_sb);
        _overlay.Draw(_sb, sceneSrc);
        _overlay.End(_sb);

        // Present result to finalTarget (backbuffer if null)
        _gd.SetRenderTarget(finalTarget);
        _sb.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);
        _sb.Draw(tempOutput, _gd.Viewport.Bounds, Color.White);
        _sb.End();
    }

    protected override void UpdateEntity(Entity entity, GameTime gameTime)
    {
        throw new System.NotImplementedException();
    }

    [DebugAction("Trigger Poison")]
    public void Debug_TriggerPoison()
    {
        var e = new PoisonDamageEvent
        {
            DurationSec = 2.0f
            // Use default visual parameters from PoisonOverlay
        };
        EventManager.Publish(e);
    }
}

