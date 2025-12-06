using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Scenes.BattleScene
{
    [DebugTab("Bloodshot Display")]
    public class BloodshotDisplaySystem : Core.System
    {
        private readonly GraphicsDevice _gd;
        private readonly SpriteBatch _sb;
        private readonly ContentManager _content;
        
        private Effect _effect;
        private BloodshotOverlay _overlay;
        private float _timeSeconds;
        private bool _isActive;

        [DebugEditable(DisplayName = "Active")]
        public bool DebugIsActive
        {
            get => _isActive;
            set => _isActive = value;
        }

        public BloodshotDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, ContentManager content) 
            : base(entityManager)
        {
            _gd = graphicsDevice;
            _sb = spriteBatch;
            _content = content;

            EventManager.Subscribe<ChangeBattlePhaseEvent>(OnPhaseChanged);
        }

        private void OnPhaseChanged(ChangeBattlePhaseEvent evt)
        {
            _isActive = evt.Current == SubPhase.EnemyStart || evt.Current == SubPhase.EnemyAttack || evt.Current == SubPhase.EnemyEnd || evt.Current == SubPhase.Block || evt.Current == SubPhase.PreBlock;
        }

        public void LoadContent()
        {
            EnsureLoaded();
        }

        private void EnsureLoaded()
        {
            if (_effect == null)
            {
                try 
                {
                    _effect = _content.Load<Effect>("Shaders/Bloodshot");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[BloodshotDisplaySystem] Failed to load shader: {e.Message}");
                    _effect = null;
                }
            }
            if (_effect != null && _overlay == null)
            {
                _overlay = new BloodshotOverlay(_effect);
            }
        }

        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return Enumerable.Empty<Entity>();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            _timeSeconds += MathHelper.Max(0f, (float)gameTime.ElapsedGameTime.TotalSeconds);
            if (_overlay == null) EnsureLoaded();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

        public new bool IsActive()
        {
            return _isActive;
        }

        /// <summary>
        /// Composites the bloodshot effect over the source texture.
        /// </summary>
        /// <param name="sceneSrc">The source texture (usually the scene render target)</param>
        /// <param name="tempOutput">A temporary render target to draw the effect into</param>
        /// <param name="finalTarget">The final destination (null for backbuffer)</param>
        public void Composite(Texture2D sceneSrc, RenderTarget2D tempOutput, RenderTarget2D finalTarget = null)
        {
            if (_overlay == null || sceneSrc == null || !_isActive)
            {
                // Fallback: blit original scene directly to finalTarget
                _gd.SetRenderTarget(finalTarget);
                _sb.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);
                _sb.Draw(sceneSrc, _gd.Viewport.Bounds, Color.White);
                _sb.End();
                return;
            }

            // Update Time for shader animation
            _overlay.Time = _timeSeconds;

            // Render bloodshot effect to temp output
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

        [DebugAction("Toggle Bloodshot")]
        public void Debug_ToggleBloodshot()
        {
            _isActive = !_isActive;
            Console.WriteLine($"[BloodshotDisplaySystem] Bloodshot toggled: {_isActive}");
        }
    }
}
