using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Objects.Cards;
using Crusaders30XX.ECS.Rendering;
using Crusaders30XX.ECS.Singletons;
using Crusaders30XX.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace Crusaders30XX.ECS.Systems
{
    [DebugTab("Card Display V2")]
    public class CardDisplaySystemV2 : CardDisplayBase
    {
        private SpriteFont _bodyFont = FontSingleton.ChakraPetchFont;

        // Toggle
        [DebugEditable(DisplayName = "Use V2 Card Display")]
        public bool UseV2 { get => CardDisplayToggle.UseV2; set => CardDisplayToggle.UseV2 = value; }

        // Card Frame
        [DebugEditable(DisplayName = "V2 Card Width", Step = 1, Min = 100, Max = 400)]
        public int V2CardWidth { get; set; } = 268;
        [DebugEditable(DisplayName = "V2 Card Height", Step = 1, Min = 100, Max = 600)]
        public int V2CardHeight { get; set; } = 377;
        [DebugEditable(DisplayName = "V2 Corner Radius", Step = 1, Min = 0, Max = 32)]
        public int V2CornerRadius { get; set; } = 10;

        // Stripe
        [DebugEditable(DisplayName = "Stripe Width", Step = 1, Min = 0, Max = 30)]
        public int StripeWidth { get; set; } = 5;

        // Stat Gutter
        [DebugEditable(DisplayName = "Gutter X", Step = 1, Min = 0, Max = 60)]
        public int GutterX { get; set; } = 6;
        [DebugEditable(DisplayName = "Gutter Width", Step = 1, Min = 10, Max = 120)]
        public int GutterWidth { get; set; } = 55;

        // Type Notch
        [DebugEditable(DisplayName = "Notch Corner Radius", Step = 1, Min = 0, Max = 20)]
        public int NotchCornerRadius { get; set; } = 8;
        [DebugEditable(DisplayName = "Notch Pad Top", Step = 1, Min = 0, Max = 20)]
        public int NotchPadTop { get; set; } = 5;
        [DebugEditable(DisplayName = "Notch Pad Right", Step = 1, Min = 0, Max = 30)]
        public int NotchPadRight { get; set; } = 10;
        [DebugEditable(DisplayName = "Notch Pad Bottom", Step = 1, Min = 0, Max = 20)]
        public int NotchPadBot { get; set; } = 5;
        [DebugEditable(DisplayName = "Notch Pad Left", Step = 1, Min = 0, Max = 30)]
        public int NotchPadLeft { get; set; } = 14;
        [DebugEditable(DisplayName = "Notch Font Scale", Step = 0.01f, Min = 0.05f, Max = 1.0f)]
        public float NotchFontScale { get; set; } = 0.08f;

        // Chip Layout
        [DebugEditable(DisplayName = "Chip Size", Step = 1, Min = 20, Max = 80)]
        public int ChipSize { get; set; } = 42;
        [DebugEditable(DisplayName = "Chip Corner Radius", Step = 1, Min = 0, Max = 16)]
        public int ChipCornerRadius { get; set; } = 4;
        [DebugEditable(DisplayName = "Chip Column X", Step = 1, Min = 0, Max = 60)]
        public int ChipColumnX { get; set; } = 14;
        [DebugEditable(DisplayName = "Chip Column Top Y", Step = 1, Min = 0, Max = 60)]
        public int ChipColumnTopY { get; set; } = 14;
        [DebugEditable(DisplayName = "Chip Slot Height", Step = 1, Min = 40, Max = 100)]
        public int ChipSlotHeight { get; set; } = 60;
        [DebugEditable(DisplayName = "Chip Border Thickness", Step = 1, Min = 1, Max = 6)]
        public int ChipBorderThickness { get; set; } = 3;
        [DebugEditable(DisplayName = "Chip Value Font Scale", Step = 0.01f, Min = 0.05f, Max = 1.0f)]
        public float ChipValueFontScale { get; set; } = 0.16f;
        [DebugEditable(DisplayName = "Chip Label Font Scale", Step = 0.01f, Min = 0.03f, Max = 0.5f)]
        public float ChipLabelFontScale { get; set; } = 0.03f;

        // Delta Slab
        [DebugEditable(DisplayName = "Slab Width", Step = 1, Min = 20, Max = 80)]
        public int SlabWidth { get; set; } = 42;
        [DebugEditable(DisplayName = "Slab Height", Step = 1, Min = 8, Max = 30)]
        public int SlabHeight { get; set; } = 16;
        [DebugEditable(DisplayName = "Slab Corner Radius", Step = 1, Min = 0, Max = 16)]
        public int SlabCornerRadius { get; set; } = 4;
        [DebugEditable(DisplayName = "Slab Font Scale", Step = 0.01f, Min = 0.03f, Max = 0.5f)]
        public float SlabFontScale { get; set; } = 0.08f;

        // Content Area
        [DebugEditable(DisplayName = "Content Margin Left", Step = 1, Min = 30, Max = 120)]
        public int ContentMarginLeft { get; set; } = 65;
        [DebugEditable(DisplayName = "Content Pad Top", Step = 1, Min = 0, Max = 40)]
        public int ContentPadTop { get; set; } = 31;
        [DebugEditable(DisplayName = "Content Pad Right", Step = 1, Min = 0, Max = 40)]
        public int ContentPadRight { get; set; } = 14;

        // Cost Pips
        [DebugEditable(DisplayName = "V2 Pip Diameter", Step = 1, Min = 6, Max = 30)]
        public int V2PipDiameter { get; set; } = 13;
        [DebugEditable(DisplayName = "V2 Pip Gap", Step = 1, Min = 0, Max = 20)]
        public int V2PipGap { get; set; } = 5;

        // Name
        [DebugEditable(DisplayName = "V2 Name Font Scale", Step = 0.01f, Min = 0.05f, Max = 1.0f)]
        public float V2NameFontScale { get; set; } = 0.11f;
        [DebugEditable(DisplayName = "V2 Name Margin Bottom", Step = 1, Min = 0, Max = 20)]
        public int V2NameMarginBottom { get; set; } = 6;

        // Rule Line
        [DebugEditable(DisplayName = "Rule Height", Step = 1, Min = 1, Max = 6)]
        public int RuleHeight { get; set; } = 2;
        [DebugEditable(DisplayName = "Rule Margin Bottom", Step = 1, Min = 0, Max = 20)]
        public int RuleMarginBottom { get; set; } = 8;

        // Description
        [DebugEditable(DisplayName = "V2 Desc Font Scale", Step = 0.01f, Min = 0.03f, Max = 0.5f)]
        public float V2DescFontScale { get; set; } = 0.11f;

        // Art
        [DebugEditable(DisplayName = "V2 Art Width", Step = 1, Min = 50, Max = 300)]
        public int V2ArtWidth { get; set; } = 191;
        [DebugEditable(DisplayName = "V2 Art Height", Step = 1, Min = 50, Max = 300)]
        public int V2ArtHeight { get; set; } = 124;
        [DebugEditable(DisplayName = "V2 Art Offset Right", Step = 1, Min = -60, Max = 60)]
        public int V2ArtOffsetRight { get; set; } = -15;
        [DebugEditable(DisplayName = "V2 Art Offset Bottom", Step = 1, Min = -60, Max = 60)]
        public int V2ArtOffsetBottom { get; set; } = -10;

        // AP Chip Y position
        [DebugEditable(DisplayName = "AP Chip Y", Step = 1, Min = 200, Max = 340)]
        public int APChipY { get; set; } = 284;

        // Color Palettes
        private static readonly Dictionary<CardData.CardColor, Color> BgColors = new()
        {
            { CardData.CardColor.White, new Color(220, 215, 206) },
            { CardData.CardColor.Red,   new Color(28, 12, 12) },
            { CardData.CardColor.Black, new Color(19, 19, 19) },
        };

        private static readonly Dictionary<CardData.CardColor, Color> StripeColors = new()
        {
            { CardData.CardColor.White, new Color(153, 153, 153) },
            { CardData.CardColor.Red,   new Color(204, 34, 34) },
            { CardData.CardColor.Black, Color.Black },
        };

        private static readonly Dictionary<CardData.CardColor, Color> GutterColors = new()
        {
            { CardData.CardColor.White, Color.Black * 0.05f },
            { CardData.CardColor.Red,   Color.Black * 0.22f },
            { CardData.CardColor.Black, Color.White * 0.025f },
        };

        private static readonly Dictionary<CardData.CardColor, Color> NotchBgColors = new()
        {
            { CardData.CardColor.White, Color.Black * 0.06f },
            { CardData.CardColor.Red,   new Color(200, 30, 30) * 0.15f },
            { CardData.CardColor.Black, Color.White * 0.04f },
        };

        private static readonly Dictionary<CardData.CardColor, Color> NotchTextColors = new()
        {
            { CardData.CardColor.White, new Color(153, 153, 153) },
            { CardData.CardColor.Red,   new Color(136, 68, 51) },
            { CardData.CardColor.Black, new Color(85, 85, 85) },
        };

        private static readonly Dictionary<CardData.CardColor, Color> CostPipColors = new()
        {
            { CardData.CardColor.White, new Color(160, 152, 136) },
            { CardData.CardColor.Red,   new Color(102, 102, 102) },
            { CardData.CardColor.Black, new Color(85, 85, 85) },
        };

        private static readonly Dictionary<CardData.CardColor, Color> NameTextColors = new()
        {
            { CardData.CardColor.White, new Color(26, 26, 26) },
            { CardData.CardColor.Red,   new Color(240, 224, 216) },
            { CardData.CardColor.Black, new Color(232, 228, 224) },
        };

        private static readonly Dictionary<CardData.CardColor, Color> RuleLineColors = new()
        {
            { CardData.CardColor.White, new Color(192, 184, 170) },
            { CardData.CardColor.Red,   new Color(68, 32, 32) },
            { CardData.CardColor.Black, new Color(51, 51, 51) },
        };

        private static readonly Dictionary<CardData.CardColor, Color> DescTextColors = new()
        {
            { CardData.CardColor.White, new Color(68, 68, 68) },
            { CardData.CardColor.Red,   new Color(204, 153, 136) },
            { CardData.CardColor.Black, new Color(153, 153, 153) },
        };

        public CardDisplaySystemV2(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, ContentManager content)
            : base(entityManager, graphicsDevice, spriteBatch, content)
        {
            EventManager.Subscribe<CardRenderEvent>(OnCardRenderEvent);
            EventManager.Subscribe<CardRenderScaledEvent>(OnCardRenderScaledEvent);
            EventManager.Subscribe<CardRenderScaledRotatedEvent>(OnCardRenderScaledRotatedEvent);
        }

        private float CW => V2CardWidth;
        private float CH => V2CardHeight;

        private void V2Rect(Vector2 cc, float rot, Vector2 off, float w, float h, Color c, float s)
            => DrawRectangleRotatedLocalScaled(cc, rot, off, w, h, c, s, CW, CH);

        private void V2Tex(Vector2 cc, float rot, Vector2 off, Texture2D tex, Vector2 sz, Color c, float s)
            => DrawTextureRotatedLocalScaled(cc, rot, off, tex, sz, c, s, CW, CH);

        private void V2Text(Vector2 cc, float rot, Vector2 off, string txt, Color c, float sc, float os, SpriteFont font = null)
            => DrawCardTextRotatedSingleScaled(cc, rot, off, txt, c, sc, os, CW, CH, font);

        private void V2TextWrapped(Vector2 cc, float rot, Vector2 off, string txt, Color c, float sc, float os, SpriteFont font, float maxW)
            => DrawCardTextWrappedRotatedScaled(cc, rot, off, txt, c, sc, os, font, maxW, CW, CH);

        // Event handlers
        private void OnCardRenderEvent(CardRenderEvent evt)
        {
            if (!CardDisplayToggle.UseV2) return;
            var t = evt.Card.GetComponent<Transform>();
            var ui = evt.Card.GetComponent<UIElement>();
            EventManager.Publish(new HighlightRenderEvent { Entity = evt.Card, Transform = t, UI = ui });
            DrawCardV2(evt.Card, evt.Position);
        }

        private void OnCardRenderScaledEvent(CardRenderScaledEvent evt)
        {
            if (!CardDisplayToggle.UseV2) return;
            var transform = evt.Card.GetComponent<Transform>();
            Vector2 originalScale = transform?.Scale ?? Vector2.One;
            if (transform != null)
            {
                transform.Scale = new Vector2(evt.Scale, evt.Scale);
                float originalRotation = transform.Rotation;
                Vector2 originalPosition = transform.Position;
                transform.Rotation = 0f;
                transform.Position = evt.Position;
                var t = evt.Card.GetComponent<Transform>();
                var ui = evt.Card.GetComponent<UIElement>();
                EventManager.Publish(new HighlightRenderEvent { Entity = evt.Card, Transform = t, UI = ui });
                DrawCardV2(evt.Card, evt.Position);
                transform.Scale = originalScale;
                transform.Rotation = originalRotation;
                transform.Position = originalPosition;
                if (ui != null) ui.Bounds = GetCardVisualRect(evt.Position, evt.Scale, V2CardWidth, V2CardHeight);
            }
            else
            {
                var t = evt.Card.GetComponent<Transform>();
                var ui = evt.Card.GetComponent<UIElement>();
                EventManager.Publish(new HighlightRenderEvent { Entity = evt.Card, Transform = t, UI = ui });
                DrawCardV2(evt.Card, evt.Position);
            }
        }

        private void OnCardRenderScaledRotatedEvent(CardRenderScaledRotatedEvent evt)
        {
            if (!CardDisplayToggle.UseV2) return;
            var transform = evt.Card.GetComponent<Transform>();
            Vector2 originalScale = transform?.Scale ?? Vector2.One;
            if (transform != null)
            {
                transform.Scale = new Vector2(evt.Scale, evt.Scale);
                Vector2 originalPosition = transform.Position;
                transform.Position = evt.Position;
                var t = evt.Card.GetComponent<Transform>();
                var ui = evt.Card.GetComponent<UIElement>();
                EventManager.Publish(new HighlightRenderEvent { Entity = evt.Card, Transform = t, UI = ui });
                DrawCardV2(evt.Card, evt.Position);
                transform.Scale = originalScale;
                transform.Position = originalPosition;
                if (ui != null) ui.Bounds = GetCardVisualRect(evt.Position, evt.Scale, V2CardWidth, V2CardHeight);
            }
            else
            {
                var t2 = evt.Card.GetComponent<Transform>();
                var ui2 = evt.Card.GetComponent<UIElement>();
                EventManager.Publish(new HighlightRenderEvent { Entity = evt.Card, Transform = t2, UI = ui2 });
                DrawCardV2(evt.Card, evt.Position);
            }
        }

        public void DrawCardV2(Entity entity, Vector2 position)
        {
            var cardData = entity.GetComponent<CardData>();
            var transform = entity.GetComponent<Transform>();
            if (cardData == null) return;

            float visualScale = transform?.Scale.X ?? 1f;
            float rotation = transform?.Rotation ?? 0f;
            CardBase card = cardData.Card;
            bool hasDef = card != null;
            var cc = cardData.Color;

            // Compute card center
            var rect = GetCardVisualRect(position, visualScale, V2CardWidth, V2CardHeight);
            var cardCenter = new Vector2(rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f);

            float sw = V2CardWidth * visualScale;
            float sh = V2CardHeight * visualScale;

            // 1. Background
            int bgW = (int)Math.Round(V2CardWidth * visualScale);
            int bgH = (int)Math.Round(V2CardHeight * visualScale);
            var bgTex = GetRoundedRectTexture(bgW, bgH, (int)(V2CornerRadius * visualScale));
            var bgColor = GetPaletteColor(BgColors, cc, new Color(220, 215, 206));
            _spriteBatch.Draw(bgTex,
                position: cardCenter,
                sourceRectangle: null,
                color: bgColor,
                rotation: rotation,
                origin: new Vector2(bgTex.Width / 2f, bgTex.Height / 2f),
                scale: Vector2.One,
                effects: SpriteEffects.None,
                layerDepth: 0f);

            // 2. Stripe
            var stripeColor = GetPaletteColor(StripeColors, cc, new Color(153, 153, 153));
            V2Rect(cardCenter, rotation, new Vector2(0, 0), StripeWidth * visualScale, sh, stripeColor, visualScale);

            // 3. Stat Gutter
            var gutterColor = GetPaletteColor(GutterColors, cc, Color.Black * 0.05f);
            V2Rect(cardCenter, rotation, new Vector2(GutterX * visualScale, 0), GutterWidth * visualScale, sh, gutterColor, visualScale);

            // 4. Type Notch
            if (hasDef)
            {
                DrawTypeNotch(cardCenter, rotation, visualScale, cc, card.Type);
            }

            // 5. Stat Chips
            if (hasDef)
            {
                DrawStatChips(cardCenter, rotation, visualScale, cc, entity, card);
            }

            // 6. Cost Pips
            if (hasDef)
            {
                var costs = card.Cost.ToArray();
                DrawV2CostPips(cardCenter, rotation, visualScale, cc, costs);
            }

            // Track Y cursor for content area
            float contentX = ContentMarginLeft * visualScale;
            float contentWidth = (V2CardWidth - ContentMarginLeft - ContentPadRight) * visualScale;
            float cursorY = ContentPadTop * visualScale;

            // Add pip height offset
            if (hasDef && card.Cost.Count > 0)
            {
                cursorY += (V2PipDiameter + 4) * visualScale;
            }

            // 7. Card Name
            if (hasDef)
            {
                string name = card.Name ?? "";
                var nameColor = GetPaletteColor(NameTextColors, cc, new Color(26, 26, 26));
                V2Text(cardCenter, rotation, new Vector2(contentX, cursorY), name, nameColor, V2NameFontScale * visualScale, visualScale, _nameFont);
                var nameSize = _nameFont.MeasureString(name) * (V2NameFontScale * visualScale);
                cursorY += nameSize.Y + V2NameMarginBottom * visualScale;
            }

            // 8. Rule Line
            var ruleColor = GetPaletteColor(RuleLineColors, cc, new Color(192, 184, 170));
            V2Rect(cardCenter, rotation, new Vector2(contentX, cursorY), contentWidth, RuleHeight * visualScale, ruleColor, visualScale);
            cursorY += RuleHeight * visualScale + RuleMarginBottom * visualScale;

            // 9. Description
            if (hasDef)
            {
                string desc = card.Text ?? "";
                var descColor = GetPaletteColor(DescTextColors, cc, new Color(68, 68, 68));
                V2TextWrapped(cardCenter, rotation, new Vector2(contentX, cursorY), desc, descColor, V2DescFontScale * visualScale, visualScale, _bodyFont, contentWidth);
            }

            // 10. Card Art
            if (hasDef && !string.IsNullOrEmpty(card.CardId))
            {
                var artTex = GetOrLoadTexture($"CardArt/{card.CardId}");
                if (artTex != null)
                {
                    float artW = V2ArtWidth * visualScale;
                    float artH = V2ArtHeight * visualScale;

                    // Preserve aspect ratio
                    float texAspect = artTex.Width / (float)artTex.Height;
                    float boxAspect = artW / artH;
                    if (texAspect > boxAspect) { artH = artW / texAspect; }
                    else { artW = artH * texAspect; }

                    float artLocalX = sw - artW + V2ArtOffsetRight * visualScale;
                    float artLocalY = sh - artH + V2ArtOffsetBottom * visualScale;
                    V2Tex(cardCenter, rotation, new Vector2(artLocalX, artLocalY), artTex, new Vector2(artW, artH), Color.White, visualScale);
                }
            }
        }

        private void DrawTypeNotch(Vector2 cardCenter, float rotation, float vs, CardData.CardColor cc, CardType type)
        {
            string typeLabel = type switch
            {
                CardType.Attack => "ATTACK",
                CardType.Prayer => "PRAYER",
                CardType.Block => "BLOCK",
                CardType.Relic => "RELIC",
                _ => "CARD"
            };

            var textSize = _bodyFont.MeasureString(typeLabel) * (NotchFontScale * vs);
            int notchW = (int)(NotchPadLeft * vs + textSize.X + NotchPadRight * vs);
            int notchH = (int)(NotchPadTop * vs + textSize.Y + NotchPadBot * vs);

            // Per-corner rounded rect: top-left 0, top-right NotchCornerRadius, bottom-right 0, bottom-left NotchCornerRadius
            int r = (int)(NotchCornerRadius * vs);
            var notchTex = GetPerCornerRoundedRectTexture(notchW, notchH, 0, r, 0, r);
            var notchBg = GetPaletteColor(NotchBgColors, cc, Color.Black * 0.06f);

            float notchLocalX = V2CardWidth * vs - notchW;
            float notchLocalY = 0;

            V2Tex(cardCenter, rotation, new Vector2(notchLocalX, notchLocalY), notchTex, new Vector2(notchW, notchH), notchBg, vs);

            var textColor = GetPaletteColor(NotchTextColors, cc, new Color(153, 153, 153));
            float textLocalX = notchLocalX + NotchPadLeft * vs;
            float textLocalY = notchLocalY + NotchPadTop * vs;
            V2Text(cardCenter, rotation, new Vector2(textLocalX, textLocalY), typeLabel, textColor, NotchFontScale * vs, vs, _bodyFont);
        }

        private void DrawStatChips(Vector2 cardCenter, float rotation, float vs, CardData.CardColor cc, Entity entity, CardBase card)
        {
            bool isW = cc == CardData.CardColor.White;
            float chipX = ChipColumnX * vs;

            // BLK chip (slot 0)
            int blockValue = BlockValueService.GetTotalBlockValue(entity);
            int printedBlock = card.Block;
            int blockDelta = blockValue - printedBlock;
            if (blockValue > 0 && !card.IsWeapon)
            {
                float chipY = ChipColumnTopY * vs;
                bool hasSlab = blockDelta != 0;
                DrawChip(cardCenter, rotation, vs, cc, chipX, chipY, blockValue.ToString(), "BLK", ChipVariant.BLK, hasSlab);
                if (hasSlab)
                {
                    float slabY = chipY + ChipSize * vs;
                    DrawDeltaSlab(cardCenter, rotation, vs, cc, chipX, slabY, blockDelta);
                }
            }

            // ATK chip (slot 1)
            if (card.Type == CardType.Attack)
            {
                int damage = GetEffectiveDamage(entity, card);
                int damageDelta = damage - card.Damage;
                float chipY = (ChipColumnTopY + ChipSlotHeight) * vs;
                bool hasSlab = damageDelta != 0;
                DrawChip(cardCenter, rotation, vs, cc, chipX, chipY, damage.ToString(), "ATK", ChipVariant.ATK, hasSlab);
                if (hasSlab)
                {
                    float slabY = chipY + ChipSize * vs;
                    DrawDeltaSlab(cardCenter, rotation, vs, cc, chipX, slabY, damageDelta);
                }
            }

            // AP / FREE chip (bottom slot)
            float apY = APChipY * vs;
            bool isFree = card.IsFreeAction || card.Type == CardType.Block;
            if (isFree)
            {
                DrawChip(cardCenter, rotation, vs, cc, chipX, apY, "0", "FREE", ChipVariant.FREE, false);
            }
            else
            {
                DrawChip(cardCenter, rotation, vs, cc, chipX, apY, "1", "AP", ChipVariant.AP, false);
            }
        }

        private enum ChipVariant { BLK, ATK, AP, FREE }

        private void DrawChip(Vector2 cardCenter, float rotation, float vs, CardData.CardColor cc, float x, float y, string value, string label, ChipVariant variant, bool hasSlab)
        {
            float chipW = ChipSize * vs;
            float chipH = ChipSize * vs;
            int cr = (int)(ChipCornerRadius * vs);
            bool isW = cc == CardData.CardColor.White;

            // Determine corner radii: if slab, bottom corners are 0
            int rTL = cr, rTR = cr, rBR = hasSlab ? 0 : cr, rBL = hasSlab ? 0 : cr;

            switch (variant)
            {
                case ChipVariant.BLK:
                {
                    // Bordered chip: draw border, then inner fill
                    Color borderColor = isW ? new Color(153, 153, 153) : new Color(119, 119, 119);
                    Color bgColor = isW ? (Color.Black * 0.08f) : (new Color(80, 80, 80) * 0.15f);
                    Color valColor = isW ? new Color(85, 85, 85) : new Color(187, 187, 187);
                    Color lblColor = isW ? new Color(119, 119, 119) : new Color(136, 136, 136);

                    var outerTex = GetPerCornerRoundedRectTexture((int)chipW, (int)chipH, rTL, rTR, rBR, rBL);
                    V2Tex(cardCenter, rotation, new Vector2(x, y), outerTex, new Vector2(chipW, chipH), borderColor, vs);

                    int bt = (int)(ChipBorderThickness * vs);
                    int innerRTL = Math.Max(0, rTL - bt), innerRTR = Math.Max(0, rTR - bt);
                    int innerRBR = Math.Max(0, rBR - bt), innerRBL = Math.Max(0, rBL - bt);
                    float innerW = chipW - bt * 2;
                    float innerH = chipH - bt * 2;
                    if (innerW > 0 && innerH > 0)
                    {
                        var innerTex = GetPerCornerRoundedRectTexture((int)innerW, (int)innerH, innerRTL, innerRTR, innerRBR, innerRBL);
                        V2Tex(cardCenter, rotation, new Vector2(x + bt, y + bt), innerTex, new Vector2(innerW, innerH), bgColor, vs);
                    }

                    DrawChipText(cardCenter, rotation, vs, x, y, chipW, chipH, value, label, valColor, lblColor);
                    break;
                }
                case ChipVariant.ATK:
                {
                    Color bgColor = new Color(204, 34, 34);
                    Color valColor = Color.White;
                    Color lblColor = new Color(255, 204, 187);

                    var tex = GetPerCornerRoundedRectTexture((int)chipW, (int)chipH, rTL, rTR, rBR, rBL);
                    V2Tex(cardCenter, rotation, new Vector2(x, y), tex, new Vector2(chipW, chipH), bgColor, vs);
                    DrawChipText(cardCenter, rotation, vs, x, y, chipW, chipH, value, label, valColor, lblColor);
                    break;
                }
                case ChipVariant.AP:
                {
                    Color bgColor = isW ? new Color(68, 68, 68) : (cc == CardData.CardColor.Red ? new Color(51, 14, 14) : new Color(30, 30, 30));
                    Color valColor = isW ? new Color(221, 221, 221) : new Color(221, 68, 51);
                    Color lblColor = isW ? new Color(170, 170, 170) : new Color(153, 51, 34);

                    var tex = GetPerCornerRoundedRectTexture((int)chipW, (int)chipH, rTL, rTR, rBR, rBL);
                    V2Tex(cardCenter, rotation, new Vector2(x, y), tex, new Vector2(chipW, chipH), bgColor, vs);
                    DrawChipText(cardCenter, rotation, vs, x, y, chipW, chipH, value, label, valColor, lblColor);
                    break;
                }
                case ChipVariant.FREE:
                {
                    // Dashed border - approximate with solid border for now, using thin lines
                    Color borderColor = isW ? new Color(170, 170, 170) : new Color(136, 51, 34);
                    Color valColor = isW ? new Color(136, 136, 136) : new Color(204, 85, 68);
                    Color lblColor = isW ? new Color(170, 170, 170) : new Color(136, 51, 34);

                    // Draw thin border then transparent inner to get an outline effect
                    var outerTex = GetPerCornerRoundedRectTexture((int)chipW, (int)chipH, rTL, rTR, rBR, rBL);
                    V2Tex(cardCenter, rotation, new Vector2(x, y), outerTex, new Vector2(chipW, chipH), borderColor, vs);

                    int bt = (int)(ChipBorderThickness * vs);
                    float innerW = chipW - bt * 2;
                    float innerH = chipH - bt * 2;
                    if (innerW > 0 && innerH > 0)
                    {
                        // Use card background color for inner fill to simulate outline-only
                        var innerBg = GetPaletteColor(BgColors, cc, new Color(220, 215, 206));
                        var innerTex = GetPerCornerRoundedRectTexture((int)innerW, (int)innerH,
                            Math.Max(0, rTL - bt), Math.Max(0, rTR - bt),
                            Math.Max(0, rBR - bt), Math.Max(0, rBL - bt));
                        V2Tex(cardCenter, rotation, new Vector2(x + bt, y + bt), innerTex, new Vector2(innerW, innerH), innerBg, vs);
                    }

                    DrawChipText(cardCenter, rotation, vs, x, y, chipW, chipH, value, label, valColor, lblColor);
                    break;
                }
            }
        }

        private void DrawChipText(Vector2 cardCenter, float rotation, float vs, float x, float y, float chipW, float chipH, string value, string label, Color valColor, Color lblColor)
        {
            // Value text centered in upper portion of chip
            var valSize = _nameFont.MeasureString(value) * (ChipValueFontScale * vs);
            float valX = x + (chipW - valSize.X) / 2f;
            float valY = y + (chipH * 0.15f);
            V2Text(cardCenter, rotation, new Vector2(valX, valY), value, valColor, ChipValueFontScale * vs, vs, _nameFont);

            // Label text centered in lower portion
            var lblSize = _bodyFont.MeasureString(label) * (ChipLabelFontScale * vs);
            float lblX = x + (chipW - lblSize.X) / 2f;
            float lblY = y + chipH - lblSize.Y - (chipH * 0.1f);
            V2Text(cardCenter, rotation, new Vector2(lblX, lblY), label, lblColor, ChipLabelFontScale * vs, vs, _bodyFont);
        }

        private void DrawDeltaSlab(Vector2 cardCenter, float rotation, float vs, CardData.CardColor cc, float x, float y, int delta)
        {
            bool isW = cc == CardData.CardColor.White;
            bool isPositive = delta > 0;

            float slabW = SlabWidth * vs;
            float slabH = SlabHeight * vs;
            int r = (int)(SlabCornerRadius * vs);

            // Bottom corners only
            var slabTex = GetPerCornerRoundedRectTexture((int)slabW, (int)slabH, 0, 0, r, r);

            Color bgColor;
            Color textColor;
            if (isPositive)
            {
                bgColor = isW ? new Color(42, 138, 42) : new Color(26, 90, 26);
                textColor = isW ? Color.White : new Color(94, 255, 94);
            }
            else
            {
                bgColor = isW ? new Color(122, 98, 16) : new Color(90, 66, 0);
                textColor = isW ? Color.White : new Color(255, 204, 68);
            }

            V2Tex(cardCenter, rotation, new Vector2(x, y), slabTex, new Vector2(slabW, slabH), bgColor, vs);

            string deltaText = delta > 0 ? $"+{delta}" : delta.ToString();
            var textSize = _bodyFont.MeasureString(deltaText) * (SlabFontScale * vs);
            float textX = x + (slabW - textSize.X) / 2f;
            float textY = y + (slabH - textSize.Y) / 2f;
            V2Text(cardCenter, rotation, new Vector2(textX, textY), deltaText, textColor, SlabFontScale * vs, vs, _bodyFont);
        }

        private void DrawV2CostPips(Vector2 cardCenter, float rotation, float vs, CardData.CardColor cc, string[] costs)
        {
            if (costs == null || costs.Length == 0) return;

            float diameter = V2PipDiameter * vs;
            float radius = diameter / 2f;
            float gap = V2PipGap * vs;
            float startX = ContentMarginLeft * vs;
            float pipY = ContentPadTop * vs + radius;

            var pipColor = GetPaletteColor(CostPipColors, cc, new Color(160, 152, 136));

            for (int i = 0; i < costs.Length; i++)
            {
                float pipX = startX + i * (diameter + gap) + radius;
                // All pips use the card-variant color (solid fill, no outline)
                DrawCirclePipRotatedScaled(cardCenter, rotation, new Vector2(pipX, pipY), radius, pipColor, null, vs, CW, CH);
            }
        }

        private int GetEffectiveDamage(Entity entity, CardBase card)
        {
            try
            {
                int baseDamage = Math.Max(0, card.Damage);
                try
                {
                    baseDamage = Math.Max(0, baseDamage + card.GetConditionalDamage(EntityManager, entity) + AttackDamageValueService.GetTotalDelta(entity));
                }
                catch { baseDamage = Math.Max(0, card.Damage); }

                int finalDamage = baseDamage;
                try
                {
                    var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
                    var enemy = EntityManager.GetEntitiesWithComponent<Enemy>().FirstOrDefault();
                    if (player != null && enemy != null)
                    {
                        var preview = new ModifyHpRequestEvent
                        {
                            Source = player,
                            Target = enemy,
                            Delta = -baseDamage,
                            DamageType = ModifyTypeEnum.Attack
                        };
                        int passiveDelta = AppliedPassivesService.GetPassiveDelta(preview, ReadOnly: true);
                        finalDamage = Math.Max(0, -(preview.Delta + passiveDelta));
                    }
                }
                catch { finalDamage = baseDamage; }

                return Math.Max(0, finalDamage);
            }
            catch
            {
                return Math.Max(0, card.Damage);
            }
        }

        private static Color GetPaletteColor(Dictionary<CardData.CardColor, Color> palette, CardData.CardColor cc, Color fallback)
        {
            return palette.TryGetValue(cc, out var c) ? c : fallback;
        }
    }
}
