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
					if (!card.IsWeapon) effectiveHandCount++;
				}
				else
				{
					effectiveHandCount++;
				}
			}

			int spaceLeft = System.Math.Max(0, maxHandSize - effectiveHandCount);
			int toDraw = System.Math.Min(spaceLeft, intellect);
			if (toDraw > 0)
			{
				System.Console.WriteLine($"[DrawHandSystem] DrawUpToIntellect toDraw={toDraw}");
				EventManager.Publish(new RequestDrawCardsEvent { Count = toDraw });
			}
			CheckForPlayerDeath();
		}
	}
}


