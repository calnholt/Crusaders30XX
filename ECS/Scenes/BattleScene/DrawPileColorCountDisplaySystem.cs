using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Crusaders30XX.ECS.Singletons;
using Crusaders30XX.ECS.Services;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// Renders three vertically-stacked color-coded pills to the left of the draw pile panel,
    /// showing the count of Red, White, and Black cards currently in the draw pile.
    /// </summary>
    [DebugTab("Draw Pile Color Count")]
    public class DrawPileColorCountDisplaySystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;

        private const string RedEntityName   = "UI_ColorCount_Red";
        private const string WhiteEntityName = "UI_ColorCount_White";
        private const string BlackEntityName = "UI_ColorCount_Black";

        // Cached color counts from the last UpdateEntity pass
        private int _redCount;
        private int _whiteCount;
        private int _blackCount;

        // Shared pill texture — all three pills share the same shape, tinted per color
        private Texture2D _pillTex;
        private int _cachedPillWidth;
        private int _cachedPillHeight;
        private int _cachedCornerRadius;

        [DebugEditable(DisplayName = "Pill Width", Step = 1, Min = 10, Max = 500)]
        public int PillWidth { get; set; } = 26;

        [DebugEditable(DisplayName = "Pill Height", Step = 1, Min = 6, Max = 200)]
        public int PillHeight { get; set; } = 22;

        [DebugEditable(DisplayName = "Pill Spacing", Step = 1, Min = 0, Max = 200)]
        public int PillSpacing { get; set; } = 26;

        [DebugEditable(DisplayName = "Pill Gap", Step = 1, Min = 0, Max = 200)]
        public int PillGap { get; set; } = 8;

        [DebugEditable(DisplayName = "Corner Radius", Step = 1, Min = 0, Max = 100)]
        public int CornerRadius { get; set; } = 4;

        [DebugEditable(DisplayName = "Text Scale", Step = 0.01f, Min = 0.01f, Max = 2f)]
        public float TextScale { get; set; } = 0.11f;

        // Reference copies of DrawPileDisplaySystem values — no cross-system coupling
        [DebugEditable(DisplayName = "Draw Pile Ref Width", Step = 1, Min = 0, Max = 500)]
        public int DrawPileRefWidth { get; set; } = 60;

        [DebugEditable(DisplayName = "Draw Pile Ref Height", Step = 1, Min = 0, Max = 500)]
        public int DrawPileRefHeight { get; set; } = 80;

        [DebugEditable(DisplayName = "Draw Pile Ref Margin", Step = 1, Min = 0, Max = 500)]
        public int DrawPileRefMargin { get; set; } = 30;

        public DrawPileColorCountDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
            : base(entityManager)
        {
            _graphicsDevice = graphicsDevice;
            _spriteBatch = spriteBatch;
            _font = FontSingleton.ContentFont;
        }

        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<Deck>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
            var deck = entity.GetComponent<Deck>();
            if (deck == null) return;

            // Count each color in the draw pile
            _redCount = 0; _whiteCount = 0; _blackCount = 0;
            foreach (var cardEntity in deck.DrawPile)
            {
                var cardData = cardEntity.GetComponent<CardData>();
                if (cardData == null) continue;
                switch (cardData.Color)
                {
                    case CardData.CardColor.Red:   _redCount++;   break;
                    case CardData.CardColor.White: _whiteCount++; break;
                    case CardData.CardColor.Black: _blackCount++; break;
                }
            }

            EnsureEntities();
            UpdatePositions();
            EnsureTexture();
            UpdateUIBounds();
        }

        public void Draw()
        {
            // Top to bottom: Red, White, Black
            DrawPill(RedEntityName,   Color.Red,             Color.White, _redCount.ToString());
            DrawPill(WhiteEntityName, Color.White,           Color.Black, _whiteCount.ToString());
            DrawPill(BlackEntityName, new Color(20, 20, 20), Color.White, _blackCount.ToString());
        }

        private void DrawPill(string entityName, Color bgColor, Color textColor, string text)
        {
            var entity = EntityManager.GetEntity(entityName);
            var t = entity?.GetComponent<Transform>();
            if (t == null || _pillTex == null) return;

            var rect = GetPillRect(t);

            // Draw pill background tinted to the pill color
            _spriteBatch.Draw(_pillTex, rect, bgColor);

            // Draw centered count text
            if (_font == null) return;
            var size = _font.MeasureString(text) * TextScale;
            var pos = new Vector2(rect.Center.X - size.X / 2f, rect.Center.Y - size.Y / 2f);
            _spriteBatch.DrawString(_font, text, pos, textColor, 0f, Vector2.Zero, TextScale, SpriteEffects.None, 0f);
        }

        private void UpdateUIBounds()
        {
            UpdateUIBoundsForEntity(RedEntityName);
            UpdateUIBoundsForEntity(WhiteEntityName);
            UpdateUIBoundsForEntity(BlackEntityName);
        }

        private void UpdateUIBoundsForEntity(string entityName)
        {
            var entity = EntityManager.GetEntity(entityName);
            var t = entity?.GetComponent<Transform>();
            if (t == null) return;

            var rect = GetPillRect(t);
            var ui = entity.GetComponent<UIElement>();
            if (ui == null)
                EntityManager.AddComponent(entity, new UIElement { Bounds = rect, IsInteractable = false });
            else
                ui.Bounds = rect;
        }

        private Rectangle GetPillRect(Transform t) =>
            new Rectangle(
                (int)Math.Round(t.Position.X - PillWidth / 2f),
                (int)Math.Round(t.Position.Y - PillHeight / 2f),
                PillWidth,
                PillHeight);

        private void EnsureEntities()
        {
            EnsureEntity(RedEntityName);
            EnsureEntity(WhiteEntityName);
            EnsureEntity(BlackEntityName);
        }

        private void EnsureEntity(string name)
        {
            if (EntityManager.GetEntity(name) != null) return;
            var e = EntityManager.CreateEntity(name);
            EntityManager.AddComponent(e, new Transform { Position = Vector2.Zero, ZOrder = 10000 });
            EntityManager.AddComponent(e, ParallaxLayer.GetUIParallaxLayer());
        }

        private void UpdatePositions()
        {
            int vw = Game1.VirtualWidth;
            int vh = Game1.VirtualHeight;

            // Anchor to the left edge of the draw pile panel, centered vertically on it
            float stackCenterX = vw - DrawPileRefWidth - DrawPileRefMargin - PillGap - PillWidth / 2f;
            float stackCenterY = vh - DrawPileRefHeight / 2f - DrawPileRefMargin;

            SetPosition(RedEntityName,   new Vector2(stackCenterX, stackCenterY - PillSpacing));
            SetPosition(WhiteEntityName, new Vector2(stackCenterX, stackCenterY));
            SetPosition(BlackEntityName, new Vector2(stackCenterX, stackCenterY + PillSpacing));
        }

        private void SetPosition(string name, Vector2 position)
        {
            var t = EntityManager.GetEntity(name)?.GetComponent<Transform>();
            if (t != null) t.Position = position;
        }

        private void EnsureTexture()
        {
            if (_pillTex != null &&
                _cachedPillWidth    == PillWidth &&
                _cachedPillHeight   == PillHeight &&
                _cachedCornerRadius == CornerRadius)
                return;

            _pillTex?.Dispose();
            _pillTex = RoundedRectTextureFactory.CreateRoundedRect(_graphicsDevice, PillWidth, PillHeight, CornerRadius);
            _cachedPillWidth    = PillWidth;
            _cachedPillHeight   = PillHeight;
            _cachedCornerRadius = CornerRadius;
        }
    }
}
