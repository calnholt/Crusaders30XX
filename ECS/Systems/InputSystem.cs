using System;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.Linq;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// System for handling input and UI interactions
    /// </summary>
    public class InputSystem : Core.System
    {
        private MouseState _previousMouseState;
        private KeyboardState _previousKeyboardState;
        
        public InputSystem(EntityManager entityManager) : base(entityManager)
        {
            _previousMouseState = Mouse.GetState();
            _previousKeyboardState = Keyboard.GetState();
        }
        
        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<UIElement>();
        }
        
        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
            // Defer hover resolution to a single pass so only the top-most UI under mouse is hovered
        }

        public override void Update(GameTime gameTime)
        {
            if (!IsActive) return;

            var mouseState = Mouse.GetState();
            var mousePosition = mouseState.Position;
            var keyboardState = Keyboard.GetState();

            // Debug menu toggle is handled globally in Game1 to be scene-independent

            // Toggle profiler overlay on P key press (edge-triggered)
            if (keyboardState.IsKeyDown(Keys.P) && !_previousKeyboardState.IsKeyDown(Keys.P))
            {
                ToggleProfilerOverlay();
            }

            // Collect all interactable UI elements
            var uiEntities = GetRelevantEntities()
                .Select(e => new { E = e, UI = e.GetComponent<UIElement>(), T = e.GetComponent<Transform>(), IsCard = e.GetComponent<CardData>() != null })
                .Where(x => x.UI != null && x.UI.IsInteractable)
                .ToList();

            // If a modal is open, restrict interactions to modal UI only
            var modalEntity = EntityManager.GetEntitiesWithComponent<CardListModal>().FirstOrDefault();
            bool isModalOpen = false;
            if (modalEntity != null)
            {
                var modal = modalEntity.GetComponent<CardListModal>();
                isModalOpen = modal != null && modal.IsOpen;
            }
            if (isModalOpen)
            {
                uiEntities = uiEntities
                    .Where(x => x.E.GetComponent<CardListModalClose>() != null)
                    .ToList();
            }

            // If pay-cost overlay is open, restrict interactions to: cancel button and hand cards
            var payEntity = EntityManager.GetEntitiesWithComponent<PayCostOverlayState>().FirstOrDefault();
            bool isPayOpen = false;
            if (payEntity != null)
            {
                var st = payEntity.GetComponent<PayCostOverlayState>();
                isPayOpen = st != null && st.IsOpen;
            }
            if (isPayOpen)
            {
                // While pay-cost overlay is open, only allow cancel button and HAND cards as candidates
                var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
                var deck = deckEntity?.GetComponent<Deck>();
                uiEntities = uiEntities
                    .Where(x => x.E.GetComponent<PayCostCancelButton>() != null || (x.IsCard && deck != null && deck.Hand.Contains(x.E)))
                    .ToList();
            }

            // During Action phase (normal play), only hand cards should be clickable among card UIs
            var phaseEntity = EntityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault();
            var phaseState = phaseEntity?.GetComponent<PhaseState>();
            if (!isPayOpen && phaseState != null && phaseState.Sub == SubPhase.Action)
            {
                var deckEntity2 = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
                var deck2 = deckEntity2?.GetComponent<Deck>();
                if (deck2 != null)
                {
                    uiEntities = uiEntities
                        .Where(x => !x.IsCard || deck2.Hand.Contains(x.E))
                        .ToList();
                }
            }

            // Reset hover flags
            foreach (var x in uiEntities)
            {
                x.UI.IsHovered = false;
            }

            // Find top-most under mouse by ZOrder
            // Use UI.Bounds for non-card UI, rotated-hit for cards; ignore very small bounds
            var underMouse = uiEntities
                .Where(x =>
                {
                    // Reject degenerate bounds
                    if (x.UI.Bounds.Width < 2 || x.UI.Bounds.Height < 2) return false;
                    return IsUnderMouse(x, mousePosition);
                })
                .OrderByDescending(x => x.T?.ZOrder ?? 0)
                .ToList();
            var top = underMouse.FirstOrDefault();

            if (top != null)
            {
                top.UI.IsHovered = true;

                // Handle click on the top-most only
                if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
                {
                    top.UI.IsClicked = true;
                    HandleUIClick(top.E);
                }
                else
                {
                    top.UI.IsClicked = false;
                }
            }

            // Optionally debug hover changes per card (limited to top)
            var cardData = top?.E.GetComponent<CardData>();
            if (cardData != null)
            {
                // You can log if needed
            }

            _previousMouseState = mouseState;
            _previousKeyboardState = keyboardState;
        }

        private bool IsUnderMouse(dynamic x, Point mousePosition)
        {
            if (!x.IsCard)
            {
                // Fallback to AABB for non-card UI
                return x.UI.Bounds.Contains(mousePosition);
            }

            // Rotated-rect hit test for cards
            var transform = x.T as Transform;
            if (transform == null) return x.UI.Bounds.Contains(mousePosition);

            // Use the same visual center as rendering does
            // Compute card center using shared CardVisualSettings
            var settingsEntity = this.EntityManager.GetEntitiesWithComponent<CardVisualSettings>().FirstOrDefault();
            var cvs = settingsEntity != null ? settingsEntity.GetComponent<CardVisualSettings>() : null;
            int cw = cvs?.CardWidth ?? 250;
            int ch = cvs?.CardHeight ?? 350;
            int offsetY = (ch / 2) + (int)Math.Round((cvs?.UIScale ?? 1f) * 25);
            Vector2 center = new Vector2(transform.Position.X, transform.Position.Y - offsetY + ch / 2f);
            float rotation = transform.Rotation;
            float cos = (float)System.Math.Cos(rotation);
            float sin = (float)System.Math.Sin(rotation);

            Vector2 m = new Vector2(mousePosition.X, mousePosition.Y);
            Vector2 d = m - center;
            // rotate mouse into card local space (inverse rotation)
            float localX = d.X * cos + d.Y * sin;
            float localY = -d.X * sin + d.Y * cos;

            float halfW = cw / 2f;
            float halfH = ch / 2f;
            return (localX >= -halfW && localX <= halfW && localY >= -halfH && localY <= halfH);
        }
        
        private void HandleUIClick(Entity entity)
        {
            // Handle different types of UI clicks
            var cardData = entity.GetComponent<CardData>();
            if (cardData != null)
            {
                // Handle card click
                // If pay-cost overlay is open, treat card click as candidate for payment instead of play
                var payStateEntity = EntityManager.GetEntitiesWithComponent<PayCostOverlayState>().FirstOrDefault();
                var payState = payStateEntity?.GetComponent<PayCostOverlayState>();
                if (payState != null && payState.IsOpen)
                {
                    EventManager.Publish(new PayCostCandidateClicked { Card = entity });
                }
                else
                {
                    HandleCardClick(entity);
                }
            }

            var button = entity.GetComponent<UIButton>();
            if (button != null)
            {
                // Publish debug command event based on button command
                EventManager.Publish(new DebugCommandEvent { Command = button.Command });
            }

            var payCancel = entity.GetComponent<PayCostCancelButton>();
            if (payCancel != null)
            {
                EventManager.Publish(new PayCostCancelRequested());
            }

            var drawPileClickable = entity.GetComponent<DrawPileClickable>();
            if (drawPileClickable != null)
            {
                // Open generic modal with draw pile content
                var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
                var deck = deckEntity?.GetComponent<Deck>();
                if (deck != null)
                {
                    EventManager.Publish(new OpenCardListModalEvent { Title = "Draw Pile", Cards = deck.DrawPile.ToList() });
                }
            }

            var discardPileClickable = entity.GetComponent<DiscardPileClickable>();
            if (discardPileClickable != null)
            {
                // Open generic modal with discard pile content
                var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
                var deck = deckEntity?.GetComponent<Deck>();
                if (deck != null)
                {
                    EventManager.Publish(new OpenCardListModalEvent { Title = "Discard Pile", Cards = deck.DiscardPile.ToList() });
                }
            }

            var cardListModalClose = entity.GetComponent<CardListModalClose>();
            if (cardListModalClose != null)
            {
                EventManager.Publish(new CloseCardListModalEvent());
            }
        }
        
        private void HandleCardClick(Entity entity)
        {
            var cardData = entity.GetComponent<CardData>();
            var phase = EntityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault()?.GetComponent<PhaseState>();
            Console.WriteLine($"[InputSystem] Card clicked id={cardData?.CardId} phase={phase}");
            if (phase.Sub == SubPhase.Action)
            {
                var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
                var deck = deckEntity?.GetComponent<Deck>();
                if (deck == null)
                {
                    Console.WriteLine("[InputSystem] No deck found; ignoring card click");
                    return;
                }
                if (!deck.Hand.Contains(entity))
                {
                    Console.WriteLine("[InputSystem] Clicked card is not in hand; ignoring");
                    return;
                }
                EventManager.Publish(new PlayCardRequested { Card = entity });
            }
        }
        
        public void UpdateInput()
        {
            _previousMouseState = Mouse.GetState();
            _previousKeyboardState = Keyboard.GetState();
        }

        // Debug menu toggle removed from here; see Game1.ToggleDebugMenu()

        private void ToggleProfilerOverlay()
        {
            var e = EntityManager.GetEntitiesWithComponent<ProfilerOverlay>().FirstOrDefault();
            if (e == null)
            {
                e = EntityManager.CreateEntity("ProfilerOverlay");
                EntityManager.AddComponent(e, new ProfilerOverlay { IsOpen = true });
            }
            else
            {
                var p = e.GetComponent<ProfilerOverlay>();
                p.IsOpen = !p.IsOpen;
            }
        }
    }
} 