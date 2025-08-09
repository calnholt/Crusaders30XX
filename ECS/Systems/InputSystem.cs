using System;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Config;
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

            // Toggle debug menu on D key press (edge-triggered)
            if (keyboardState.IsKeyDown(Keys.D) && !_previousKeyboardState.IsKeyDown(Keys.D))
            {
                ToggleDebugMenu();
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

            // Reset hover flags
            foreach (var x in uiEntities)
            {
                x.UI.IsHovered = false;
            }

            // Find top-most under mouse by ZOrder
            var top = uiEntities
                .Where(x => IsUnderMouse(x, mousePosition))
                .OrderByDescending(x => x.T?.ZOrder ?? 0)
                .FirstOrDefault();

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

        private static bool IsUnderMouse(dynamic x, Point mousePosition)
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
            var rect = CardConfig.GetCardVisualRect(transform.Position);
            Vector2 center = new Vector2(rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f);
            float rotation = transform.Rotation;
            float cos = (float)System.Math.Cos(rotation);
            float sin = (float)System.Math.Sin(rotation);

            Vector2 m = new Vector2(mousePosition.X, mousePosition.Y);
            Vector2 d = m - center;
            // rotate mouse into card local space (inverse rotation)
            float localX = d.X * cos + d.Y * sin;
            float localY = -d.X * sin + d.Y * cos;

            float halfW = CardConfig.CARD_WIDTH / 2f;
            float halfH = CardConfig.CARD_HEIGHT / 2f;
            return (localX >= -halfW && localX <= halfW && localY >= -halfH && localY <= halfH);
        }
        
        private void HandleUIClick(Entity entity)
        {
            // Handle different types of UI clicks
            var cardData = entity.GetComponent<CardData>();
            if (cardData != null)
            {
                // Handle card click
                HandleCardClick(entity);
            }

            var button = entity.GetComponent<UIButton>();
            if (button != null)
            {
                // Publish debug command event based on button command
                EventManager.Publish(new DebugCommandEvent { Command = button.Command });
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
            var cardInPlay = entity.GetComponent<CardInPlay>();
            if (cardInPlay != null && cardInPlay.IsPlayable)
            {
                // Try to play the card
                // This would typically trigger a card playing system
                Console.WriteLine($"Card clicked: {entity.GetComponent<CardData>()?.Name}");
            }
        }
        
        public void UpdateInput()
        {
            _previousMouseState = Mouse.GetState();
            _previousKeyboardState = Keyboard.GetState();
        }

        private void ToggleDebugMenu()
        {
            // Find or create a debug menu entity
            var menuEntity = EntityManager.GetEntitiesWithComponent<DebugMenu>().FirstOrDefault();
            if (menuEntity == null)
            {
                menuEntity = EntityManager.CreateEntity("DebugMenu");
                EntityManager.AddComponent(menuEntity, new DebugMenu { IsOpen = true });
                EntityManager.AddComponent(menuEntity, new Transform { Position = new Vector2(1800, 200), ZOrder = 5000 });
                EntityManager.AddComponent(menuEntity, new UIElement { Bounds = new Rectangle(1750, 150, 150, 300), IsInteractable = true });

                // Add a button: Hand -> Draw Card
                var drawButton = EntityManager.CreateEntity("DebugButton_DrawCard");
                EntityManager.AddComponent(drawButton, new Transform { Position = new Vector2(1800, 260), ZOrder = 5001 });
                EntityManager.AddComponent(drawButton, new UIElement { Bounds = new Rectangle(1730, 240, 140, 40), IsInteractable = true, Tooltip = "Draw 1 card" });
                EntityManager.AddComponent(drawButton, new UIButton { Label = "Draw Card", Command = "DrawCard" });

                // Add a button: Hand -> Redraw (discard, shuffle, draw 4)
                var redrawButton = EntityManager.CreateEntity("DebugButton_Redraw");
                EntityManager.AddComponent(redrawButton, new Transform { Position = new Vector2(1800, 300), ZOrder = 5001 });
                EntityManager.AddComponent(redrawButton, new UIElement { Bounds = new Rectangle(1730, 285, 140, 40), IsInteractable = true, Tooltip = "Discard hand, shuffle, draw 4" });
                EntityManager.AddComponent(redrawButton, new UIButton { Label = "Redraw", Command = "RedrawHand" });
            }
            else
            {
                var menu = menuEntity.GetComponent<DebugMenu>();
                menu.IsOpen = !menu.IsOpen;
                // Toggle interactability for children as well
                foreach (var e in EntityManager.GetEntitiesWithComponent<UIButton>())
                {
                    var ui = e.GetComponent<UIElement>();
                    if (ui != null) ui.IsInteractable = menu.IsOpen;
                }
            }
        }
    }
} 