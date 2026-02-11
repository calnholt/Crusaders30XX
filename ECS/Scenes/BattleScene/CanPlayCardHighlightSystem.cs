using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Factories;
using Crusaders30XX.ECS.Objects.Cards;
using Crusaders30XX.ECS.Objects.EnemyAttacks;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Rendering;
using Crusaders30XX.ECS.Services;
using Crusaders30XX.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Scenes.BattleScene
{
    [DebugTab("Can Play Highlight")]
    public class CanPlayCardHighlightSystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly Dictionary<(int w, int h, int r), Texture2D> _roundedRectCache = new();
        private double _totalSeconds;

        public CanPlayCardHighlightSystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
            : base(entityManager)
        {
            _graphicsDevice = graphicsDevice;
            _spriteBatch = spriteBatch;
            EventManager.Subscribe<DeleteCachesEvent>(_ => _roundedRectCache.Clear());
        }

        protected override IEnumerable<Entity> GetRelevantEntities() => Array.Empty<Entity>();
        protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

        public override void Update(GameTime gameTime)
        {
            _totalSeconds = gameTime.TotalGameTime.TotalSeconds;
            base.Update(gameTime);
        }

        public void Draw()
        {
            // Get current phase
            var phaseEntity = EntityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault();
            if (phaseEntity == null) return;
            var phase = phaseEntity.GetComponent<PhaseState>();
            if (phase.Sub != SubPhase.Action && phase.Sub != SubPhase.Block) return;

            // Get hand
            var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
            var deck = deckEntity?.GetComponent<Deck>();
            if (deck == null || deck.Hand.Count == 0) return;

            // Get settings
            var settingsEntity = EntityManager.GetEntitiesWithComponent<CanPlayHighlightSettings>().FirstOrDefault();
            var hs = settingsEntity?.GetComponent<CanPlayHighlightSettings>() ?? new CanPlayHighlightSettings();

            // Get card visual settings for bounds/radius
            var cvEntity = EntityManager.GetEntitiesWithComponent<CardVisualSettings>().FirstOrDefault();
            var cvs = cvEntity?.GetComponent<CardVisualSettings>();
            int cornerRadius = cvs?.CardCornerRadius ?? 18;
            int borderThickness = cvs?.HighlightBorderThickness ?? 5;

            // Pulse animation (shared across all playable cards)
            float pulse01 = (float)(Math.Cos(_totalSeconds * hs.GlowPulseSpeed) * 0.5 + 0.5);
            float eased = (float)Math.Pow(MathHelper.Clamp(pulse01, 0f, 1f), hs.GlowEasingPower);
            float pulseAmount = MathHelper.Lerp(
                MathHelper.Clamp(hs.GlowMinIntensity, 0f, 1f),
                MathHelper.Clamp(hs.GlowMaxIntensity, 0f, 1f),
                eased);
            Color glowColor = new Color((byte)hs.GlowColorR, (byte)hs.GlowColorG, (byte)hs.GlowColorB);

            // Pre-gather data needed for action phase checks
            Entity player = null;
            ActionPoints ap = null;
            AppliedPassives appliedPassives = null;
            List<Entity> costEligibleCards = null;

            if (phase.Sub == SubPhase.Action)
            {
                player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
                ap = player?.GetComponent<ActionPoints>();
                appliedPassives = GetComponentHelper.GetAppliedPassives(EntityManager, "Player");
                costEligibleCards = BuildCostEligibleCards(deck);
            }

            // Check for active attack intent during block phase
            bool hasActiveAttack = false;
            if (phase.Sub == SubPhase.Block)
            {
                var enemy = EntityManager.GetEntitiesWithComponent<AttackIntent>().FirstOrDefault();
                var pa = enemy?.GetComponent<AttackIntent>()?.Planned?.FirstOrDefault();
                hasActiveAttack = pa != null && !string.IsNullOrEmpty(pa.ContextId);
            }

            foreach (var cardEntity in deck.Hand)
            {
                // Skip cards that are animating or assigned as block
                if (cardEntity.GetComponent<AnimatingHandToDiscard>() != null) continue;
                if (cardEntity.GetComponent<AnimatingHandToZone>() != null) continue;
                if (cardEntity.GetComponent<AnimatingHandToDrawPile>() != null) continue;
                if (cardEntity.GetComponent<AssignedBlockCard>() != null) continue;
                if (cardEntity.GetComponent<SelectedForPayment>() != null) continue;

                var data = cardEntity.GetComponent<CardData>();
                if (data == null) continue;

                bool canPlay = false;
                if (phase.Sub == SubPhase.Action)
                    canPlay = IsPlayableInAction(cardEntity, data, ap, appliedPassives, costEligibleCards);
                else if (phase.Sub == SubPhase.Block)
                    canPlay = IsPlayableInBlock(cardEntity, data, hasActiveAttack);

                if (!canPlay) continue;

                var t = cardEntity.GetComponent<Transform>();
                if (t == null) continue;

                int cw = cvs?.CardWidth ?? 250;
                int ch = cvs?.CardHeight ?? 350;
                int offsetYExtra = cvs?.CardOffsetYExtra ?? (int)Math.Round((cvs?.UIScale ?? 1f) * 25);
                var cardRect = new Rectangle(
                    (int)t.Position.X - cw / 2,
                    (int)t.Position.Y - (ch / 2 + offsetYExtra),
                    cw,
                    ch);

                if (cardRect.Width <= 1 || cardRect.Height <= 1) continue;

                float rotation = t.Rotation;

                DrawGlow(cardRect, rotation, cornerRadius, borderThickness, hs, pulseAmount, glowColor);
            }
        }

        // --- Action phase playability check ---
        private bool IsPlayableInAction(Entity cardEntity, CardData data, ActionPoints ap, AppliedPassives appliedPassives, List<Entity> costEligibleCards)
        {
            var card = data.Card;

            // Relics can't be played
            if (card.Type == CardType.Relic) return false;

            // Need AP unless free action
            if (!card.IsFreeAction)
            {
                int currentAp = ap?.Current ?? 0;
                if (currentAp <= 0) return false;
            }

            // Card-specific CanPlay check (now pure bool, safe per-frame)
            if (card.CanPlay != null && !card.CanPlay(EntityManager, cardEntity)) return false;

            // Silenced + pledged can't play
            if (cardEntity.HasComponent<Pledge>() && appliedPassives != null)
            {
                appliedPassives.Passives.TryGetValue(AppliedPassiveType.Silenced, out int silencedStacks);
                if (silencedStacks > 0) return false;
            }

            // Check discard costs can be satisfied
            var costs = card.Cost;
            if (costs != null && costs.Count > 0)
            {
                // Build available cards excluding self
                var available = costEligibleCards.Where(c => c != cardEntity).ToList();
                if (!CanSatisfyCosts(costs, available)) return false;
            }

            return true;
        }

        // --- Block phase playability check ---
        private bool IsPlayableInBlock(Entity cardEntity, CardData data, bool hasActiveAttack)
        {
            if (!hasActiveAttack) return false;

            var card = data.Card;

            // Weapons can't block
            if (card.IsWeapon) return false;

            // Tokens can't block
            if (card.IsToken) return false;

            // Intimidated cards can't block
            if (cardEntity.GetComponent<Intimidated>() != null) return false;

            // Pledged cards can't block
            if (cardEntity.GetComponent<Pledge>() != null) return false;

            // Cards marked as cannot block this attack
            if (cardEntity.GetComponent<CannotBlockThisAttack>() != null) return false;

            // Block-type cards need CanPlay check (e.g., Stalwart courage cost)
            if (card.Type == CardType.Block && card.CanPlay != null && !card.CanPlay(EntityManager, cardEntity))
                return false;

            return true;
        }

        // --- Build list of cards eligible to pay costs (excludes weapons, yellow, pledged, sealed) ---
        private List<Entity> BuildCostEligibleCards(Deck deck)
        {
            var result = new List<Entity>();
            foreach (var c in deck.Hand)
            {
                var cd = c.GetComponent<CardData>();
                if (cd == null) continue;
                if (cd.Color == CardData.CardColor.Yellow) continue;
                if (cd.Card.IsWeapon) continue;
                if (c.GetComponent<Pledge>() != null) continue;
                if (c.GetComponent<Sealed>() != null) continue;
                result.Add(c);
            }
            return result;
        }

        // --- Greedy cost satisfaction (mirrors CardPlaySystem.CanSatisfy) ---
        private bool CanSatisfyCosts(List<string> requiredCosts, List<Entity> candidates)
        {
            var remaining = new List<string>(requiredCosts);
            var used = new HashSet<Entity>();

            // Match specific colors first
            foreach (var e in candidates)
            {
                if (remaining.Count == 0) break;
                var cd = e.GetComponent<CardData>();
                if (cd == null) continue;
                int idx = remaining.FindIndex(r =>
                    (r == "Red" && cd.Color == CardData.CardColor.Red) ||
                    (r == "White" && cd.Color == CardData.CardColor.White) ||
                    (r == "Black" && cd.Color == CardData.CardColor.Black));
                if (idx >= 0)
                {
                    used.Add(e);
                    remaining.RemoveAt(idx);
                }
            }

            // Then satisfy Any with remaining cards
            foreach (var e in candidates)
            {
                if (remaining.Count == 0) break;
                if (used.Contains(e)) continue;
                int idx = remaining.FindIndex(r => r == "Any");
                if (idx >= 0)
                {
                    used.Add(e);
                    remaining.RemoveAt(idx);
                }
            }

            return remaining.Count == 0;
        }

        // --- Draw glow layers around a card rect ---
        private void DrawGlow(Rectangle bounds, float rotation, int cornerRadius, int borderThickness, CanPlayHighlightSettings hs, float pulseAmount, Color glowColor)
        {
            int th = borderThickness;
            var highlightRect = new Rectangle(
                bounds.X - th,
                bounds.Y - th,
                bounds.Width + th * 2,
                bounds.Height + th * 2);

            int radius = Math.Max(0, cornerRadius + th);
            var baseTex = GetRoundedRectTexture(highlightRect.Width, highlightRect.Height, radius);
            var center = new Vector2(highlightRect.X + highlightRect.Width / 2f, highlightRect.Y + highlightRect.Height / 2f);

            int layers = hs.GlowLayers;
            float spread = hs.GlowSpread;

            for (int i = layers; i >= 1; i--)
            {
                float spreadAnim = 1f + hs.GlowSpreadAmplitude * (float)Math.Sin(_totalSeconds * hs.GlowSpreadSpeed);
                float scale = 1f + i * spread * spreadAnim;
                float layerAlpha = MathHelper.Clamp(pulseAmount * (0.22f / i), 0f, hs.MaxAlpha);
                _spriteBatch.Draw(
                    baseTex,
                    position: center,
                    sourceRectangle: null,
                    color: glowColor * layerAlpha,
                    rotation: rotation,
                    origin: new Vector2(baseTex.Width / 2f, baseTex.Height / 2f),
                    scale: new Vector2(scale, scale),
                    effects: SpriteEffects.None,
                    layerDepth: 0f);
            }
        }

        private Texture2D GetRoundedRectTexture(int width, int height, int radius)
        {
            var key = (width, height, radius);
            if (_roundedRectCache.TryGetValue(key, out var tex)) return tex;
            var texture = RoundedRectTextureFactory.CreateRoundedRect(_graphicsDevice, width, height, radius);
            _roundedRectCache[key] = texture;
            return texture;
        }
    }
}
