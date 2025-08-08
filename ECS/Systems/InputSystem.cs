using System;
using Crusaders30XX.ECS.Core;
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
            var uiElement = entity.GetComponent<UIElement>();
            if (uiElement == null || !uiElement.IsInteractable) return;
            
            var mouseState = Mouse.GetState();
            var mousePosition = mouseState.Position;
            
            // Check if mouse is hovering over UI element
            bool wasHovered = uiElement.IsHovered;
            uiElement.IsHovered = uiElement.Bounds.Contains(mousePosition);
            
            // Debug output for hover state changes (only for cards)
            var cardData = entity.GetComponent<CardData>();
            if (cardData != null && wasHovered != uiElement.IsHovered)
            {
                Console.WriteLine($"Card {cardData.Name} hover changed: {wasHovered} -> {uiElement.IsHovered} at mouse pos {mousePosition} bounds {uiElement.Bounds}");
            }
            
            // Check for clicks
            if (uiElement.IsHovered && 
                mouseState.LeftButton == ButtonState.Pressed && 
                _previousMouseState.LeftButton == ButtonState.Released)
            {
                uiElement.IsClicked = true;
                HandleUIClick(entity);
            }
            else
            {
                uiElement.IsClicked = false;
            }
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