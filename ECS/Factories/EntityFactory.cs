using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Data.Temperance;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Data.Loadouts;
using System;
using Crusaders30XX.ECS.Systems;
using Crusaders30XX.ECS.Data.Locations;
using Crusaders30XX.ECS.Services;
using Crusaders30XX.ECS.Data.Save;
using Crusaders30XX.ECS.Objects.Cards;

namespace Crusaders30XX.ECS.Factories
{
    /// <summary>
    /// Factory for creating common game entities
    /// </summary>
    public static class EntityFactory
    {
        
        /// <summary>
        /// Creates a player entity
        /// </summary>
        public static Entity CreatePlayer(World world, string name = "Player")
        {
            var entity = world.CreateEntity(name);
            
            var player = new Player{};
            
            var transform = new Transform
            {
                Position = new Vector2(100, 300),
                Scale = Vector2.One
            };
            
            // Try to find a DungeonLoadout first, otherwise fall back to loadout_1
            LoadoutDefinition loadout = null;
            var qeEntity = world.EntityManager.GetEntitiesWithComponent<QueuedEvents>().FirstOrDefault();
            if (qeEntity != null && qeEntity.HasComponent<DungeonLoadout>())
            {
                loadout = qeEntity.GetComponent<DungeonLoadout>().Loadout;
            }
            else
            {
                loadout = LoadoutDefinitionCache.TryGet("loadout_1", out var def) ? def : null;
            }

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
                AbilityId = loadout.temperanceId,
                Owner = entity
            };
            
            world.AddComponent(entity, player);
            world.AddComponent(entity, transform);
            world.AddComponent(entity, sprite);
            world.AddComponent(entity, portraitInfo);
            world.AddComponent(entity, equippedTemperanceAbility);
            world.AddComponent(entity, ParallaxLayer.GetCharacterParallaxLayer());
            // Equip default weapon (not in deck)
            world.AddComponent(entity, new EquippedWeapon { WeaponId = loadout.weaponId });
            if (!string.IsNullOrWhiteSpace(loadout.headId))
            {
                var equipHeadEntity = world.CreateEntity("Equip_Head");
                var equipment = EquipmentFactory.Create(loadout.headId);
                equipment.Initialize(world.EntityManager, equipHeadEntity);
                world.AddComponent(equipHeadEntity, new Transform { Position = new Vector2(0, 0), ZOrder = 10001 });
                world.AddComponent(equipHeadEntity, new UIElement { IsInteractable = true });
                world.AddComponent(equipHeadEntity, new EquippedEquipment { EquippedOwner = entity, Equipment = equipment });
                world.AddComponent(equipHeadEntity, ParallaxLayer.GetUIParallaxLayer());
            }
            if (!string.IsNullOrWhiteSpace(loadout.legsId))
            {
                var equipLegsEntity = world.CreateEntity("Equip_Legs");
                var equipment = EquipmentFactory.Create(loadout.legsId);
                equipment.Initialize(world.EntityManager, equipLegsEntity);
                world.AddComponent(equipLegsEntity, new Transform { Position = new Vector2(0, 0), ZOrder = 10001 });
                world.AddComponent(equipLegsEntity, new UIElement { IsInteractable = true });
                world.AddComponent(equipLegsEntity, new EquippedEquipment { EquippedOwner = entity, Equipment = equipment });
                world.AddComponent(equipLegsEntity, ParallaxLayer.GetUIParallaxLayer());
            }
            if (!string.IsNullOrWhiteSpace(loadout.armsId))
            {
                var equipArmsEntity = world.CreateEntity("Equip_Arms");
                var equipment = EquipmentFactory.Create(loadout.armsId);
                equipment.Initialize(world.EntityManager, equipArmsEntity);
                world.AddComponent(equipArmsEntity, new Transform { Position = new Vector2(0, 0), ZOrder = 10001 });
                world.AddComponent(equipArmsEntity, new UIElement { IsInteractable = true });
                world.AddComponent(equipArmsEntity, new EquippedEquipment { EquippedOwner = entity, Equipment = equipment });
                world.AddComponent(equipArmsEntity, ParallaxLayer.GetUIParallaxLayer());
            }
            if (!string.IsNullOrWhiteSpace(loadout.chestId))
            {
                var equipChestEntity = world.CreateEntity("Equip_Chest");
                var equipment = EquipmentFactory.Create(loadout.chestId);
                equipment.Initialize(world.EntityManager, equipChestEntity);
                world.AddComponent(equipChestEntity, new Transform { Position = new Vector2(0, 0), ZOrder = 10001 });
                world.AddComponent(equipChestEntity, new UIElement { IsInteractable = true });
                world.AddComponent(equipChestEntity, new EquippedEquipment { EquippedOwner = entity, Equipment = equipment });
                world.AddComponent(equipChestEntity, ParallaxLayer.GetUIParallaxLayer());
            }
            // Parallax handled by UI root in EquipmentDisplaySystem
            // Attach Courage resource component by default (optional mechanics can read presence)
            world.AddComponent(entity, new Courage { Amount = 0 });
            // Attach Temperance resource component by default
            world.AddComponent(entity, new Temperance { Amount = 0 });
            // Attach Action Points component by default
            world.AddComponent(entity, new ActionPoints { Current = 0 });
            // Attach HP component
            world.AddComponent(entity, new HP { Max = 20, Current = 20 });
            // Equip default medals (can equip multiple later). For now, just st_luke.
            foreach (var medalId in loadout.medalIds)
            {
                var medalEntity = world.CreateEntity($"Medal_{medalId}");
                var medal = MedalFactory.Create(medalId);
                medal.Initialize(world.EntityManager, medalEntity);
                world.AddComponent(medalEntity, new EquippedMedal { EquippedOwner = entity, Medal = medal });
                world.AddComponent(medalEntity, ParallaxLayer.GetUIParallaxLayer());
                world.AddComponent(medalEntity, new UIElement { IsInteractable = false });
                world.AddComponent(medalEntity, new Transform { Position = Vector2.Zero, ZOrder = 10001 });
            }
            // Attach starting Intellect and MaxHandSize stats
            world.AddComponent(entity, new Intellect { Value = 4 });
            world.AddComponent(entity, new MaxHandSize { Value = 5 });

            var st = new BattleStateInfo { Owner = entity };
            world.AddComponent(entity, st);

            world.AddComponent(entity, new AppliedPassives());
            // Pre-create Courage tooltip hover entity (bounds updated by CourageDisplaySystem)
            var courageTooltip = world.CreateEntity("UI_CourageTooltip");
            world.AddComponent(courageTooltip, new CourageTooltipAnchor());
            world.AddComponent(courageTooltip, new Transform { Position = Vector2.Zero, ZOrder = 10001 });
            world.AddComponent(courageTooltip, new UIElement { Bounds = new Rectangle(0, 0, 1, 1), Tooltip = "Courage\n\n(Blocking with red cards increases your courage by 1)" });

            // Pre-create Temperance tooltip hover entity (bounds updated by TemperanceDisplaySystem)
            var temperanceTooltip = world.CreateEntity("UI_TemperanceTooltip");
            world.AddComponent(temperanceTooltip, new TemperanceTooltipAnchor());
            world.AddComponent(temperanceTooltip, new Transform { Position = Vector2.Zero, ZOrder = 10001 });
            string temperanceTooltipText = "Temperance Meter";
            if (TemperanceAbilityDefinitionCache.TryGet(equippedTemperanceAbility.AbilityId, out var tempDef) && tempDef != null)
            {
                string nm = string.IsNullOrWhiteSpace(tempDef.name) ? equippedTemperanceAbility.AbilityId : tempDef.name;
                string tx = tempDef.text ?? string.Empty;
                temperanceTooltipText += "\n\n" + nm + "\n\n" + tx + "\n\n" + "(Blocking with white cards increases your temperance by 1)";
            }
            world.AddComponent(temperanceTooltip, new UIElement { Bounds = new Rectangle(0, 0, 1, 1), Tooltip = temperanceTooltipText });

            // Pre-create Action Points tooltip hover entity (bounds updated by ActionPointDisplaySystem)
            var apTooltip = world.CreateEntity("UI_APTooltip");
            world.AddComponent(apTooltip, new Transform { Position = Vector2.Zero, ZOrder = 10001 });
            world.AddComponent(apTooltip, new UIElement { Bounds = new Rectangle(0, 0, 1, 1), Tooltip = "Action Points" });

            // Pre-create Weapon tooltip hover entity (bounds updated by EquippedWeaponDisplaySystem)
            var weaponTooltip = world.CreateEntity("UI_WeaponTooltip");
            world.AddComponent(weaponTooltip, new Transform { Position = Vector2.Zero, ZOrder = 10001 });
            world.AddComponent(weaponTooltip, new UIElement { Bounds = new Rectangle(0, 0, 1, 1), Tooltip = "Weapon" });

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
            
			var phaseState = world.CreateEntity("PhaseState");
			var ps = new PhaseState { Main = MainPhase.StartBattle, Sub = SubPhase.StartBattle, TurnNumber = 0 };
			world.AddComponent(phaseState, ps);


            // Ensure a Battlefield world component exists with a default location
            world.AddComponent(entity, new Battlefield { Location = BattleLocation.Desert });


            // Ensure BattleInfo singleton exists
            var biEntity = world.EntityManager.GetEntitiesWithComponent<BattleInfo>().FirstOrDefault();
            if (biEntity == null)
            {
                biEntity = world.CreateEntity("BattleInfo");
                world.AddComponent(biEntity, new BattleInfo { TurnNumber = 0 });
            }
            return entity;
        }

        public static Entity CreateCardVisualSettings(World world)
        {
            float sU = 1.0f; // starting UI scale
            var entity = world.CreateEntity("CardVisualSettings");
            world.AddComponent(entity, new CardVisualSettings
            {
                UIScale = sU,
                CardWidth = (int)Math.Round(250 * sU),
                CardHeight = (int)Math.Round(350 * sU),
                CardOffsetYExtra = (int)Math.Round(25 * sU),
                CardGap = (int)Math.Round(-20 * sU),
                CardBorderThickness = (int)Math.Max(0, Math.Round(0 * sU)),
                CardCornerRadius = (int)Math.Max(2, Math.Round(18 * sU)),
                HighlightBorderThickness = (int)Math.Max(1, Math.Round(5 * sU)),
                TextMarginX = (int)Math.Round(16 * sU),
                TextMarginY = (int)Math.Round(16 * sU),
                NameScale = 0.155f * sU,
                CostScale = 0.6f * sU,
                DescriptionScale = 0.115f * sU,
                BlockScale = 0.5f * sU,
                BlockNumberScale = 0.225f * sU,
                BlockNumberMarginX = (int)Math.Round(14 * sU),
                BlockNumberMarginY = (int)Math.Round(12 * sU)
            });
            return entity;
        }
        
        public static List<Entity> CreateDeckFromLoadout(EntityManager entityManager)
        {
			var result = new List<Entity>();
            
            // Try to find a DungeonLoadout first, otherwise fall back to loadout_1
            LoadoutDefinition loadout = null;
            var qeEntity = entityManager.GetEntitiesWithComponent<QueuedEvents>().FirstOrDefault();
            if (qeEntity != null && qeEntity.HasComponent<DungeonLoadout>())
            {
                loadout = qeEntity.GetComponent<DungeonLoadout>().Loadout;
            }
            else
            {
                loadout = LoadoutDefinitionCache.TryGet("loadout_1", out var lo) ? lo : null;
            }

			if (loadout == null || loadout.cardIds == null || loadout.cardIds.Count == 0) return result;
			foreach (var entry in loadout.cardIds)
			{
				if (string.IsNullOrWhiteSpace(entry)) continue;
				string cardId = entry;
				CardData.CardColor color = CardData.CardColor.White;
				int sep = entry.IndexOf('|');
				if (sep >= 0)
				{
					cardId = entry.Substring(0, sep);
					var colorKey = entry.Substring(sep + 1);
					color = ParseColor(colorKey);
				}
                var card = CardFactory.Create(cardId);
				if (card == null) continue;
				if (card.IsWeapon) continue; // weapons are not in the deck
                var entity = CreateCardFromDefinition(entityManager, cardId, color);
				var cd = entity.GetComponent<CardData>();
				if (cd != null)
				{
                    // already initialized in CreateCardFromDefinition
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

        // Helper: create card entity from CardDefinition id and color
        public static Entity CreateCardFromDefinition(EntityManager entityManager, string cardId, CardData.CardColor color, bool allowWeapons = false, int index = 0)
        {
            var card = CardFactory.Create(cardId);
            if (card == null) return null;
            if (card.IsWeapon && !allowWeapons) return null; // only non-weapons for deck/library
            string name = card.Name ?? cardId;
            var entity = entityManager.CreateEntity($"Card_{name}_{color}_{index}");

            var cardData = new CardData
            {
                Card = CardFactory.Create(cardId),
                Color = color
            };
            
            try
            {
                if (cardData.Card?.OnCreate != null)
                {
                    cardData.Card.OnCreate(entityManager, entity);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in OnCreate for card {cardId}: {ex}");
                throw;
            }

            var transform = new Transform { Position = new Vector2(-1000, -1000), Scale = Vector2.One };
            var sprite = new Sprite { TexturePath = string.Empty, IsVisible = true };
            var uiElement = new UIElement { Bounds = new Rectangle(-1000, -1000, 250, 350), IsInteractable = true, TooltipPosition = TooltipPosition.Above, TooltipOffsetPx = 30 };

            entityManager.AddComponent(entity, cardData);
            entityManager.AddComponent(entity, transform);
            entityManager.AddComponent(entity, sprite);
            entityManager.AddComponent(entity, uiElement);
            entityManager.AddComponent(entity, ParallaxLayer.GetUIParallaxLayer());
            entityManager.AddComponent(entity, new Hint { Text = card.GetCardHint(color) });
            entityManager.AddComponent(entity, new DontDestroyOnReload());
            var modifiedBlock = new ModifiedBlock { Modifications = new List<Modification>() };
            entityManager.AddComponent(entity, modifiedBlock);
            if (color == CardData.CardColor.Black)
            {
                modifiedBlock.Modifications.Add(new Modification { Delta = 1, Reason = "Black card" });
            }
            if (card.Type == CardType.Attack)
            {
                entityManager.AddComponent(entity, new ModifiedDamage { Modifications = new List<Modification>() });
            }
            // Auto-generate tooltip from card text keywords
            if (string.IsNullOrEmpty(card.Tooltip) && !string.IsNullOrEmpty(card.Text))
            {
                card.Tooltip = card.Text;  // Setter will process keywords via KeywordTooltipTextService
            }
            // Set tooltip from definition (precomputed in CardDefinitionCache)
            if (!string.IsNullOrEmpty(card.Tooltip))
            {
                uiElement.Tooltip = card.Tooltip;
            }
            // Attach CardTooltip when data specifies a tooltip card, and mark UI tooltip type
            if (!string.IsNullOrWhiteSpace(card.CardTooltip))
            {
                entityManager.AddComponent(entity, new CardTooltip { CardId = card.CardTooltip });
                uiElement.TooltipType = TooltipType.Card;
            }
            return entity;
        }

        public static Entity CreateEnemyFromId(World world, string enemyId, EntityManager entityManager, List<EnemyModification> modifications = null)
        {
            var def = EnemyFactory.Create(enemyId);
            def.EntityManager = entityManager;

            var enemyEntity = world.CreateEntity($"Enemy");
            var numEquippedEquipment = world.EntityManager.GetEntitiesWithComponent<EquippedEquipment>().Count();
            int equipmentHpModifier = numEquippedEquipment * 3;
            def.MaxHealth += equipmentHpModifier;
            var enemy = new Enemy { Id = def.Id, Name = def.Id, MaxHealth = def.MaxHealth, CurrentHealth = def.CurrentHealth != 0 ? def.CurrentHealth : def.MaxHealth, EnemyBase = def };
            var enemyTransform = new Transform { Position = new Vector2(world.EntityManager.GetEntitiesWithComponent<Player>().Any() ? 1200 : 1000, 260), Scale = Vector2.One };
            world.AddComponent(enemyEntity, enemy);
            world.AddComponent(enemyEntity, enemyTransform);
            world.AddComponent(enemyEntity, new UIElement { Tooltip = def.Name ?? def.Id, IsInteractable = false , TooltipPosition = TooltipPosition.Above });
            world.AddComponent(enemyEntity, new HP { Max = enemy.MaxHealth, Current = enemy.CurrentHealth });
            world.AddComponent(enemyEntity, new PortraitInfo { TextureWidth = 0, TextureHeight = 0, CurrentScale = 1f });
            world.AddComponent(enemyEntity, new EnemyArsenal { AttackIds = [.. def.GetAttackIds(world.EntityManager, 0)] });
            world.AddComponent(enemyEntity, new AttackIntent());
            world.AddComponent(enemyEntity, new AppliedPassives());
            world.AddComponent(enemyEntity, new Threat { Amount = 0 });
            world.AddComponent(enemyEntity, ParallaxLayer.GetCharacterParallaxLayer());

            var playerEntity = world.EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();

            // if (def.OnStartOfBattle != null)
            // {
            //     def.OnStartOfBattle(world.EntityManager);
            // }

            // Apply quest modifications if any
            if (modifications != null && modifications.Count > 0)
            {
                foreach (var mod in modifications)
                {
                    if (string.IsNullOrEmpty(mod.Type)) continue;

                    if (mod.Type.Equals("HP", StringComparison.OrdinalIgnoreCase))
                    {
                        // Apply HP modification
                        enemy.MaxHealth += mod.Delta;
                        enemy.CurrentHealth += mod.Delta;
                        var hpComponent = enemyEntity.GetComponent<HP>();
                        if (hpComponent != null)
                        {
                            hpComponent.Max += mod.Delta;
                            hpComponent.Current += mod.Delta;
                        }
                        Console.WriteLine($"[EntityFactory] Applied HP modification: +{mod.Delta} (new HP: {enemy.MaxHealth})");
                    }
                    else if (mod.Type.Equals("Armor", StringComparison.OrdinalIgnoreCase))
                    {
                        // Apply Armor modification via passive
                        Enum.TryParse<AppliedPassiveType>("Armor", true, out var armorType);
                        EventManager.Publish(new ApplyPassiveEvent { Target = enemyEntity, Delta = mod.Delta, Type = armorType });
                        Console.WriteLine($"[EntityFactory] Applied Armor modification: +{mod.Delta}");
                    }
                }
            }

            // Pre-create Threat tooltip hover entity (bounds updated by ThreatDisplaySystem)
            var threatTooltip = world.CreateEntity("UI_ThreatTooltip");
            world.AddComponent(threatTooltip, new ThreatTooltipAnchor());
            world.AddComponent(threatTooltip, new Transform { Position = Vector2.Zero, ZOrder = 10001 });
            world.AddComponent(threatTooltip, new UIElement { Bounds = new Rectangle(0, 0, 1, 1), Tooltip = "Threat\n\nAt the start of the enemy's turn, it gains X aggression equal to its current threat level.\nWhenever you attack an enemy, it reduces their threat by one.\nEnemy's gain one threat at the end of their turn." });

            return enemyEntity;
        }

		/// <summary>
		/// Create UI entities for each for-sale item in a shop.
		/// </summary>
		public static List<Entity> CreateForSale(EntityManager entityManager, IEnumerable<ForSaleItemDefinition> defs, string shopName)
		{
			var result = new List<Entity>();
			if (defs == null) return result;
			var collection = SaveCache.GetCollectionSet();
			int idx = 0;
			foreach (var fs in defs)
			{
				if (fs == null || string.IsNullOrWhiteSpace(fs.id)) { idx++; continue; }
				string id = fs.id;
				string type = fs.type ?? "Card";
				ForSaleItemType itemType = ForSaleItemType.Card;
				switch (type.Trim().ToLowerInvariant())
				{
					case "medal": itemType = ForSaleItemType.Medal; break;
					case "equipment": itemType = ForSaleItemType.Equipment; break;
					case "weapon": itemType = ForSaleItemType.Weapon; break;
					case "card":
					default: itemType = ForSaleItemType.Card; break;
				}

				string displayName = id;
                var card = CardFactory.Create(id);
				try
				{
					if ((itemType == ForSaleItemType.Card || itemType == ForSaleItemType.Weapon) && card != null)
					{
						displayName = string.IsNullOrWhiteSpace(card.Name) ? id : card.Name;
					}
					else if (itemType == ForSaleItemType.Medal && MedalFactory.GetAllMedals().TryGetValue(id, out var medal) && medal != null)
					{
						displayName = string.IsNullOrWhiteSpace(medal.Name) ? id : medal.Name;
					}
					else if (itemType == ForSaleItemType.Equipment)
					{
						var equipment = EquipmentFactory.Create(id);
						displayName = string.IsNullOrWhiteSpace(equipment.Name) ? id : equipment.Name;
					}
				}
				catch { }

				var e = entityManager.CreateEntity($"ShopItem_{id}_{idx}");
				entityManager.AddComponent(e, new Transform { Position = new Vector2(-1000, -1000), ZOrder = 10002 });
				// Configure parallax for UI tiles and ensure it updates UI bounds with its offset
				var pl = ParallaxLayer.GetUIParallaxLayer();
				pl.AffectsUIBounds = true;
				entityManager.AddComponent(e, pl);
                var uiElement = new UIElement { Bounds = new Rectangle(-1000, -1000, 1, 1), IsInteractable = true };
                if (itemType == ForSaleItemType.Card || itemType == ForSaleItemType.Weapon)
                {
                    uiElement.TooltipType = TooltipType.Card;
                    entityManager.AddComponent(e, new CardTooltip { CardId = id, TooltipScale = 0.8f });
                }
                else if (itemType == ForSaleItemType.Medal)
                {
                    var medal = MedalFactory.Create(id);
                    uiElement.Tooltip = $"{medal.Text}";
                }
                else if (itemType == ForSaleItemType.Equipment)
                {
                    var equipment = EquipmentFactory.Create(id);
                    uiElement.Tooltip = EquipmentService.GetTooltipText(equipment, EquipmentTooltipType.Shop);
                }
				// Always attach UIElement for hover/click regardless of item type
				entityManager.AddComponent(e, uiElement);
				entityManager.AddComponent(e, new OwnedByScene { Scene = SceneId.Shop });
				entityManager.AddComponent(e, new ForSaleItem
				{
					Id = id,
					ItemType = itemType,
					Price = fs.price,
					IsPurchased = collection.Contains(id),
					DisplayName = displayName,
					SourceShopName = shopName ?? string.Empty
				});
				result.Add(e);
				idx++;
			}
			return result;
		}

        /// <summary>
        /// Clones a card entity, copying all gameplay-affecting components while creating fresh visual/animation state.
        /// </summary>
        public static Entity CloneEntity(EntityManager entityManager, Entity sourceEntity)
        {
            if (sourceEntity == null) return null;

            // Get the card data to construct a proper name
            var sourceCardData = sourceEntity.GetComponent<CardData>();
            if (sourceCardData == null) return null; // Only supports card entities for now

            string name = sourceCardData.Card?.Name ?? "Card";
            var clonedEntity = entityManager.CreateEntity($"Card_{name}_{sourceCardData.Color}_Clone_{sourceEntity.Id}");

            // Deep copy CardData
            var clonedCardData = new CardData
            {
                Card = CardFactory.Create(sourceCardData.Card.CardId),
                Color = sourceCardData.Color,
                Owner = clonedEntity
            };
            entityManager.AddComponent(clonedEntity, clonedCardData);

            // Deep copy ModifiedBlock (with all modifications)
            var sourceModifiedBlock = sourceEntity.GetComponent<ModifiedBlock>();
            if (sourceModifiedBlock != null)
            {
                var clonedModifiedBlock = new ModifiedBlock
                {
                    Owner = clonedEntity,
                    Modifications = new List<Modification>(sourceModifiedBlock.Modifications)
                };
                entityManager.AddComponent(clonedEntity, clonedModifiedBlock);
            }

            // Deep copy ModifiedDamage (with all modifications)
            var sourceModifiedDamage = sourceEntity.GetComponent<ModifiedDamage>();
            if (sourceModifiedDamage != null)
            {
                var clonedModifiedDamage = new ModifiedDamage
                {
                    Owner = clonedEntity,
                    Modifications = new List<Modification>(sourceModifiedDamage.Modifications)
                };
                entityManager.AddComponent(clonedEntity, clonedModifiedDamage);
            }

            // Copy Intimidated status
            if (sourceEntity.HasComponent<Intimidated>())
            {
                entityManager.AddComponent(clonedEntity, new Intimidated { Owner = clonedEntity });
            }

            // Copy Frozen status
            if (sourceEntity.HasComponent<Frozen>())
            {
                entityManager.AddComponent(clonedEntity, new Frozen { Owner = clonedEntity });
            }

            // Copy Hint
            var sourceHint = sourceEntity.GetComponent<Hint>();
            if (sourceHint != null)
            {
                entityManager.AddComponent(clonedEntity, new Hint 
                { 
                    Owner = clonedEntity,
                    Text = sourceHint.Text 
                });
            }

            // Copy CardTooltip
            var sourceCardTooltip = sourceEntity.GetComponent<CardTooltip>();
            if (sourceCardTooltip != null)
            {
                entityManager.AddComponent(clonedEntity, new CardTooltip
                {
                    Owner = clonedEntity,
                    CardId = sourceCardTooltip.CardId,
                    TooltipScale = sourceCardTooltip.TooltipScale
                });
            }

            // Copy DontDestroyOnReload
            if (sourceEntity.HasComponent<DontDestroyOnReload>())
            {
                entityManager.AddComponent(clonedEntity, new DontDestroyOnReload { Owner = clonedEntity });
            }

            // Create fresh Transform (off-screen position)
            var sourceTransform = sourceEntity.GetComponent<Transform>();
            entityManager.AddComponent(clonedEntity, new Transform
            {
                Owner = clonedEntity,
                Position = new Vector2(-1000, -1000),
                BasePosition = new Vector2(-1000, -1000),
                Rotation = 0f,
                Scale = sourceTransform?.Scale ?? Vector2.One,
                ZOrder = 0
            });

            // Create fresh UIElement (no hover state, fresh bounds)
            var sourceUIElement = sourceEntity.GetComponent<UIElement>();
            var clonedUIElement = new UIElement
            {
                Owner = clonedEntity,
                Bounds = new Rectangle(-1000, -1000, 250, 350),
                IsHovered = false,
                IsClicked = false,
                IsInteractable = true,
                Tooltip = sourceUIElement?.Tooltip ?? "",
                TooltipType = sourceUIElement?.TooltipType ?? TooltipType.Text,
                TooltipPosition = sourceUIElement?.TooltipPosition ?? TooltipPosition.Above,
                TooltipOffsetPx = sourceUIElement?.TooltipOffsetPx ?? 30,
                EventType = UIElementEventType.None,
                LayerType = sourceUIElement?.LayerType ?? UILayerType.Default,
                IsPreventDefaultClick = false,
                IsHidden = false
            };
            entityManager.AddComponent(clonedEntity, clonedUIElement);

            // Create fresh Sprite
            var sourceSprite = sourceEntity.GetComponent<Sprite>();
            entityManager.AddComponent(clonedEntity, new Sprite
            {
                Owner = clonedEntity,
                TexturePath = sourceSprite?.TexturePath ?? string.Empty,
                SourceRectangle = null,
                Tint = Color.White,
                IsVisible = true
            });

            // Create fresh ParallaxLayer (fresh animation state)
            entityManager.AddComponent(clonedEntity, ParallaxLayer.GetUIParallaxLayer());

            // Note: Explicitly excluding transient state components:
            // - AnimatingHandToDiscard
            // - AnimatingHandToZone
            // - AnimatingHandToDrawPile
            // - CardToDiscardFlight
            // - SelectedForPayment
            // - MarkedForSpecificDiscard
            // - MarkedForReturnToDeck
            // - TooltipOverrideBackup
            // - OwnedByScene (auto-added by EntityManager)

            return clonedEntity;
        }
    }
} 