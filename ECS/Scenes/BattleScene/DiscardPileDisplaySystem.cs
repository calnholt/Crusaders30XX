using System;
using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// Renders a discard pile rectangle at bottom-left with the current discard pile count.
    /// Clicking opens the generic card list modal with the discard pile contents.
    /// </summary>
    [DebugTab("Discard Pile Display")]
    public class DiscardPileDisplaySystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;
        private readonly Texture2D _pixel;
		private const string RootEntityName = "UI_DiscardPileRoot";
		private int _lastViewportW = -1;
		private int _lastViewportH = -1;
        private double _pulseTimeRemaining = 0.0;
        private const double PulseDuration = 0.15; // seconds
        private const float PulseAmplitude = 0.12f; // 12% size bump at peak

        [DebugEditable(DisplayName = "Panel Width", Step = 1, Min = 10, Max = 2000)]
        public int PanelWidth { get; set; } = 60;
        [DebugEditable(DisplayName = "Panel Height", Step = 1, Min = 10, Max = 2000)]
        public int PanelHeight { get; set; } = 80;
        [DebugEditable(DisplayName = "Panel Margin", Step = 1, Min = 0, Max = 500)]
        public int PanelMargin { get; set; } = 20;
        [DebugEditable(DisplayName = "Text Scale", Step = 0.05f, Min = 0.1f, Max = 10f)]
        public float TextScale { get; set; } = 0.2f;

        public DiscardPileDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, SpriteFont font)
            : base(entityManager)
        {
            _graphicsDevice = graphicsDevice;
            _spriteBatch = spriteBatch;
            _font = font;
            _pixel = new Texture2D(graphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });
            EventManager.Subscribe<CardMoved>(OnCardMoved);
            System.Console.WriteLine("[DiscardPileDisplaySystem] Subscribed to CardMoved");
        }

        protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<Deck>();
        }

        public override void Update(GameTime gameTime)
        {
            if (_pulseTimeRemaining > 0.0)
            {
                _pulseTimeRemaining = System.Math.Max(0.0, _pulseTimeRemaining - gameTime.ElapsedGameTime.TotalSeconds);
            }
            base.Update(gameTime);
        }

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			EnsureRootEntity();
			int w = _graphicsDevice.Viewport.Width;
			int h = _graphicsDevice.Viewport.Height;
			if (w != _lastViewportW || h != _lastViewportH)
			{
				_lastViewportW = w;
				_lastViewportH = h;
				var root = EntityManager.GetEntity(RootEntityName);
				var t = root?.GetComponent<Transform>();
				if (t != null)
				{
					int rectW = PanelWidth;
					int rectH = PanelHeight;
					int m = PanelMargin;
					var center = new Vector2(rectW / 2f + m, h - rectH / 2f - m);
					t.Position = center;
				}
			}
		}

        public void Draw()
        {
            var deckEntity = GetRelevantEntities().FirstOrDefault();
            if (deckEntity == null) return;
            var deck = deckEntity.GetComponent<Deck>();
            if (deck == null) return;

			var root = EntityManager.GetEntity(RootEntityName);
			var tRoot = root?.GetComponent<Transform>();
			if (tRoot == null) return;

			int rectW = PanelWidth;
            int rectH = PanelHeight;
			var center = new Vector2(tRoot.Position.X, tRoot.Position.Y);
			var rect = new Rectangle(
				(int)System.Math.Round(center.X - rectW / 2f),
				(int)System.Math.Round(center.Y - rectH / 2f),
				rectW,
				rectH);

            // Pulse scale factor
            float scale = 1f;
            if (_pulseTimeRemaining > 0.0)
            {
                float t = (float)(1.0 - (_pulseTimeRemaining / PulseDuration)); // 0->1
                float wave = (float)System.Math.Sin(t * System.Math.PI); // 0..1..0
                scale = 1f + PulseAmplitude * wave;
            }

            // Scale about the rect center
            var center2 = new Vector2(rect.Center.X, rect.Center.Y);
            int scaledW = (int)System.Math.Round(rectW * scale);
            int scaledH = (int)System.Math.Round(rectH * scale);
            var scaledRect = new Rectangle((int)(center2.X - scaledW / 2f), (int)(center2.Y - scaledH / 2f), scaledW, scaledH);

            // Panel
            _spriteBatch.Draw(_pixel, scaledRect, new Color(20, 20, 20) * 0.75f);
            // Border
            DrawBorder(scaledRect, Color.White, 2);

            // Count text centered (scale pulses with panel)
            if (_font != null)
            {
                string text = deck.DiscardPile.Count.ToString();
                float textScale = TextScale * scale;
                var size = _font.MeasureString(text) * textScale;
                var pos = new Vector2(scaledRect.Center.X - size.X / 2f, scaledRect.Center.Y - size.Y / 2f);
                _spriteBatch.DrawString(_font, text, pos, Color.White, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
            }

            // Use root entity UIElement for hit-testing and tooltips; mark with component for InputSystem routing
            var rootUi = root.GetComponent<UIElement>();
			if (rootUi == null)
			{
                EntityManager.AddComponent(root, new UIElement { Bounds = scaledRect, IsInteractable = true, Tooltip = "View Discard Pile" });
                EntityManager.AddComponent(root, new DiscardPileClickable());
			}
			else
			{
				rootUi.Bounds = scaledRect;
				rootUi.IsInteractable = true;
				rootUi.Tooltip = "View Discard Pile";
                if (root.GetComponent<DiscardPileClickable>() == null)
                {
                    EntityManager.AddComponent(root, new DiscardPileClickable());
                }
			}
        }

        private void EnsureRootEntity()
        {
            var e = EntityManager.GetEntity(RootEntityName);
            if (e == null)
            {
                e = EntityManager.CreateEntity(RootEntityName);
                int h = _graphicsDevice.Viewport.Height;
                int rectW = PanelWidth;
                int rectH = PanelHeight;
                int m = PanelMargin;
                var center = new Vector2(rectW / 2f + m, h - rectH / 2f - m);
                EntityManager.AddComponent(e, new Transform { Position = center, ZOrder = 10000 });
                EntityManager.AddComponent(e, ParallaxLayer.GetUIParallaxLayer());
            }
        }

        private void DrawBorder(Rectangle r, Color color, int thickness)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(r.X, r.Y, r.Width, thickness), color);
            _spriteBatch.Draw(_pixel, new Rectangle(r.X, r.Bottom - thickness, r.Width, thickness), color);
            _spriteBatch.Draw(_pixel, new Rectangle(r.X, r.Y, thickness, r.Height), color);
            _spriteBatch.Draw(_pixel, new Rectangle(r.Right - thickness, r.Y, thickness, r.Height), color);
        }

        private void OnCardMoved(CardMoved evt)
        {
            if (evt.To == CardZoneType.DiscardPile)
            {
                _pulseTimeRemaining = PulseDuration;
            }
        }
    }
}

