using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Factories;
using Crusaders30XX.ECS.Services;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Draws cards based on phase transitions:
	/// - On StartOfBattle entry: draw up to Intellect (respect MaxHandSize)
	/// - On Block entry from phases other than StartOfBattle and ProcessEnemyAttack: draw up to Intellect
	/// </summary>
	public class DrawHandSystem : Core.System
	{

		public DrawHandSystem(EntityManager entityManager) : base(entityManager)
		{
			var s = entityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault()?.GetComponent<PhaseState>();
			EventManager.Subscribe<ChangeBattlePhaseEvent>(_ =>
			{
				// TODO: mindfog system? - kinda shoehorned in here
				if (_.Current == SubPhase.PlayerEnd)
				{
					var passives = GetComponentHelper.GetAppliedPassives(EntityManager, "Player");
					if (passives != null)
					{
						if (passives.Passives.TryGetValue(AppliedPassiveType.MindFog, out int mindFogAmount) && mindFogAmount > 0)
						{
							EventManager.Publish(new DiscardAllCardsEvent());
						}
					}
				}
				if (_.Current == SubPhase.EnemyStart)
				{
					DrawUpToIntellect();
				}
			});
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return System.Array.Empty<Entity>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
		}

		private void CheckForPlayerDeath()
		{
			var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
			var deck = deckEntity?.GetComponent<Deck>();
			if (deck == null) return;
			// Count non-weapon cards in hand
			int nonWeaponHandCount = 0;
			foreach (var e in deck.Hand)
			{
				if (e.HasComponent<AnimatingHandToDiscard>()) continue;
				if (e.HasComponent<AnimatingHandToZone>()) continue;
				if (e.HasComponent<AnimatingHandToDrawPile>()) continue;

				var cd = e.GetComponent<CardData>();
				if (cd == null) continue;
				string id = cd.Card.CardId ?? string.Empty;
				if (string.IsNullOrEmpty(id)) continue;
				var card = CardFactory.Create(id);
				if (card != null)
				{
					if (!card.IsWeapon) nonWeaponHandCount++;
				}
				else
				{
					// If we can't find the definition, count it as non-weapon (fallback)
					nonWeaponHandCount++;
				}
			}
		}

		private void DrawUpToIntellect()
		{
			var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
			if (player == null) return;
			int intellect = player.GetComponent<Intellect>()?.Value ?? 0;
			if (intellect <= 0) return;
			int maxHandSize = player.GetComponent<MaxHandSize>()?.Value ?? 0;
			var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
			var deck = deckEntity?.GetComponent<Deck>();
			if (deck == null) return;

			int effectiveHandCount = 0;
			System.Console.WriteLine($"[DrawHandSystem] DrawUpToIntellect evaluating deck.Hand.Count={deck.Hand.Count} intellect={intellect} maxHandSize={maxHandSize}");
			foreach (var e in deck.Hand)
			{
				string debugId = e.GetComponent<CardData>()?.Card?.CardId ?? $"entity#{e.Id}";
				bool isActive = e.IsActive;
				if (e.HasComponent<AnimatingHandToDiscard>()) { System.Console.WriteLine($"[DrawHandSystem]   skip {debugId} (AnimatingHandToDiscard) active={isActive}"); continue; }
				if (e.HasComponent<AnimatingHandToZone>()) { System.Console.WriteLine($"[DrawHandSystem]   skip {debugId} (AnimatingHandToZone) active={isActive}"); continue; }
				if (e.HasComponent<AnimatingHandToDrawPile>()) { System.Console.WriteLine($"[DrawHandSystem]   skip {debugId} (AnimatingHandToDrawPile) active={isActive}"); continue; }
				// Pledged cards don't count against max hand size
				if (e.HasComponent<Pledge>()) { System.Console.WriteLine($"[DrawHandSystem]   skip {debugId} (Pledge) active={isActive}"); continue; }

				var cd = e.GetComponent<CardData>();
				if (cd == null) { System.Console.WriteLine($"[DrawHandSystem]   skip entity#{e.Id} (no CardData) active={isActive}"); continue; }
				string id = cd.Card.CardId ?? string.Empty;
				if (string.IsNullOrEmpty(id)) { System.Console.WriteLine($"[DrawHandSystem]   skip entity#{e.Id} (empty CardId) active={isActive}"); continue; }
				var card = CardFactory.Create(id);
				if (card != null)
				{
					if (!card.IsWeapon) { effectiveHandCount++; System.Console.WriteLine($"[DrawHandSystem]   count {id} (non-weapon) active={isActive}"); }
					else { System.Console.WriteLine($"[DrawHandSystem]   skip {id} (weapon) active={isActive}"); }
				}
				else
				{
					effectiveHandCount++;
					System.Console.WriteLine($"[DrawHandSystem]   count {id} (factory returned null) active={isActive}");
				}
			}

			int spaceLeft = System.Math.Max(0, maxHandSize - effectiveHandCount);
			int toDraw = System.Math.Min(spaceLeft, intellect);
			System.Console.WriteLine($"[DrawHandSystem] DrawUpToIntellect effectiveHandCount={effectiveHandCount} spaceLeft={spaceLeft} toDraw={toDraw}");
			if (toDraw > 0)
			{
				EventManager.Publish(new RequestDrawCardsEvent { Count = toDraw });
			}
			CheckForPlayerDeath();
		}
	}
}


