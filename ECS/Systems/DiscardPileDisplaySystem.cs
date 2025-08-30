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
        public float TextScale { get; set; } = 0.8f;

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

        protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

        public void Draw()
        {
            var deckEntity = GetRelevantEntities().FirstOrDefault();
            if (deckEntity == null) return;
            var deck = deckEntity.GetComponent<Deck>();
            if (deck == null) return;

            int w = _graphicsDevice.Viewport.Width;
            int h = _graphicsDevice.Viewport.Height;

            int rectW = PanelWidth;
            int rectH = PanelHeight;
            int m = PanelMargin;
            var rect = new Rectangle(m, h - rectH - m, rectW, rectH);

            // Pulse scale factor
            float scale = 1f;
            if (_pulseTimeRemaining > 0.0)
            {
                float t = (float)(1.0 - (_pulseTimeRemaining / PulseDuration)); // 0->1
                float wave = (float)System.Math.Sin(t * System.Math.PI); // 0..1..0
                scale = 1f + PulseAmplitude * wave;
            }

            // Scale about the rect center
            var center = new Vector2(rect.Center.X, rect.Center.Y);
            int scaledW = (int)System.Math.Round(rectW * scale);
            int scaledH = (int)System.Math.Round(rectH * scale);
            var scaledRect = new Rectangle((int)(center.X - scaledW / 2f), (int)(center.Y - scaledH / 2f), scaledW, scaledH);

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

            // Ensure a clickable UI entity exists and stays in sync for hit-testing
            var clickable = EntityManager.GetEntitiesWithComponent<DiscardPileClickable>().FirstOrDefault();
            if (clickable == null)
            {
                clickable = EntityManager.CreateEntity("UIPanel_DiscardPileClickable");
                EntityManager.AddComponent(clickable, new DiscardPileClickable());
                EntityManager.AddComponent(clickable, new Transform { Position = new Vector2(scaledRect.X, scaledRect.Y), ZOrder = 10000 });
                EntityManager.AddComponent(clickable, new UIElement { Bounds = scaledRect, IsInteractable = true, Tooltip = "View Discard Pile" });
            }
            else
            {
                var ui = clickable.GetComponent<UIElement>();
                if (ui != null) ui.Bounds = scaledRect;
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

