using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Services;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Crusaders30XX.ECS.Factories;
using Crusaders30XX.ECS.Objects.Cards;

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
					// Reset to off-screen left so weapon flies in from the left
					var tween = weapon.GetComponent<PositionTween>();
					var transform = weapon.GetComponent<Transform>();
					if (tween != null && transform != null)
					{
						var cvs = EntityManager.GetEntitiesWithComponent<CardVisualSettings>().FirstOrDefault()?.GetComponent<CardVisualSettings>();
						float cardW = cvs?.CardWidth ?? 250;
						var spawnPos = new Vector2(-(cardW * 1.5f), Game1.VirtualHeight);
						tween.Current = spawnPos;
						tween.Target = spawnPos;
						tween.Initialized = true;
						transform.Position = spawnPos;
					}
					var ui = weapon.GetComponent<UIElement>();
					if (ui != null)
					{
						ui.SuppressCount = 0; // clear any phase suppression carried over
						ui.IsInteractable = true;
						ui.IsHovered = false;
						ui.IsClicked = false;
						ui.EventType = UIElementEventType.CardClicked; // restore after ResetDeckExcludingWeapon wipes it
					}

					LoggingService.Append("WeaponManagementSystem.OnPhaseChanged", new System.Text.Json.Nodes.JsonObject
					{
						["action"] = "WeaponAdded",
						["phase"] = evt.Current.ToString(),
						["handIndex"] = 0,
						["handCount"] = deck.Hand.Count,
						["visibleHandCount"] = HandStateLoggingService.CountVisibleHand(deck.Hand),
						["effectiveDrawHandCount"] = HandStateLoggingService.CountEffectiveDrawHand(deck.Hand),
						["card"] = HandStateLoggingService.BuildCardSnapshot(weapon)
					});
					HandStateLoggingService.AppendHandSnapshot("WeaponManagementSystem.HandSnapshot", deck, "WeaponAdded", evt.Current);
				}
				else
				{
					// Ensure weapon stays at index 0
					int idx = deck.Hand.IndexOf(weapon);
					if (idx > 0)
					{
						deck.Hand.RemoveAt(idx);
						deck.Hand.Insert(0, weapon);

						LoggingService.Append("WeaponManagementSystem.OnPhaseChanged", new System.Text.Json.Nodes.JsonObject
						{
							["action"] = "WeaponReordered",
							["phase"] = evt.Current.ToString(),
							["fromIndex"] = idx,
							["toIndex"] = 0,
							["handCount"] = deck.Hand.Count,
							["visibleHandCount"] = HandStateLoggingService.CountVisibleHand(deck.Hand),
							["effectiveDrawHandCount"] = HandStateLoggingService.CountEffectiveDrawHand(deck.Hand),
							["card"] = HandStateLoggingService.BuildCardSnapshot(weapon)
						});
						HandStateLoggingService.AppendHandSnapshot("WeaponManagementSystem.HandSnapshot", deck, "WeaponReordered", evt.Current);
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

					LoggingService.Append("WeaponManagementSystem.OnPhaseChanged", new System.Text.Json.Nodes.JsonObject
					{
						["action"] = "WeaponRemoved",
						["phase"] = evt.Current.ToString(),
						["handCount"] = deck.Hand.Count,
						["visibleHandCount"] = HandStateLoggingService.CountVisibleHand(deck.Hand),
						["effectiveDrawHandCount"] = HandStateLoggingService.CountEffectiveDrawHand(deck.Hand),
						["card"] = HandStateLoggingService.BuildCardSnapshot(weapon)
					});
					HandStateLoggingService.AppendHandSnapshot("WeaponManagementSystem.HandSnapshot", deck, "WeaponRemoved", evt.Current);
				}
			}
		}

		private Entity GetEquippedWeaponEntityIfSpawned()
		{
			var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
			var equipped = player?.GetComponent<EquippedWeapon>();
			if (equipped?.SpawnedEntity == null) return null;
			
			// Validate the entity still exists (it may have been destroyed)
			var existingEntity = EntityManager.GetEntity(equipped.SpawnedEntity.Id);
			if (existingEntity != null && existingEntity.IsActive)
			{
				return equipped.SpawnedEntity;
			}
			
			// Entity was destroyed, clear the reference
			equipped.SpawnedEntity = null;
			return null;
		}

		private Entity FindOrCreateEquippedWeapon(Deck deck)
		{
			// Use player's EquippedWeapon to spawn/find one instance
			var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
			var equipped = player?.GetComponent<EquippedWeapon>();
			if (equipped == null || string.IsNullOrWhiteSpace(equipped.WeaponId)) return null;
			
			// If a spawned entity exists, validate it still exists in EntityManager
			// (it may have been destroyed when the weapon was played)
			if (equipped.SpawnedEntity != null)
			{
				var existingEntity = EntityManager.GetEntity(equipped.SpawnedEntity.Id);
				if (existingEntity != null && existingEntity.IsActive)
				{
					return equipped.SpawnedEntity;
				}
				// Entity was destroyed, clear the reference so we can create a new one
				equipped.SpawnedEntity = null;
			}
			
			var card = CardFactory.Create(equipped.WeaponId);
			if (card == null) return null;
			var weapon = CreateWeaponEntity(card);
			equipped.SpawnedEntity = weapon;
			return weapon;
		}

		private Entity CreateWeaponEntity(CardBase card)
		{
			string name = card.Name ?? card.CardId ?? "Weapon";
			var e = EntityManager.CreateEntity($"Weapon");
			// Spawn off-screen left so weapon flies in from the left
			var cvs = EntityManager.GetEntitiesWithComponent<CardVisualSettings>().FirstOrDefault()?.GetComponent<CardVisualSettings>();
			float cardW = cvs?.CardWidth ?? 250;
			var spawnPos = new Vector2(-(cardW * 1.5f), Game1.VirtualHeight);
			var cd = new CardData
			{
					Card = card,
					Color = CardData.CardColor.Yellow
			};
			var t = new Transform { Position = spawnPos, Scale = Vector2.One };
			var s = new Sprite { TexturePath = string.Empty, IsVisible = true };
			var ui = new UIElement { Bounds = new Rectangle(0, 0, 250, 350), IsInteractable = true, TooltipType = TooltipType.Card, EventType = UIElementEventType.CardClicked };
			EntityManager.AddComponent(e, cd);
			EntityManager.AddComponent(e, t);
			EntityManager.AddComponent(e, s);
			EntityManager.AddComponent(e, ui);
			EntityManager.AddComponent(e, ParallaxLayer.GetUIParallaxLayer());
			EntityManager.AddComponent(e, new PositionTween { Speed = 12f, Current = spawnPos, Target = spawnPos, Initialized = true });
			EntityManager.AddComponent(e, new ModifiedDamage { Modifications = [] });
			// Run OnCreate callback (e.g. adds Frozen component for GlacialMaul)
			card.OnCreate?.Invoke(EntityManager, e);
			return e;
		}

	}
}


