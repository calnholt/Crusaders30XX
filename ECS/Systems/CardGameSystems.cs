using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.Linq;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// System for managing deck operations like shuffling, drawing, and discarding
    /// </summary>
    public class DeckManagementSystem : Core.System
    {
        public DeckManagementSystem(EntityManager entityManager) : base(entityManager) { }
        
        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<Deck>();
        }
        
        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
            var deck = entity.GetComponent<Deck>();
            if (deck == null) return;
            
            // Ensure all cards are properly categorized
            CategorizeCards(deck);
        }
        
        private void CategorizeCards(Deck deck)
        {
            // Move cards to appropriate piles based on their state
            var allCards = deck.Cards.ToList();
            
            foreach (var card in allCards)
            {
                var cardInPlay = card.GetComponent<CardInPlay>();
                if (cardInPlay != null && cardInPlay.IsExhausted)
                {
                    if (!deck.ExhaustPile.Contains(card))
                    {
                        deck.DrawPile.Remove(card);
                        deck.DiscardPile.Remove(card);
                        deck.Hand.Remove(card);
                        deck.ExhaustPile.Add(card);
                    }
                }
            }
        }
        
        /// <summary>
        /// Shuffles the draw pile
        /// </summary>
        public void ShuffleDrawPile(Deck deck)
        {
            var random = new System.Random();
            var cards = deck.DrawPile.ToList();
            
            for (int i = cards.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                var temp = cards[i];
                cards[i] = cards[j];
                cards[j] = temp;
            }
            
            deck.DrawPile.Clear();
            deck.DrawPile.AddRange(cards);
        }
        
        /// <summary>
        /// Draws a card from the draw pile to the hand
        /// </summary>
        public bool DrawCard(Deck deck)
        {
            if (deck.DrawPile.Count == 0)
            {
                // Reshuffle discard pile into draw pile
                if (deck.DiscardPile.Count > 0)
                {
                    deck.DrawPile.AddRange(deck.DiscardPile);
                    deck.DiscardPile.Clear();
                    ShuffleDrawPile(deck);
                }
                else
                {
                    return false; // No cards to draw
                }
            }
            
            if (deck.DrawPile.Count > 0 && deck.Hand.Count < deck.MaxHandSize)
            {
                var card = deck.DrawPile[0];
                deck.DrawPile.RemoveAt(0);
                deck.Hand.Add(card);
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Discards a card from hand to discard pile
        /// </summary>
        public void DiscardCard(Deck deck, Entity card)
        {
            if (deck.Hand.Contains(card))
            {
                deck.Hand.Remove(card);
                deck.DiscardPile.Add(card);
            }
        }
    }
    
    /// <summary>
    /// System for handling combat mechanics
    /// </summary>
    public class CombatSystem : Core.System
    {
        public CombatSystem(EntityManager entityManager) : base(entityManager) { }
        
        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<Player>()
                .Concat(EntityManager.GetEntitiesWithComponent<Enemy>());
        }
        
        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
            var player = entity.GetComponent<Player>();
            var enemy = entity.GetComponent<Enemy>();
            
            if (player != null)
            {
                UpdatePlayer(player);
            }
            else if (enemy != null)
            {
                UpdateEnemy(enemy);
            }
        }
        
        private void UpdatePlayer(Player player)
        {
            // Reset energy at start of turn
            // This would typically be called by a turn management system
        }
        
        private void UpdateEnemy(Enemy enemy)
        {
            // Handle enemy AI and actions
            // This would be expanded based on your game's AI system
        }
        
        /// <summary>
        /// Applies damage to a target, considering block
        /// </summary>
        public void ApplyDamage(Entity target, int damage)
        {
            var player = target.GetComponent<Player>();
            var enemy = target.GetComponent<Enemy>();
            
            if (player != null)
            {
                ApplyDamageToPlayer(player, damage);
            }
            else if (enemy != null)
            {
                ApplyDamageToEnemy(enemy, damage);
            }
        }
        
        private void ApplyDamageToPlayer(Player player, int damage)
        {
            if (player.Block > 0)
            {
                if (player.Block >= damage)
                {
                    player.Block -= damage;
                    damage = 0;
                }
                else
                {
                    damage -= player.Block;
                    player.Block = 0;
                }
            }
            
            player.CurrentHealth -= damage;
            if (player.CurrentHealth < 0)
                player.CurrentHealth = 0;
        }
        
        private void ApplyDamageToEnemy(Enemy enemy, int damage)
        {
            if (enemy.Block > 0)
            {
                if (enemy.Block >= damage)
                {
                    enemy.Block -= damage;
                    damage = 0;
                }
                else
                {
                    damage -= enemy.Block;
                    enemy.Block = 0;
                }
            }
            
            enemy.CurrentHealth -= damage;
            if (enemy.CurrentHealth < 0)
                enemy.CurrentHealth = 0;
        }
        
        /// <summary>
        /// Adds block to a target
        /// </summary>
        public void AddBlock(Entity target, int block)
        {
            var player = target.GetComponent<Player>();
            var enemy = target.GetComponent<Enemy>();
            
            if (player != null)
            {
                player.Block += block;
            }
            else if (enemy != null)
            {
                enemy.Block += block;
            }
        }
    }
    
    /// <summary>
    /// System for rendering sprites and UI elements
    /// </summary>
    public class RenderingSystem : Core.System
    {
        private readonly SpriteBatch _spriteBatch;
        private readonly GraphicsDevice _graphicsDevice;
        private readonly Dictionary<string, Texture2D> _textureCache = new();
        
        public RenderingSystem(EntityManager entityManager, SpriteBatch spriteBatch, GraphicsDevice graphicsDevice) 
            : base(entityManager)
        {
            _spriteBatch = spriteBatch;
            _graphicsDevice = graphicsDevice;
        }
        
        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<Sprite>();
        }
        
        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
            // Rendering is typically done in a separate Draw method
            // This Update method could handle animation updates
            var animation = entity.GetComponent<Animation>();
            if (animation != null && animation.IsPlaying)
            {
                UpdateAnimation(animation, gameTime);
            }
        }
        
        private void UpdateAnimation(Animation animation, GameTime gameTime)
        {
            animation.CurrentTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
            
            if (animation.CurrentTime >= animation.Duration)
            {
                if (animation.IsLooping)
                {
                    animation.CurrentTime = 0f;
                }
                else
                {
                    animation.IsPlaying = false;
                }
            }
        }
        
        /// <summary>
        /// Draws all entities with sprite components
        /// </summary>
        public void Draw()
        {
            var entities = GetRelevantEntities().OrderBy(e => 
            {
                var transform = e.GetComponent<Transform>();
                return transform?.ZOrder ?? 0;
            });
            
            foreach (var entity in entities)
            {
                DrawEntity(entity);
            }
        }
        
        private void DrawEntity(Entity entity)
        {
            var sprite = entity.GetComponent<Sprite>();
            var transform = entity.GetComponent<Transform>();
            
            if (sprite == null || !sprite.IsVisible) return;
            
            var texture = GetTexture(sprite.TexturePath);
            if (texture == null) return;
            
            var position = transform?.Position ?? Vector2.Zero;
            var scale = transform?.Scale ?? Vector2.One;
            var rotation = transform?.Rotation ?? 0f;
            var origin = new Vector2(texture.Width / 2f, texture.Height / 2f);
            
            _spriteBatch.Draw(
                texture,
                position,
                sprite.SourceRectangle,
                sprite.Tint,
                rotation,
                origin,
                scale,
                SpriteEffects.None,
                0f
            );
        }
        
        private Texture2D GetTexture(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            
            if (!_textureCache.TryGetValue(path, out var texture))
            {
                // In a real implementation, you'd load the texture from content
                // For now, we'll return null
                return null;
            }
            
            return texture;
        }
    }
    
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
            uiElement.IsHovered = uiElement.Bounds.Contains(mousePosition);
            
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
            }
        }
        
        public void UpdateInput()
        {
            _previousMouseState = Mouse.GetState();
            _previousKeyboardState = Keyboard.GetState();
        }
    }
} 