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

			LoggingService.Append("DrawHandSystem.DrawUpToIntellect", new System.Text.Json.Nodes.JsonObject { ["deckHandCount"] = deck.Hand.Count, ["intellect"] = intellect, ["maxHandSize"] = maxHandSize });
			int effectiveHandCount = GetEffectiveHandCountForDraw(deck.Hand, true);
			int spaceLeft = System.Math.Max(0, maxHandSize - effectiveHandCount);
			int toDraw = System.Math.Min(spaceLeft, intellect);
			LoggingService.Append("DrawHandSystem.DrawUpToIntellect.result", new System.Text.Json.Nodes.JsonObject { ["effectiveHandCount"] = effectiveHandCount, ["spaceLeft"] = spaceLeft, ["toDraw"] = toDraw });
			for (int i = 0; i < toDraw; i++)
			{
				EventQueueBridge.EnqueueTriggerAction("DrawHandSystem.DrawCard", () => EventManager.Publish(new RequestDrawCardsEvent { Count = 1 }), 0.12f);
			}
			CheckForPlayerDeath();
		}

		public static int CalculateCardsToDraw(int intellect, int maxHandSize, System.Collections.Generic.IEnumerable<Entity> hand)
		{
			return CalculateCardsToDraw(intellect, maxHandSize, GetEffectiveHandCountForDraw(hand));
		}

		public static int GetEffectiveHandCountForDraw(System.Collections.Generic.IEnumerable<Entity> hand, bool emitLogs = false)
		{
			int effectiveHandCount = 0;
			foreach (var e in hand)
			{
				string debugId = e.GetComponent<CardData>()?.Card?.CardId ?? $"entity#{e.Id}";
				bool isActive = e.IsActive;
				if (e.HasComponent<AnimatingHandToDiscard>()) { LogDrawSkip(emitLogs, debugId, "AnimatingHandToDiscard", isActive); continue; }
				if (e.HasComponent<AnimatingHandToZone>()) { LogDrawSkip(emitLogs, debugId, "AnimatingHandToZone", isActive); continue; }
				if (e.HasComponent<AnimatingHandToDrawPile>()) { LogDrawSkip(emitLogs, debugId, "AnimatingHandToDrawPile", isActive); continue; }
				// Pledged cards don't count against max hand size.
				if (e.HasComponent<Pledge>()) { LogDrawSkip(emitLogs, debugId, "Pledge", isActive); continue; }

				var cd = e.GetComponent<CardData>();
				if (cd == null) { LogDrawSkip(emitLogs, e.Id, "no CardData", isActive); continue; }
				string id = cd.Card.CardId ?? string.Empty;
				if (string.IsNullOrEmpty(id)) { LogDrawSkip(emitLogs, e.Id, "empty CardId", isActive); continue; }
				var card = CardFactory.Create(id);
				if (card != null)
				{
					if (!card.IsWeapon) { effectiveHandCount++; LogDrawCount(emitLogs, id, "non-weapon", isActive); }
					else { LogDrawSkip(emitLogs, id, "weapon", isActive); }
				}
				else
				{
					effectiveHandCount++;
					LogDrawCount(emitLogs, id, "factory returned null", isActive);
				}
			}

			return effectiveHandCount;
		}

		private static int CalculateCardsToDraw(int intellect, int maxHandSize, int effectiveHandCount)
		{
			int spaceLeft = System.Math.Max(0, maxHandSize - effectiveHandCount);
			return System.Math.Min(spaceLeft, intellect);
		}

		private static void LogDrawSkip(bool emitLogs, string cardId, string reason, bool isActive)
		{
			if (!emitLogs) return;
			LoggingService.Append("DrawHandSystem.DrawUpToIntellect.skip", new System.Text.Json.Nodes.JsonObject { ["cardId"] = cardId, ["reason"] = reason, ["isActive"] = isActive });
		}

		private static void LogDrawSkip(bool emitLogs, int entityId, string reason, bool isActive)
		{
			if (!emitLogs) return;
			LoggingService.Append("DrawHandSystem.DrawUpToIntellect.skip", new System.Text.Json.Nodes.JsonObject { ["entityId"] = entityId, ["reason"] = reason, ["isActive"] = isActive });
		}

		private static void LogDrawCount(bool emitLogs, string cardId, string reason, bool isActive)
		{
			if (!emitLogs) return;
			LoggingService.Append("DrawHandSystem.DrawUpToIntellect.count", new System.Text.Json.Nodes.JsonObject { ["cardId"] = cardId, ["reason"] = reason, ["isActive"] = isActive });
		}
	}
}


