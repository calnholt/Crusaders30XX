using System;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Config;
using Crusaders30XX.ECS.Components;
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

            // Collect all interactable UI elements
            var uiEntities = GetRelevantEntities()
                .Select(e => new { E = e, UI = e.GetComponent<UIElement>(), T = e.GetComponent<Transform>(), IsCard = e.GetComponent<CardData>() != null })
                .Where(x => x.UI != null && x.UI.IsInteractable)
                .ToList();

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
            _previousKeyboardState = Keyboard.GetState();
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
    }
} 