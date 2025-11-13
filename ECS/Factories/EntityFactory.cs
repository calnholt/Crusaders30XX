using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Data.Enemies;
using Crusaders30XX.ECS.Data.Temperance;
using Crusaders30XX.ECS.Data.Equipment;
using Crusaders30XX.ECS.Data.Medals;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Data.Loadouts;
using Crusaders30XX.ECS.Data.Cards;
using System;
using Crusaders30XX.ECS.Systems;
using Crusaders30XX.ECS.Data.Locations;
using Crusaders30XX.ECS.Services;

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
            var loadout = LoadoutDefinitionCache.TryGet("loadout_1", out var def) ? def : null;
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
            world.AddComponent(entity, ParallaxLayer.GetCharacterParallaxLayer());
            // Equip default weapon (not in deck)
            world.AddComponent(entity, new EquippedWeapon { WeaponId = loadout.weaponId });
            var equipHeadEntity = world.CreateEntity("Equip_Head");
            world.AddComponent(equipHeadEntity, new Transform { Position = new Vector2(0, 0), ZOrder = 10001 });
            world.AddComponent(equipHeadEntity, new UIElement { IsInteractable = true });
            world.AddComponent(equipHeadEntity, new EquippedEquipment { EquippedOwner = entity, EquipmentId = loadout.headId, EquipmentType = "Head" });
            world.AddComponent(equipHeadEntity, ParallaxLayer.GetUIParallaxLayer());
            var equipLegsEntity = world.CreateEntity("Equip_Legs");
            world.AddComponent(equipLegsEntity, new Transform { Position = new Vector2(0, 0), ZOrder = 10001 });
            world.AddComponent(equipLegsEntity, new UIElement { IsInteractable = true });
            world.AddComponent(equipLegsEntity, new EquippedEquipment { EquippedOwner = entity, EquipmentId = loadout.legsId, EquipmentType = "Legs" });
            world.AddComponent(equipLegsEntity, ParallaxLayer.GetUIParallaxLayer());
            var equipArmsEntity = world.CreateEntity("Equip_Arms");
            world.AddComponent(equipArmsEntity, new Transform { Position = new Vector2(0, 0), ZOrder = 10001 });
            world.AddComponent(equipArmsEntity, new UIElement { IsInteractable = true });
            world.AddComponent(equipArmsEntity, new EquippedEquipment { EquippedOwner = entity, EquipmentId = loadout.armsId, EquipmentType = "Arms" });
            world.AddComponent(equipArmsEntity, ParallaxLayer.GetUIParallaxLayer());
            var equipChestEntity = world.CreateEntity("Equip_Chest");
            world.AddComponent(equipChestEntity, new Transform { Position = new Vector2(0, 0), ZOrder = 10001 });
            world.AddComponent(equipChestEntity, new UIElement { IsInteractable = true });
            world.AddComponent(equipChestEntity, new EquippedEquipment { EquippedOwner = entity, EquipmentId = loadout.chestId, EquipmentType = "Chest" });
            world.AddComponent(equipChestEntity, ParallaxLayer.GetUIParallaxLayer());
            // Parallax handled by UI root in EquipmentDisplaySystem
            // Attach Courage resource component by default (optional mechanics can read presence)
            world.AddComponent(entity, new Courage { Amount = 0 });
            // Attach Temperance resource component by default
            world.AddComponent(entity, new Temperance { Amount = 0 });
            // Attach EquipmentUsedState tracker
            world.AddComponent(entity, new EquipmentUsedState());
            // Attach Action Points component by default
            world.AddComponent(entity, new ActionPoints { Current = 0 });
            // Attach HP component
            world.AddComponent(entity, new HP { Max = 25, Current = 25 });
            // Equip default medals (can equip multiple later). For now, just st_luke.
            foreach (var medalId in loadout.medalIds)
            {
                var medalEntity = world.CreateEntity($"Medal_{medalId}");
                world.AddComponent(medalEntity, new EquippedMedal { EquippedOwner = entity, MedalId = medalId });
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
            world.AddComponent(courageTooltip, new UIElement { Bounds = new Rectangle(0, 0, 1, 1), Tooltip = "Courage" });

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
                CardBorderThickness = (int)Math.Max(1, Math.Round(3 * sU)),
                CardCornerRadius = (int)Math.Max(2, Math.Round(18 * sU)),
                HighlightBorderThickness = (int)Math.Max(1, Math.Round(5 * sU)),
                TextMarginX = (int)Math.Round(16 * sU),
                TextMarginY = (int)Math.Round(16 * sU),
                NameScale = 0.175f * sU,
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
			var loadout = LoadoutDefinitionCache.TryGet("loadout_1", out var lo) ? lo : null;
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
				if (!CardDefinitionCache.TryGet(cardId, out var def) || def == null) continue;
				if (def.isWeapon) continue; // weapons are not in the deck
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
            if (!CardDefinitionCache.TryGet(cardId, out var def) || def == null) return null;
            if (def.isWeapon && !allowWeapons) return null; // only non-weapons for deck/library
            string name = def.name ?? def.id ?? cardId;
            var entity = entityManager.CreateEntity($"Card_{name}_{color}_{index}");

            var cardData = new CardData
            {
                CardId = cardId,
                Color = color
            };

            var transform = new Transform { Position = new Vector2(-1000, -1000), Scale = Vector2.One };
            var sprite = new Sprite { TexturePath = string.Empty, IsVisible = true };
            var uiElement = new UIElement { Bounds = new Rectangle(-1000, -1000, 250, 350), IsInteractable = true, TooltipPosition = TooltipPosition.Above, TooltipOffsetPx = 30 };

            entityManager.AddComponent(entity, cardData);
            entityManager.AddComponent(entity, transform);
            entityManager.AddComponent(entity, sprite);
            entityManager.AddComponent(entity, uiElement);
            entityManager.AddComponent(entity, ParallaxLayer.GetUIParallaxLayer());
            entityManager.AddComponent(entity, new Hint { Text = CardHintService.GetCardHint(def, color) });
            entityManager.AddComponent(entity, new DontDestroyOnReload());
            // Set tooltip from definition (precomputed in CardDefinitionCache)
            if (!string.IsNullOrEmpty(def.tooltip))
            {
                uiElement.Tooltip = def.tooltip;
            }
            // Attach CardTooltip when data specifies a tooltip card, and mark UI tooltip type
            if (!string.IsNullOrWhiteSpace(def.cardTooltip))
            {
                entityManager.AddComponent(entity, new CardTooltip { CardId = def.cardTooltip });
                uiElement.TooltipType = TooltipType.Card;
            }
            return entity;
        }

        public static Entity CreateEnemyFromId(World world, string enemyId, List<EnemyModification> modifications = null)
        {
            var all = EnemyDefinitionCache.GetAll();
            if (!all.TryGetValue(enemyId, out var def))
            {
                Console.WriteLine($"[EntityFactory] Enemy id '{enemyId}' not found. Falling back to basic demon.");
                def = new EnemyDefinition { id = enemyId, name = enemyId, hp = 60, attackIds = new List<string>() };
            }

            var enemyEntity = world.CreateEntity($"Enemy");
            var enemy = new Enemy { Id = def.id, Name = def.name ?? def.id, Type = EnemyType.Demon, MaxHealth = def.hp, CurrentHealth = def.hp };
            var enemyTransform = new Transform { Position = new Vector2(world.EntityManager.GetEntitiesWithComponent<Player>().Any() ? 1200 : 1000, 260), Scale = Vector2.One };
            world.AddComponent(enemyEntity, enemy);
            world.AddComponent(enemyEntity, enemyTransform);
            world.AddComponent(enemyEntity, new UIElement { Tooltip = def.name, IsInteractable = false , TooltipPosition = TooltipPosition.Above });
            world.AddComponent(enemyEntity, new HP { Max = enemy.MaxHealth, Current = enemy.CurrentHealth });
            world.AddComponent(enemyEntity, new PortraitInfo { TextureWidth = 0, TextureHeight = 0, CurrentScale = 1f });
            world.AddComponent(enemyEntity, new EnemyArsenal { AttackIds = new List<string>(def.attackIds) });
            world.AddComponent(enemyEntity, new AttackIntent());
            world.AddComponent(enemyEntity, new AppliedPassives());
            world.AddComponent(enemyEntity, ParallaxLayer.GetCharacterParallaxLayer());

            var playerEntity = world.EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();

            foreach (var passive in def.passives)
            {
                Enum.TryParse<AppliedPassiveType>(passive.type, true, out var passiveType);
                Console.WriteLine($"[EntityFactory] loading passives {passiveType} {passive.amount}");
                EventManager.Publish(new ApplyPassiveEvent { Target = passive.target == "Player" ? playerEntity : enemyEntity, Delta = passive.amount, Type = passiveType});
            }

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

            return enemyEntity;
        }

		/// <summary>
		/// Create UI entities for each for-sale item in a shop.
		/// </summary>
		public static List<Entity> CreateForSale(EntityManager entityManager, IEnumerable<ForSaleItemDefinition> defs, string shopName)
		{
			var result = new List<Entity>();
			if (defs == null) return result;
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
					case "card":
					default: itemType = ForSaleItemType.Card; break;
				}

				string displayName = id;
				try
				{
					if (itemType == ForSaleItemType.Card && CardDefinitionCache.TryGet(id, out var cdef) && cdef != null)
					{
						displayName = string.IsNullOrWhiteSpace(cdef.name) ? id : cdef.name;
					}
					else if (itemType == ForSaleItemType.Medal && MedalDefinitionCache.TryGet(id, out var mdef) && mdef != null)
					{
						displayName = string.IsNullOrWhiteSpace(mdef.name) ? id : mdef.name;
					}
					else if (itemType == ForSaleItemType.Equipment && EquipmentDefinitionCache.TryGet(id, out var edef) && edef != null)
					{
						displayName = string.IsNullOrWhiteSpace(edef.name) ? id : edef.name;
					}
				}
				catch { }

				var e = entityManager.CreateEntity($"ShopItem_{id}_{idx}");
				entityManager.AddComponent(e, new Transform { Position = new Vector2(-1000, -1000), ZOrder = 10002 });
				entityManager.AddComponent(e, ParallaxLayer.GetUIParallaxLayer());
                var uiElement = new UIElement { Bounds = new Rectangle(-1000, -1000, 1, 1), IsInteractable = true };
                if (itemType == ForSaleItemType.Card)
                {
                    uiElement.TooltipType = TooltipType.Card;
                    entityManager.AddComponent(e, new CardTooltip { CardId = id });
                }
                else if (itemType == ForSaleItemType.Medal)
                {
                    uiElement.Tooltip = $"{displayName} - {fs.price} gold";
                }
                else if (itemType == ForSaleItemType.Equipment)
				entityManager.AddComponent(e, uiElement);
				entityManager.AddComponent(e, new OwnedByScene { Scene = SceneId.Shop });
				entityManager.AddComponent(e, new ForSaleItem
				{
					Id = id,
					ItemType = itemType,
					Price = fs.price,
					IsPurchased = fs.isPurchased,
					DisplayName = displayName,
					SourceShopName = shopName ?? string.Empty
				});
				result.Add(e);
				idx++;
			}
			return result;
		}
    }
} 