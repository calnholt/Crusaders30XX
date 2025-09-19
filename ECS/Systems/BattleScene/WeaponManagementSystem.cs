using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Ensures the equipped weapon (marked isWeapon in JSON) is available in hand during Action,
	/// pinned at index 0, cannot be used to pay costs, and is removed from hand when used.
	/// </summary>
	public class WeaponManagementSystem : Core.System
	{
		public WeaponManagementSystem(EntityManager entityManager) : base(entityManager)
		{
			EventManager.Subscribe<ChangeBattlePhaseEvent>(OnPhaseChanged);
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return System.Array.Empty<Entity>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		private void OnPhaseChanged(ChangeBattlePhaseEvent evt)
		{
			var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
			var deck = deckEntity?.GetComponent<Deck>();
			if (deck == null) return;

			var weapon = FindOrCreateEquippedWeapon(deck);
			if (weapon == null) return;

			// If not in hand, add to hand
			if (!deck.Hand.Contains(weapon))
			{
				deck.Hand.Insert(0, weapon);
				var ui = weapon.GetComponent<UIElement>();
				if (ui != null)
				{
					ui.IsInteractable = true;
					ui.IsHovered = false;
					ui.IsClicked = false;
				}
			}
			else
			{
				// Ensure weapon stays at index 0
				int idx = deck.Hand.IndexOf(weapon);
				if (idx > 0)
				{
					deck.Hand.RemoveAt(idx);
					deck.Hand.Insert(0, weapon);
				}
			}
		}

		private Entity FindOrCreateEquippedWeapon(Deck deck)
		{
			// Use player's EquippedWeapon to spawn/find one instance
			var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
			var equipped = player?.GetComponent<EquippedWeapon>();
			if (equipped == null || string.IsNullOrWhiteSpace(equipped.WeaponId)) return null;
			if (equipped.SpawnedEntity != null) return equipped.SpawnedEntity;
			// Create a new card entity from definition id
			if (!Crusaders30XX.ECS.Data.Cards.CardDefinitionCache.TryGet(equipped.WeaponId, out var def)) return null;
			var weapon = CreateWeaponEntity(def);
			equipped.SpawnedEntity = weapon;
			return weapon;
		}

		private Entity CreateWeaponEntity(Crusaders30XX.ECS.Data.Cards.CardDefinition def)
		{
			string name = def.name ?? def.id ?? "Weapon";
			// Create minimal card like EntityFactory.CreateCard
			var e = EntityManager.CreateEntity($"Card_{name}");
			string description = def.text;
			if (!string.IsNullOrEmpty(def.text) && def.valuesParse != null && def.valuesParse.Length > 0)
			{
				for (int i = 0; i < def.valuesParse.Length; i++)
				{
						description = description.Replace($"{{{i + 1}}}", def.valuesParse[i].ToString());
				}
			}
			var cd = new CardData
			{
				Name = name,
				Description = description,
				Cost = 0,
				Type = CardData.CardType.Attack,
				Rarity = ParseRarity(def.rarity),
				ImagePath = string.Empty,
				Color = CardData.CardColor.White,
				CardCostType = CardData.CostType.NoCost,
				BlockValue = 3
			};
			cd.CostArray = new System.Collections.Generic.List<CardData.CostType>();
			if (def.cost != null)
			{
				foreach (var c in def.cost)
				{
					var ct = ParseCostType(c);
					if (ct != CardData.CostType.NoCost) cd.CostArray.Add(ct);
				}
			}
			var t = new Transform { Position = Vector2.Zero, Scale = Vector2.One };
			var s = new Sprite { TexturePath = string.Empty, IsVisible = true };
			var ui = new UIElement { Bounds = new Rectangle(0, 0, 250, 350), IsInteractable = true };
			EntityManager.AddComponent(e, cd);
			EntityManager.AddComponent(e, t);
			EntityManager.AddComponent(e, s);
			EntityManager.AddComponent(e, ui);
			return e;
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

        // Weapon color now irrelevant; derived from loadout for non-weapon cards.

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
	}
}


