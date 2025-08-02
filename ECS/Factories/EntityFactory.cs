using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Factories
{
    /// <summary>
    /// Factory for creating common game entities
    /// </summary>
    public static class EntityFactory
    {
        /// <summary>
        /// Creates a new card entity
        /// </summary>
        public static Entity CreateCard(World world, string name, string description, int cost, 
            CardData.CardType type, CardData.CardRarity rarity, string imagePath = "")
        {
            var entity = world.CreateEntity($"Card_{name}");
            
            var cardData = new CardData
            {
                Name = name,
                Description = description,
                Cost = cost,
                Type = type,
                Rarity = rarity,
                ImagePath = imagePath
            };
            
            var cardInPlay = new CardInPlay
            {
                EnergyCost = cost,
                IsPlayable = true
            };
            
            var transform = new Transform
            {
                Position = Vector2.Zero,
                Scale = Vector2.One
            };
            
            var sprite = new Sprite
            {
                TexturePath = imagePath,
                IsVisible = true
            };
            
            var uiElement = new UIElement
            {
                Bounds = new Rectangle(0, 0, 100, 150), // Default card size
                IsInteractable = true
            };
            
            world.AddComponent(entity, cardData);
            world.AddComponent(entity, cardInPlay);
            world.AddComponent(entity, transform);
            world.AddComponent(entity, sprite);
            world.AddComponent(entity, uiElement);
            
            return entity;
        }
        
        /// <summary>
        /// Creates a player entity
        /// </summary>
        public static Entity CreatePlayer(World world, string name = "Player")
        {
            var entity = world.CreateEntity(name);
            
            var player = new Player
            {
                MaxHealth = 100,
                CurrentHealth = 100,
                MaxEnergy = 3,
                CurrentEnergy = 3,
                Block = 0,
                Gold = 0
            };
            
            var transform = new Transform
            {
                Position = new Vector2(100, 300),
                Scale = Vector2.One
            };
            
            var sprite = new Sprite
            {
                TexturePath = "player",
                IsVisible = true
            };
            
            world.AddComponent(entity, player);
            world.AddComponent(entity, transform);
            world.AddComponent(entity, sprite);
            
            return entity;
        }
        
        /// <summary>
        /// Creates an enemy entity
        /// </summary>
        public static Entity CreateEnemy(World world, string name, int maxHealth, Vector2 position)
        {
            var entity = world.CreateEntity(name);
            
            var enemy = new Enemy
            {
                Name = name,
                MaxHealth = maxHealth,
                CurrentHealth = maxHealth,
                Block = 0
            };
            
            var transform = new Transform
            {
                Position = position,
                Scale = Vector2.One
            };
            
            var sprite = new Sprite
            {
                TexturePath = "enemy",
                IsVisible = true
            };
            
            world.AddComponent(entity, enemy);
            world.AddComponent(entity, transform);
            world.AddComponent(entity, sprite);
            
            return entity;
        }
        
        /// <summary>
        /// Creates a deck entity
        /// </summary>
        public static Entity CreateDeck(World world, string name = "Deck")
        {
            var entity = world.CreateEntity(name);
            
            var deck = new Deck
            {
                MaxHandSize = 10,
                DrawPerTurn = 5
            };
            
            world.AddComponent(entity, deck);
            
            return entity;
        }
        
        /// <summary>
        /// Creates a game state entity
        /// </summary>
        public static Entity CreateGameState(World world)
        {
            var entity = world.CreateEntity("GameState");
            
            var gameState = new GameState
            {
                CurrentPhase = GameState.GamePhase.MainMenu,
                TurnNumber = 1,
                IsPlayerTurn = true,
                IsGameOver = false
            };
            
            world.AddComponent(entity, gameState);
            
            return entity;
        }
        
        /// <summary>
        /// Creates a UI element entity
        /// </summary>
        public static Entity CreateUIElement(World world, string name, Rectangle bounds, string texturePath = "")
        {
            var entity = world.CreateEntity(name);
            
            var transform = new Transform
            {
                Position = new Vector2(bounds.X, bounds.Y),
                Scale = Vector2.One
            };
            
            var sprite = new Sprite
            {
                TexturePath = texturePath,
                IsVisible = true
            };
            
            var uiElement = new UIElement
            {
                Bounds = bounds,
                IsInteractable = true
            };
            
            world.AddComponent(entity, transform);
            world.AddComponent(entity, sprite);
            world.AddComponent(entity, uiElement);
            
            return entity;
        }
        
        /// <summary>
        /// Creates a basic attack card
        /// </summary>
        public static Entity CreateAttackCard(World world, string name, int damage, int cost = 1)
        {
            return CreateCard(world, name, $"Deal {damage} damage", cost, 
                CardData.CardType.Attack, CardData.CardRarity.Common);
        }
        
        /// <summary>
        /// Creates a basic skill card
        /// </summary>
        public static Entity CreateSkillCard(World world, string name, string description, int cost = 1)
        {
            return CreateCard(world, name, description, cost, 
                CardData.CardType.Skill, CardData.CardRarity.Common);
        }
        
        /// <summary>
        /// Creates a basic power card
        /// </summary>
        public static Entity CreatePowerCard(World world, string name, string description, int cost = 2)
        {
            return CreateCard(world, name, description, cost, 
                CardData.CardType.Power, CardData.CardRarity.Uncommon);
        }
    }
} 