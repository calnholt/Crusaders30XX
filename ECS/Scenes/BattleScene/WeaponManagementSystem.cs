using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Crusaders30XX.ECS.Data.Cards;
using System.Collections.Generic;

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

			bool isPlayerTurn = evt.Current == SubPhase.PlayerStart || evt.Current == SubPhase.Action || evt.Current == SubPhase.PlayerEnd;

			if (isPlayerTurn)
			{
				// Ensure a weapon exists and is at hand index 0
				var weapon = FindOrCreateEquippedWeapon(deck);
				if (weapon == null) return;
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
			else
			{
				// Remove weapon from hand during non-player phases, if it exists
				var weapon = GetEquippedWeaponEntityIfSpawned();
				if (weapon != null && deck.Hand.Contains(weapon))
				{
					deck.Hand.Remove(weapon);
					var ui = weapon.GetComponent<UIElement>();
					if (ui != null)
					{
						ui.IsInteractable = false;
						ui.IsHovered = false;
						ui.IsClicked = false;
					}
				}
			}
		}

		private Entity GetEquippedWeaponEntityIfSpawned()
		{
			var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
			var equipped = player?.GetComponent<EquippedWeapon>();
			return equipped?.SpawnedEntity;
		}

		private Entity FindOrCreateEquippedWeapon(Deck deck)
		{
			// Use player's EquippedWeapon to spawn/find one instance
			var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
			var equipped = player?.GetComponent<EquippedWeapon>();
			if (equipped == null || string.IsNullOrWhiteSpace(equipped.WeaponId)) return null;
			if (equipped.SpawnedEntity != null) return equipped.SpawnedEntity;
			// Create a new card entity from definition id
			if (!CardDefinitionCache.TryGet(equipped.WeaponId, out var def)) return null;
			var weapon = CreateWeaponEntity(def);
			equipped.SpawnedEntity = weapon;
			return weapon;
		}

			private Entity CreateWeaponEntity(CardDefinition def)
		{
			string name = def.name ?? def.id ?? "Weapon";
			// Create minimal card like EntityFactory.CreateCard
			var e = EntityManager.CreateEntity($"Card_{name}");
			var cd = new CardData
			{
					CardId = def.id,
					Color = CardData.CardColor.Yellow
			};
			var t = new Transform { Position = Vector2.Zero, Scale = Vector2.One };
			var s = new Sprite { TexturePath = string.Empty, IsVisible = true };
			var ui = new UIElement { Bounds = new Rectangle(0, 0, 250, 350), IsInteractable = true };
			EntityManager.AddComponent(e, cd);
			EntityManager.AddComponent(e, t);
			EntityManager.AddComponent(e, s);
			EntityManager.AddComponent(e, ui);
			return e;
		}

	}
}


