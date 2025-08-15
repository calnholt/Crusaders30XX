using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;

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
                Bounds = new Rectangle(0, 0, 250, 350),
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
            
            // Attach Courage resource component by default (optional mechanics can read presence)
            world.AddComponent(entity, new Courage { Amount = 0 });
            // Attach Temperance resource component by default
            world.AddComponent(entity, new Temperance { Amount = 0 });
            // Attach StoredBlock resource component by default
            world.AddComponent(entity, new StoredBlock { Amount = 0 });
            // Attach HP component
            world.AddComponent(entity, new HP { Max = 100, Current = 100 });
            
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

            // Ensure a Battlefield world component exists with a default location
            world.AddComponent(entity, new Battlefield { Location = BattleLocation.Desert });

            // Ensure CardVisualSettings singleton exists
            var cvsEntity = world.EntityManager.GetEntitiesWithComponent<CardVisualSettings>().FirstOrDefault();
            if (cvsEntity == null)
            {
                cvsEntity = world.CreateEntity("CardVisualSettings");
                float sU = 1.0f; // starting UI scale
                world.AddComponent(cvsEntity, new CardVisualSettings
                {
                    UIScale = sU,
                    CardWidth = (int)System.Math.Round(250 * sU),
                    CardHeight = (int)System.Math.Round(350 * sU),
                    CardOffsetYExtra = (int)System.Math.Round(25 * sU),
                    CardGap = (int)System.Math.Round(-20 * sU),
                    CardBorderThickness = (int)System.Math.Max(1, System.Math.Round(3 * sU)),
                    CardCornerRadius = (int)System.Math.Max(2, System.Math.Round(18 * sU)),
                    HighlightBorderThickness = (int)System.Math.Max(1, System.Math.Round(5 * sU)),
                    TextMarginX = (int)System.Math.Round(16 * sU),
                    TextMarginY = (int)System.Math.Round(16 * sU),
                    NameScale = 0.7f * sU,
                    CostScale = 0.6f * sU,
                    DescriptionScale = 0.4f * sU,
                    BlockScale = 0.5f * sU,
                    BlockNumberScale = 0.9f * sU,
                    BlockNumberMarginX = (int)System.Math.Round(14 * sU),
                    BlockNumberMarginY = (int)System.Math.Round(12 * sU)
                });
            }
            
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

        /// <summary>
        /// Creates a hand of demo cards based on provided card definitions
        /// </summary>
        public static List<Entity> CreateDemoHand(World world)
        {
            return [
                // Red Cards
                CreateCard(world, "Strike", "6 damage, if Courage >= 5: +3 damage", 0, CardData.CardType.Attack, CardData.CardRarity.Common, "", CardData.CardColor.Red, [], 3),
                CreateCard(world, "Crush", "10 damage, if Courage >= 5: Stun", 0, CardData.CardType.Attack, CardData.CardRarity.Common, "", CardData.CardColor.Red, [], 3),
                CreateCard(world, "Devastate", "If Courage >= 10: Lose 10 Courage, deal 30 damage", 0, CardData.CardType.Attack, CardData.CardRarity.Rare, "", CardData.CardColor.Red, [CardData.CardColor.Red], 3),

                // White Cards
                CreateCard(world, "Empower", "Gain +2 Power", 0, CardData.CardType.Skill, CardData.CardRarity.Common, "", CardData.CardColor.White, [], 3),
                CreateCard(world, "Sharpen Blade", "Weapon gains +4 damage this turn and loses Once Per Turn", 0, CardData.CardType.Skill, CardData.CardRarity.Common, "", CardData.CardColor.White, [], 3),
                CreateCard(world, "Enrage", "Gain 3 Courage", 0, CardData.CardType.Skill, CardData.CardRarity.Common, "", CardData.CardColor.White, [], 3),
                CreateCard(world, "Siphon", "8 damage, heal amount dealt", 0, CardData.CardType.Attack, CardData.CardRarity.Uncommon, "", CardData.CardColor.White, [CardData.CardColor.Red], 3),
                CreateCard(world, "Charge", "Next attack gains +5 damage", 0, CardData.CardType.Skill, CardData.CardRarity.Common, "", CardData.CardColor.White, [], 3),

                // Black Cards (block value 6)
                CreateCard(world, "Anticipate", "Gain 15 Block", 0, CardData.CardType.Skill, CardData.CardRarity.Common, "", CardData.CardColor.Black, [CardData.CardColor.Black], 6),
                CreateCard(world, "Mark", "Enemy gains Mark. If Courage >= 5: Gain 1 Temperance", 0, CardData.CardType.Skill, CardData.CardRarity.Common, "", CardData.CardColor.Black, [], 6),
                CreateCard(world, "Finisher", "12 damage. If Courage >= 5 and this kills an enemy: Gain 1 Temperance", 0, CardData.CardType.Attack, CardData.CardRarity.Rare, "", CardData.CardColor.Black, [CardData.CardColor.Red], 6),
                CreateCard(world, "Impervious", "Until next turn: immune to enemy attack abilities", 0, CardData.CardType.Skill, CardData.CardRarity.Rare, "", CardData.CardColor.Black, [CardData.CardColor.White], 6),
                CreateCard(world, "Rapid Reflexes", "Next turn: blocks return to your hand after the monster turn", 0, CardData.CardType.Skill, CardData.CardRarity.Uncommon, "", CardData.CardColor.Black, [], 6)
            ];
        }

        // Overload for CreateCard to support color, cost type, and block value
        public static Entity CreateCard(World world, string name, string description, int cost,
            CardData.CardType type, CardData.CardRarity rarity, string imagePath,
            CardData.CardColor color, List<CardData.CardColor> costColors, int blockValue)
        {
            var entity = world.CreateEntity($"Card_{name}");

            var cardData = new CardData
            {
                Name = name,
                Description = description,
                Cost = cost,
                Type = type,
                Rarity = rarity,
                ImagePath = imagePath,
                Color = color,
                CardCostType = costColors.Count > 0 ? (CardData.CostType)costColors[0] : CardData.CostType.NoCost,
                BlockValue = blockValue
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
                Bounds = new Rectangle(0, 0, 250, 350),
                IsInteractable = true
            };

            world.AddComponent(entity, cardData);
            world.AddComponent(entity, cardInPlay);
            world.AddComponent(entity, transform);
            world.AddComponent(entity, sprite);
            world.AddComponent(entity, uiElement);

            return entity;
        }
    }
} 