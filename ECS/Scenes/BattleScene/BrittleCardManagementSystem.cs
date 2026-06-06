using System;
using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Crusaders30XX.ECS.Services;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Handles brittle effects from enemy attacks.
	/// Randomly picks cards from the player's deck/draw pile/hand and adds the Brittle component.
	/// When a brittle card is the sole blocker for an attack (PlayedCards == 1), mills 1.
	/// Brittle persists for the entire run.
	/// </summary>
	public class BrittleCardManagementSystem : Core.System
	{
		public BrittleCardManagementSystem(EntityManager entityManager) : base(entityManager)
		{
			EventManager.Subscribe<BrittleCardsEvent>(OnBrittleCards);
			EventManager.Subscribe<CardBlockedEvent>(OnCardBlocked);
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return Array.Empty<Entity>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		private void OnBrittleCards(BrittleCardsEvent evt)
		{
			switch (evt.Type)
			{
				case FreezeType.HandAndDrawPile:
					ApplyBrittleEffectHandAndDrawPile(evt.Amount);
					break;
				case FreezeType.Hand:
					ApplyBrittleEffectHand(evt.Amount);
					break;
				case FreezeType.TopXCards:
					ApplyBrittleEffectTopXCards(evt.Amount);
					break;
				case FreezeType.Deck:
					ApplyBrittleEffectDeck(evt.Amount);
					break;
				case FreezeType.DrawPileAndDiscard:
					ApplyBrittleEffectDrawPileAndDiscard(evt.Amount);
					break;
			}
		}

		private void OnCardBlocked(CardBlockedEvent evt)
		{
			if (evt.Card?.GetComponent<Brittle>() == null) return;

			var contextId = evt.Card.GetComponent<AssignedBlockCard>()?.ContextId
				?? GetComponentHelper.GetContextId(EntityManager);
			if (string.IsNullOrEmpty(contextId)) return;

			var progress = EntityManager.GetEntitiesWithComponent<EnemyAttackProgress>()
				.FirstOrDefault(e => e.GetComponent<EnemyAttackProgress>()?.ContextId == contextId)
				?.GetComponent<EnemyAttackProgress>();
			if (progress == null || progress.PlayedCards != 1) return;

			EventManager.Publish(new MillCardEvent { });
		}

		private void ApplyBrittleEffectDeck(int amount)
		{
			var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
			if (deckEntity == null) return;
			var deck = deckEntity.GetComponent<Deck>();
			if (deck?.Cards == null) return;

			var cardsToBrittle = deck.Cards
				.Where(c => c != null && c.GetComponent<Brittle>() == null && (c.GetComponent<CardData>()?.Card.IsWeapon ?? false) == false)
				.OrderBy(_ => new Random().Next())
				.Take(amount)
				.ToList();
			foreach (var card in cardsToBrittle)
			{
				EntityManager.AddComponent(card, new Brittle { Owner = card });
				RunScopedStateService.SyncCardRestrictionsFromComponents(card);
			}
		}

		private void ApplyBrittleEffectHand(int amount)
		{
			var cardsToBrittle = GetComponentHelper.GetHandOfCards(EntityManager)
				.Where(c => c.GetComponent<Brittle>() == null)
				.OrderBy(x => new Random().Next())
				.Take(amount)
				.ToList();
			foreach (var card in cardsToBrittle)
			{
				EntityManager.AddComponent(card, new Brittle { Owner = card });
				RunScopedStateService.SyncCardRestrictionsFromComponents(card);
			}
		}

		private void ApplyBrittleEffectTopXCards(int amount)
		{
			var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
			if (deckEntity == null) return;
			var deck = deckEntity.GetComponent<Deck>();
			if (deck == null) return;
			var drawPile = deck.DrawPile;
			if (drawPile == null) return;
			var cardsToBrittle = drawPile
				.Where(c => c.GetComponent<Brittle>() == null && (c.GetComponent<CardData>()?.Card.IsWeapon ?? false) == false)
				.OrderBy(x => new Random().Next())
				.Take(amount)
				.ToList();
			foreach (var card in cardsToBrittle)
			{
				EntityManager.AddComponent(card, new Brittle { Owner = card });
				RunScopedStateService.SyncCardRestrictionsFromComponents(card);
			}
		}

		private void ApplyBrittleEffectDrawPileAndDiscard(int amount)
		{
			var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
			if (deckEntity == null) return;

			var deck = deckEntity.GetComponent<Deck>();
			if (deck == null) return;

			var availableCards = new System.Collections.Generic.List<Entity>();

			if (deck.DrawPile != null)
			{
				availableCards.AddRange(deck.DrawPile.Where(c => c.GetComponent<Brittle>() == null && (c.GetComponent<CardData>()?.Card.IsWeapon ?? false) == false));
			}

			if (deck.DiscardPile != null)
			{
				availableCards.AddRange(deck.DiscardPile.Where(c => c.GetComponent<Brittle>() == null && (c.GetComponent<CardData>()?.Card.IsWeapon ?? false) == false));
			}

			if (availableCards.Count == 0)
			{
				LoggingService.Append("BrittleCardManagementSystem.ApplyBrittleEffectDrawPileAndDiscard", new System.Text.Json.Nodes.JsonObject { ["message"] = "no available cards to brittle" });
				return;
			}

			var random = new Random();
			var cardsToBrittle = availableCards
				.OrderBy(x => random.Next())
				.Take(amount)
				.ToList();

			foreach (var card in cardsToBrittle)
			{
				EntityManager.AddComponent(card, new Brittle { Owner = card });
				RunScopedStateService.SyncCardRestrictionsFromComponents(card);
				var cardData = card.GetComponent<CardData>();
				LoggingService.Append("BrittleCardManagementSystem.ApplyBrittleEffectDrawPileAndDiscard.brittle", new System.Text.Json.Nodes.JsonObject { ["cardId"] = cardData?.Card.CardId ?? "unknown" });
			}
		}

		private void ApplyBrittleEffectHandAndDrawPile(int amount)
		{
			var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
			if (deckEntity == null) return;

			var deck = deckEntity.GetComponent<Deck>();
			if (deck == null) return;

			var availableCards = new System.Collections.Generic.List<Entity>();

			if (deck.DrawPile != null)
			{
				availableCards.AddRange(deck.DrawPile.Where(c => c.GetComponent<Brittle>() == null && (c.GetComponent<CardData>()?.Card.IsWeapon ?? false) == false));
			}

			if (deck.Hand != null)
			{
				availableCards.AddRange(GetComponentHelper.GetHandOfCards(EntityManager).Where(c => c.GetComponent<Brittle>() == null));
			}

			if (availableCards.Count == 0)
			{
				LoggingService.Append("BrittleCardManagementSystem.ApplyBrittleEffectHandAndDrawPile", new System.Text.Json.Nodes.JsonObject { ["message"] = "no available cards to brittle" });
				return;
			}

			var random = new Random();
			var cardsToBrittle = availableCards
				.OrderBy(x => random.Next())
				.Take(amount)
				.ToList();

			foreach (var card in cardsToBrittle)
			{
				EntityManager.AddComponent(card, new Brittle { Owner = card });
				RunScopedStateService.SyncCardRestrictionsFromComponents(card);
				var cardData = card.GetComponent<CardData>();
				LoggingService.Append("BrittleCardManagementSystem.ApplyBrittleEffectHandAndDrawPile.brittle", new System.Text.Json.Nodes.JsonObject { ["cardId"] = cardData?.Card.CardId ?? "unknown" });
			}
		}
	}
}
