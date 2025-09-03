using System;
using System.Linq;
using System.Collections.Generic;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Crusaders30XX.Diagnostics;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// Renders and manages the overlay used to pay color costs by discarding cards.
    /// </summary>
    [DebugTab("Pay Cost Overlay")] 
    public class PayCostOverlaySystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;
        private readonly Texture2D _pixel;

        [DebugEditable(DisplayName = "Fade In (s)", Step = 0.05f, Min = 0.01f, Max = 1.0f)]
        public float FadeInDurationSec { get; set; } = 0.25f;

        [DebugEditable(DisplayName = "Overlay Alpha", Step = 0.05f, Min = 0.1f, Max = 1.0f)]
        public float OverlayAlpha { get; set; } = 0.5f; // 0..1

        [DebugEditable(DisplayName = "Staged Scale", Step = 0.05f, Min = 0.5f, Max = 2.0f)]
        public float StagedCardScale { get; set; } = 1.1f;
        
        [DebugEditable(DisplayName = "Staged Move (s)", Step = 0.05f, Min = 0.01f, Max = 1.0f)]
        public float StagedMoveDurationSec { get; set; } = 0.25f;
        [DebugEditable(DisplayName = "Staged Return (s)", Step = 0.05f, Min = 0.01f, Max = 1.0f)]
        public float StagedReturnDurationSec { get; set; } = 0.1f;

        [DebugEditable(DisplayName = "Text Scale", Step = 0.05f, Min = 0.5f, Max = 2.0f)]
        public float TextScale { get; set; } = 1.0f;

        [DebugEditable(DisplayName = "Text Offset X", Step = 1f, Min = -1000f, Max = 1000f)]
        public float TextOffsetX { get; set; } = 0f;

        [DebugEditable(DisplayName = "Text Offset Y", Step = 1f, Min = -1000f, Max = 1000f)]
        public float TextOffsetY { get; set; } = -375f;

        [DebugEditable(DisplayName = "Card Offset X", Step = 1f, Min = -2000f, Max = 2000f)]
        public float CardOffsetX { get; set; } = 0f;

        [DebugEditable(DisplayName = "Card Offset Y", Step = 1f, Min = -2000f, Max = 2000f)]
        public float CardOffsetY { get; set; } = 0f;

        public PayCostOverlaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, SpriteFont font)
            : base(entityManager)
        {
            _graphicsDevice = graphicsDevice;
            _spriteBatch = spriteBatch;
            _font = font;
            _pixel = new Texture2D(graphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            EventManager.Subscribe<OpenPayCostOverlayEvent>(OnOpen);
            EventManager.Subscribe<ClosePayCostOverlayEvent>(_ => Close());
            EventManager.Subscribe<PayCostCandidateClicked>(OnCandidateClicked);
            EventManager.Subscribe<PayCostCancelRequested>(_ => Close());
        }

        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<PayCostOverlayState>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
            var state = entity.GetComponent<PayCostOverlayState>();
            if (state == null) return;
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (state.IsOpen)
            {
                state.OpenElapsedSeconds += dt;
                state.StagedMoveElapsedSeconds += dt;
            }
            if (state.IsReturning)
            {
                state.ReturnElapsedSeconds += dt;
                if (state.ReturnElapsedSeconds >= StagedMoveDurationSec)
                {
                    // End of return tween; fully close overlay and clear staged flags
                    state.IsReturning = false;
                    state.IsOpen = false;
                    state.CardToPlay = null;
                    state.RequiredCosts.Clear();
                    state.SelectedCards.Clear();
                    state.OpenElapsedSeconds = 0f;
                    state.StagedMoveElapsedSeconds = 0f;
                    state.ReturnElapsedSeconds = 0f;
                    state.OriginalHandIndex = -1;
                }
            }
        }

        private void EnsureStateEntityExists()
        {
            var e = EntityManager.GetEntitiesWithComponent<PayCostOverlayState>().FirstOrDefault();
            if (e == null)
            {
                e = EntityManager.CreateEntity("PayCostOverlay");
                EntityManager.AddComponent(e, new PayCostOverlayState { IsOpen = false });
            }
            // Ensure a cancel button entity exists (bounds updated in Draw)
            var cancel = EntityManager.GetEntitiesWithComponent<PayCostCancelButton>().FirstOrDefault();
            if (cancel == null)
            {
                cancel = EntityManager.CreateEntity("PayCostOverlay_Cancel");
                EntityManager.AddComponent(cancel, new Transform { Position = Vector2.Zero, ZOrder = 20000 });
                EntityManager.AddComponent(cancel, new UIElement { Bounds = new Rectangle(0, 0, 1, 1), IsInteractable = true, Tooltip = "Cancel" });
                EntityManager.AddComponent(cancel, new PayCostCancelButton());
            }
        }

        private void OnOpen(OpenPayCostOverlayEvent evt)
        {
            if (evt == null || evt.CardToPlay == null) return;
            EnsureStateEntityExists();
            var e = EntityManager.GetEntitiesWithComponent<PayCostOverlayState>().FirstOrDefault();
            var state = e.GetComponent<PayCostOverlayState>();
            state.IsOpen = true;
            state.CardToPlay = evt.CardToPlay;
            state.RequiredCosts = (evt.RequiredCosts ?? new List<string>()).ToList();
            state.SelectedCards.Clear();
            state.OpenElapsedSeconds = 0f;

            // Stage the card: remove it from hand and record original index
            var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
            var deck = deckEntity?.GetComponent<Deck>();
            if (deck != null)
            {
                state.OriginalHandIndex = deck.Hand.IndexOf(state.CardToPlay);
                if (state.OriginalHandIndex >= 0)
                {
                    deck.Hand.RemoveAt(state.OriginalHandIndex);
                }
            }
            // Disable staged card interactions and set its z-order high
            var stagedUI = state.CardToPlay.GetComponent<UIElement>();
            if (stagedUI != null)
            {
                stagedUI.IsInteractable = false;
                stagedUI.IsHovered = false;
                stagedUI.IsClicked = false;
            }
            var stagedT = state.CardToPlay.GetComponent<Transform>();
            if (stagedT != null)
            {
                // Record start for tween and elevate Z
                state.StagedStartPos = stagedT.Position;
                state.StagedMoveElapsedSeconds = 0f;
                stagedT.ZOrder = 30000;
            }

            // Update interactability of remaining hand cards to only those viable to pay costs
            UpdateInteractablesForRemainingCosts(state);
        }

        private void Close()
        {
            var e = EntityManager.GetEntitiesWithComponent<PayCostOverlayState>().FirstOrDefault();
            if (e == null) return;
            var state = e.GetComponent<PayCostOverlayState>();
            if (state != null)
            {
                // If the play was canceled (overlay is closing without PayCostSatisfied), restore staged card to hand
                bool restoring = state.CardToPlay != null && state.RequiredCosts.Count > 0; // if not satisfied
                if (restoring)
                {
                    var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
                    var deck = deckEntity?.GetComponent<Deck>();
                    if (deck != null && state.CardToPlay != null)
                    {
                        int insertIndex = state.OriginalHandIndex;
                        if (insertIndex < 0 || insertIndex > deck.Hand.Count) insertIndex = deck.Hand.Count;
                        deck.Hand.Insert(insertIndex, state.CardToPlay);
                        // Reset transform so the hand system repositions it
                        var t = state.CardToPlay.GetComponent<Transform>();
                        if (t != null)
                        {
                            // Return to the original on-screen position so HandDisplaySystem tweens back smoothly
                            t.Position = state.StagedStartPos;
                            t.Rotation = 0f;
                            t.ZOrder = 0;
                        }
                    }
                }
                // Restore interactability for all hand cards
                RestoreHandInteractables();

                if (restoring)
                {
                    // Begin return tween for staged card from center back to original hand index
                    state.IsReturning = true;
                    state.ReturnElapsedSeconds = 0f;
                    // Keep overlay open briefly to animate the card back
                    state.IsOpen = true;
                }
                else
                {
                    // Successful pay: do not return to hand; close immediately
                    state.IsReturning = false;
                    state.ReturnElapsedSeconds = 0f;
                    state.IsOpen = false;
                    state.CardToPlay = null;
                    state.RequiredCosts.Clear();
                    state.SelectedCards.Clear();
                    state.OpenElapsedSeconds = 0f;
                    state.StagedMoveElapsedSeconds = 0f;
                    state.OriginalHandIndex = -1;
                }
            }
        }

        private static bool TryConsumeCostForCard(List<string> remainingCosts, CardData.CardColor candidateColor, out int consumedIndex)
        {
            consumedIndex = -1;
            // Prefer consuming a specific matching color first
            for (int i = 0; i < remainingCosts.Count; i++)
            {
                string c = remainingCosts[i];
                if ((c == "Red" && candidateColor == CardData.CardColor.Red) ||
                    (c == "White" && candidateColor == CardData.CardColor.White) ||
                    (c == "Black" && candidateColor == CardData.CardColor.Black))
                {
                    consumedIndex = i;
                    return true;
                }
            }
            // Otherwise consume an Any slot if available
            for (int i = 0; i < remainingCosts.Count; i++)
            {
                if (remainingCosts[i] == "Any")
                {
                    consumedIndex = i;
                    return true;
                }
            }
            return false;
        }

        private void OnCandidateClicked(PayCostCandidateClicked evt)
        {
            var stateEntity = EntityManager.GetEntitiesWithComponent<PayCostOverlayState>().FirstOrDefault();
            if (stateEntity == null) return;
            var state = stateEntity.GetComponent<PayCostOverlayState>();
            if (state == null || !state.IsOpen || evt?.Card == null) return;

            // Only allow selecting from current hand and not the card being played
            var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
            var deck = deckEntity?.GetComponent<Deck>();
            if (deck == null) return;
            if (!deck.Hand.Contains(evt.Card)) return;
            if (evt.Card == state.CardToPlay) return;

            var cd = evt.Card.GetComponent<CardData>();
            if (cd == null) return;

            // Avoid selecting the same card twice
            if (state.SelectedCards.Contains(evt.Card)) return;

            // Check if this card can satisfy one of the remaining costs
            if (TryConsumeCostForCard(state.RequiredCosts, cd.Color, out int idx))
            {
                // Consume that cost slot and record selection
                state.RequiredCosts.RemoveAt(idx);
                state.SelectedCards.Add(evt.Card);
                // Disable this card's interactions immediately
                var uiSel = evt.Card.GetComponent<UIElement>();
                if (uiSel != null)
                {
                    uiSel.IsInteractable = false;
                    uiSel.IsHovered = false;
                    uiSel.IsClicked = false;
                }

                // If all requirements satisfied, discard selected and complete
                if (state.RequiredCosts.Count == 0)
                {
                    foreach (var c in state.SelectedCards)
                    {
                        EventManager.Publish(new CardMoveRequested { Card = c, Deck = deckEntity, Destination = CardZoneType.DiscardPile, Reason = "PayCost" });
                    }
                    EventManager.Publish(new PayCostSatisfied { CardToPlay = state.CardToPlay, PaymentCards = new List<Entity>(state.SelectedCards) });
                    Close();
                }
                else
                {
                    // Update which remaining hand cards are viable based on new remaining requirements
                    UpdateInteractablesForRemainingCosts(state);
                }
            }
        }

        private string BuildCostPhrase(IEnumerable<string> costs)
        {
            var list = costs.ToList();
            if (list.Count == 0) return "";
            var groups = list
                .GroupBy(c => c)
                .Select(g => g.Count() > 1 ? $"{g.Count()} {g.Key}" : g.Key)
                .ToList();
            return string.Join(", ", groups);
        }

        public void DrawBackdrop()
        {
            var stateEntity = EntityManager.GetEntitiesWithComponent<PayCostOverlayState>().FirstOrDefault();
            if (stateEntity == null) return;
            var state = stateEntity.GetComponent<PayCostOverlayState>();
            if (state == null || !state.IsOpen) return;

            int w = _graphicsDevice.Viewport.Width;
            int h = _graphicsDevice.Viewport.Height;

            float t = MathHelper.Clamp(state.OpenElapsedSeconds / FadeInDurationSec, 0f, 1f);
            // EaseOutQuad
            float eased = 1f - (1f - t) * (1f - t);
            float alphaF = MathHelper.Clamp(eased * OverlayAlpha, 0f, 1f);

            // Full-screen dim overlay only
            _spriteBatch.Draw(_pixel, new Rectangle(0, 0, w, h), new Color(0f, 0f, 0f, alphaF));
        }

        public void DrawForeground()
        {
            var stateEntity = EntityManager.GetEntitiesWithComponent<PayCostOverlayState>().FirstOrDefault();
            if (stateEntity == null) return;
            var state = stateEntity.GetComponent<PayCostOverlayState>();
            if (state == null || !state.IsOpen) return;

            int w = _graphicsDevice.Viewport.Width;
            int h = _graphicsDevice.Viewport.Height;

            float t = MathHelper.Clamp(state.OpenElapsedSeconds / FadeInDurationSec, 0f, 1f);
            float eased = 1f - (1f - t) * (1f - t);
            float alphaF = MathHelper.Clamp(eased * OverlayAlpha, 0f, 1f);

            // Text
            var cd = state.CardToPlay?.GetComponent<CardData>();
            string cardName = cd?.Name ?? "Card";
            var defTextCosts = GetDefinitionCosts(state.CardToPlay);
            string costText = BuildCostPhrase(defTextCosts);
            string line = $"Discard {costText} to pay for {cardName}";
            var size = _font.MeasureString(line);
            var textPos = new Vector2(w / 2f - (size.X * TextScale) / 2f + TextOffsetX, h / 2f - (size.Y * TextScale) / 2f + TextOffsetY);
            _spriteBatch.DrawString(_font, line, textPos, Color.White, 0f, Vector2.Zero, TextScale, SpriteEffects.None, 0f);

            // Staged card above the dim
            var centerPos = new Vector2(w / 2f + CardOffsetX, h / 2f - (size.Y * TextScale) + CardOffsetY);
            if (state.CardToPlay != null)
            {
                Vector2 pos;
                if (!state.IsReturning)
                {
                    // Tween from hand to center (ease-out)
                    Vector2 start = state.StagedStartPos;
                    Vector2 target = centerPos;
                    float tm = MathHelper.Clamp(state.StagedMoveElapsedSeconds / Math.Max(0.001f, StagedMoveDurationSec), 0f, 1f);
                    float ease = 1f - (1f - tm) * (1f - tm);
                    pos = start + (target - start) * ease;
                }
                else
                {
                    // Tween back from center to original position (ease-in)
                    Vector2 start = centerPos;
                    Vector2 target = state.StagedStartPos;
                    float tm = MathHelper.Clamp(state.ReturnElapsedSeconds / Math.Max(0.001f, StagedReturnDurationSec), 0f, 1f);
                    float easeIn = tm * tm; // ease-in
                    pos = start + (target - start) * easeIn;
                }
                EventManager.Publish(new CardRenderScaledEvent { Card = state.CardToPlay, Position = pos, Scale = StagedCardScale });
            }

            // Cancel button (top-right)
            var btnRect = new Rectangle(w - 28 - 24, 24, 28, 28);
            float btnAlpha = Math.Min(1f, alphaF + 0.2f);
            _spriteBatch.Draw(_pixel, btnRect, new Color(70f / 255f, 70f / 255f, 70f / 255f, btnAlpha));
            DrawBorder(btnRect, Color.White, 2);
            var xSize = _font.MeasureString("X") * 0.6f;
            _spriteBatch.DrawString(_font, "X", new Vector2(btnRect.Center.X - xSize.X / 2f, btnRect.Center.Y - xSize.Y / 2f), Color.White, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);

            // Sync clickable cancel bounds entity
            var cancel = EntityManager.GetEntitiesWithComponent<PayCostCancelButton>().FirstOrDefault();
            if (cancel == null)
            {
                EnsureStateEntityExists();
                cancel = EntityManager.GetEntitiesWithComponent<PayCostCancelButton>().FirstOrDefault();
            }
            var ui = cancel?.GetComponent<UIElement>();
            if (ui != null)
            {
                ui.Bounds = btnRect;
            }
        }

        private void RestoreHandInteractables()
        {
            var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
            var deck = deckEntity?.GetComponent<Deck>();
            if (deck == null) return;
            foreach (var c in deck.Hand)
            {
                var ui = c.GetComponent<UIElement>();
                if (ui != null)
                {
                    ui.IsInteractable = true;
                    ui.IsHovered = false;
                    ui.IsClicked = false;
                }
            }
        }

        private static bool IsCardViableForCosts(Entity card, List<string> remainingCosts)
        {
            var cd = card.GetComponent<CardData>();
            if (cd == null) return false;
            // Card is viable if it can satisfy at least one remaining requirement
            foreach (var c in remainingCosts)
            {
                if (c == "Any") return true;
                if (c == "Red" && cd.Color == CardData.CardColor.Red) return true;
                if (c == "White" && cd.Color == CardData.CardColor.White) return true;
                if (c == "Black" && cd.Color == CardData.CardColor.Black) return true;
            }
            return false;
        }

        private void UpdateInteractablesForRemainingCosts(PayCostOverlayState state)
        {
            var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
            var deck = deckEntity?.GetComponent<Deck>();
            if (deck == null) return;
            foreach (var c in deck.Hand)
            {
                if (c == state.CardToPlay) continue;
                var ui = c.GetComponent<UIElement>();
                if (ui == null) continue;
                bool viable = IsCardViableForCosts(c, state.RequiredCosts);
                ui.IsInteractable = viable;
                if (!viable)
                {
                    ui.IsHovered = false;
                    ui.IsClicked = false;
                }
            }
        }

        private List<string> GetDefinitionCosts(Entity card)
        {
            if (card == null) return new List<string>();
            var data = card.GetComponent<CardData>();
            if (data == null) return new List<string>();

            // Lookup JSON definition to read cost array
            string id = (data.Name ?? string.Empty).Trim().ToLowerInvariant().Replace(' ', '_');
            if (string.IsNullOrEmpty(id)) return new List<string>();
            if (!Crusaders30XX.ECS.Data.Cards.CardDefinitionCache.TryGet(id, out var def))
            {
                string alt = (data.Name ?? string.Empty).Trim().ToLowerInvariant();
                if (!Crusaders30XX.ECS.Data.Cards.CardDefinitionCache.TryGet(alt, out def)) return new List<string>();
            }
            return (def.cost ?? Array.Empty<string>()).ToList();
        }

        private void DrawBorder(Rectangle rect, Color color, int thickness)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
            _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            _spriteBatch.Draw(_pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        }
    }
}


