using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Data.Enemies;
using Crusaders30XX.ECS.Data.Temperance;
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
                Block = 0,
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

            var portraitInfo = new PortraitInfo {
                CurrentScale = 0,
                Owner = entity,
                TextureHeight = 0,
                TextureWidth = 0,
            };
            var equippedTemperanceAbility = new EquippedTemperanceAbility {
                AbilityId = "radiance",
                Owner = entity
            };
            
            world.AddComponent(entity, player);
            world.AddComponent(entity, transform);
            world.AddComponent(entity, sprite);
            world.AddComponent(entity, portraitInfo);
            world.AddComponent(entity, equippedTemperanceAbility);
            
            // Attach Courage resource component by default (optional mechanics can read presence)
            world.AddComponent(entity, new Courage { Amount = 0 });
            // Attach Temperance resource component by default
            world.AddComponent(entity, new Temperance { Amount = 0 });
            // Attach Action Points component by default
            world.AddComponent(entity, new ActionPoints { Current = 0 });
            // Attach StoredBlock resource component by default
            world.AddComponent(entity, new StoredBlock { Amount = 0 });
            // Attach HP component
            world.AddComponent(entity, new HP { Max = 100, Current = 100 });
            // Attach starting Intellect and MaxHandSize stats
            world.AddComponent(entity, new Intellect { Value = 4 });
            world.AddComponent(entity, new MaxHandSize { Value = 5 });
            
            // Pre-create Courage tooltip hover entity (bounds updated by CourageDisplaySystem)
            var courageTooltip = world.CreateEntity("UI_CourageTooltip");
            world.AddComponent(courageTooltip, new CourageTooltipAnchor());
            world.AddComponent(courageTooltip, new Transform { Position = Vector2.Zero, ZOrder = 10001 });
            world.AddComponent(courageTooltip, new UIElement { Bounds = new Rectangle(0, 0, 1, 1), IsInteractable = true, Tooltip = "Courage" });

            // Pre-create Temperance tooltip hover entity (bounds updated by TemperanceDisplaySystem)
            var temperanceTooltip = world.CreateEntity("UI_TemperanceTooltip");
            world.AddComponent(temperanceTooltip, new TemperanceTooltipAnchor());
            world.AddComponent(temperanceTooltip, new Transform { Position = Vector2.Zero, ZOrder = 10001 });
            string temperanceTooltipText = "Temperance";
            if (TemperanceAbilityDefinitionCache.TryGet(equippedTemperanceAbility.AbilityId, out var tempDef) && tempDef != null)
            {
                string nm = string.IsNullOrWhiteSpace(tempDef.name) ? equippedTemperanceAbility.AbilityId : tempDef.name;
                string tx = tempDef.text ?? string.Empty;
                temperanceTooltipText = nm + "\n\n" + tx;
            }
            world.AddComponent(temperanceTooltip, new UIElement { Bounds = new Rectangle(0, 0, 1, 1), IsInteractable = true, Tooltip = temperanceTooltipText });

            // Pre-create Action Points tooltip hover entity (bounds updated by ActionPointDisplaySystem)
            var apTooltip = world.CreateEntity("UI_APTooltip");
            world.AddComponent(apTooltip, new Transform { Position = Vector2.Zero, ZOrder = 10001 });
            world.AddComponent(apTooltip, new UIElement { Bounds = new Rectangle(0, 0, 1, 1), IsInteractable = true, Tooltip = "Action Points" });

            // Pre-create Stored Block tooltip hover entity (bounds updated by StoredBlockDisplaySystem)
            var storedBlockTooltip = world.CreateEntity("UI_StoredBlockTooltip");
            world.AddComponent(storedBlockTooltip, new StoredBlockTooltipAnchor());
            world.AddComponent(storedBlockTooltip, new Transform { Position = Vector2.Zero, ZOrder = 10001 });
            world.AddComponent(storedBlockTooltip, new UIElement { Bounds = new Rectangle(0, 0, 1, 1), IsInteractable = true, Tooltip = "Stored Block" });

            return entity;
        }
        
        /// <summary>
        /// Creates a deck entity
        /// </summary>
        public static Entity CreateDeck(World world, string name = "Deck")
        {
            var entity = world.CreateEntity(name);
            
            var deck = new Deck {};
            
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
                TurnNumber = 0,
                IsPlayerTurn = true,
                IsGameOver = false
            };
            
			var phaseState = world.CreateEntity("PhaseState");
			var ps = new PhaseState { Main = MainPhase.StartBattle, Sub = SubPhase.StartBattle, TurnNumber = 0 };
			world.AddComponent(phaseState, ps);

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
                    DescriptionScale = 0.5f * sU,
                    BlockScale = 0.5f * sU,
                    BlockNumberScale = 0.9f * sU,
                    BlockNumberMarginX = (int)System.Math.Round(14 * sU),
                    BlockNumberMarginY = (int)System.Math.Round(12 * sU)
                });
            }
            

            // Ensure BattleInfo singleton exists
            var biEntity = world.EntityManager.GetEntitiesWithComponent<BattleInfo>().FirstOrDefault();
            if (biEntity == null)
            {
                biEntity = world.CreateEntity("BattleInfo");
                world.AddComponent(biEntity, new BattleInfo { TurnNumber = 0 });
            }
            // Create a default enemy from ID (fully driven by enemy JSON)
            var enemyEntity = CreateEnemyFromId(world, "demon");
            
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
        /// Creates a set of demo cards from JSON definitions in ECS/Data/Cards
        /// Description format: "X damage. If courage is >= threshold, this deals bonus damage instead."
        /// </summary>
        public static List<Entity> CreateDemoHand(World world)
        {
            var result = new List<Entity>();
            var all = Crusaders30XX.ECS.Data.Cards.CardDefinitionCache.GetAll()
                .Values
                .OrderBy(d => d.id)
                .ToList();
            foreach (var def in all)
            {
                var name = def.name ?? def.id ?? "Card";
                var color = ParseColor(def.color);
                int blockValue = (color == CardData.CardColor.Black) ? 6 : 3;
                // Use text from JSON directly
                string description = def.text ?? "";
                var entity = CreateCard(
                    world,
                    name,
                    description,
                    0,
                    CardData.CardType.Attack,
                    ParseRarity(def.rarity),
                    "",
                    color,
                    new List<CardData.CardColor>(),
                    blockValue
                );
                // Populate multi-cost array from JSON
                var cd = entity.GetComponent<CardData>();
                if (cd != null)
                {
                    cd.CostArray = new List<CardData.CostType>();
                    if (def.cost != null)
                    {
                        foreach (var c in def.cost)
                        {
                            var ct = ParseCostType(c);
                            if (ct != CardData.CostType.NoCost) cd.CostArray.Add(ct);
                        }
                    }
                    // Legacy single-cost compatibility (use the first non-Any if present)
                    var firstSpecific = cd.CostArray.FirstOrDefault(x => x == CardData.CostType.Red || x == CardData.CostType.White || x == CardData.CostType.Black);
                    if (firstSpecific != CardData.CostType.NoCost)
                        cd.CardCostType = firstSpecific;
                    else if (cd.CostArray.Any(x => x == CardData.CostType.Any))
                        cd.CardCostType = CardData.CostType.Any;
                    else
                        cd.CardCostType = CardData.CostType.NoCost;
                }
                result.Add(entity);
            }
            return result;
        }

        // Dynamic description generation removed; we now use JSON 'text' exclusively

        private static CardData.CardColor ParseColor(string color)
        {
            if (string.IsNullOrEmpty(color)) return CardData.CardColor.White;
            switch (color.Trim().ToLowerInvariant())
            {
                case "red": return CardData.CardColor.Red;
                case "black": return CardData.CardColor.Black;
                case "white":
                default: return CardData.CardColor.White;
            }
        }

        private static CardData.CardRarity ParseRarity(string rarity)
        {
            if (string.IsNullOrEmpty(rarity)) return CardData.CardRarity.Common;
            switch (rarity.Trim().ToLowerInvariant())
            {
                case "uncommon": return CardData.CardRarity.Uncommon;
                case "rare": return CardData.CardRarity.Rare;
                case "legendary": return CardData.CardRarity.Legendary;
                case "common":
                default: return CardData.CardRarity.Common;
            }
        }

        private static CardData.CostType ParseCostType(string cost)
        {
            if (string.IsNullOrEmpty(cost)) return CardData.CostType.NoCost;
            switch (cost.Trim().ToLowerInvariant())
            {
                case "red": return CardData.CostType.Red;
                case "white": return CardData.CostType.White;
                case "black": return CardData.CostType.Black;
                case "any": return CardData.CostType.Any;
                default: return CardData.CostType.NoCost;
            }
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
            world.AddComponent(entity, transform);
            world.AddComponent(entity, sprite);
            world.AddComponent(entity, uiElement);

            return entity;
        }

        public static Entity CreateEnemyFromId(World world, string enemyId)
        {
            var all = EnemyDefinitionCache.GetAll();
            if (!all.TryGetValue(enemyId, out var def))
            {
                System.Console.WriteLine($"[EntityFactory] Enemy id '{enemyId}' not found. Falling back to basic demon.");
                def = new EnemyDefinition { id = enemyId, name = enemyId, hp = 60, attackIds = new List<string>() };
            }

            var entity = world.CreateEntity($"Enemy_{def.id}");
            var enemy = new Enemy { Id = def.id, Name = def.name ?? def.id, Type = EnemyType.Demon, MaxHealth = def.hp, CurrentHealth = def.hp };
            var enemyTransform = new Transform { Position = new Vector2(world.EntityManager.GetEntitiesWithComponent<Player>().Any() ? 1200 : 1000, 260), Scale = Vector2.One };
            world.AddComponent(entity, enemy);
            world.AddComponent(entity, enemyTransform);
            world.AddComponent(entity, new HP { Max = enemy.MaxHealth, Current = enemy.CurrentHealth });
            world.AddComponent(entity, new PortraitInfo { TextureWidth = 0, TextureHeight = 0, CurrentScale = 1f });
            world.AddComponent(entity, new EnemyArsenal { AttackIds = new List<string>(def.attackIds) });
            world.AddComponent(entity, new AttackIntent());
            return entity;
        }
    }
} 