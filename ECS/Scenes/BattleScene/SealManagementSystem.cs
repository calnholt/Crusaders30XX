using System;
using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Services;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Handles sealed (petrified) effects from enemy attacks (Medusa).
	/// Sealed cards cannot be played or pledged, but CAN block.
	/// Seals count down: -1 per card played (applies to ALL sealed cards in hand),
	/// -1 when the sealed card is used to block.
	/// At 0 seals, the card is freed (Sealed removed).
	/// Pledged cards are immune to sealing.
	/// </summary>
	public class SealManagementSystem : Core.System
	{
		public SealManagementSystem(EntityManager entityManager) : base(entityManager)
		{
			EventManager.Subscribe<SealCardsEvent>(OnSealCards);
			EventManager.Subscribe<CardMoved>(OnCardMoved);
			EventManager.Subscribe<CardPlayedEvent>(OnCardPlayed);
			EventManager.Subscribe<ModifySealsEvent>(OnModifySeals);
			EventManager.Subscribe<ShuffleSealedIntoDrawPileEvent>(OnShuffleSealedIntoDrawPile);
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return Array.Empty<Entity>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		private void OnSealCards(SealCardsEvent evt)
		{
			LoggingService.Append("SealManagementSystem.OnSealCards", new System.Text.Json.Nodes.JsonObject
			{
				["sealType"] = evt.Type.ToString(),
				["amount"] = evt.Amount
			});
			switch (evt.Type)
			{
				case SealType.Hand:
					ApplySealToHand(evt.Amount);
					break;
				case SealType.TopOfDrawPile:
					ApplySealToTopOfDrawPile(evt.Amount);
					break;
			}
		}

		private void ApplySealToHand(int amount)
		{
			var cardsToSeal = GetComponentHelper.GetHandOfCards(EntityManager)
				.Where(c => c.GetComponent<Pledge>() == null) // Pledged cards are immune
				.OrderBy(x => new Random().Next())
				.Take(1)
				.ToList();

			foreach (var card in cardsToSeal)
			{
				var sealedComp = card.GetComponent<Sealed>();
				if (sealedComp == null)
				{
					EntityManager.AddComponent(card, new Sealed { Owner = card, Seals = amount });
				}
				else
				{
					sealedComp.Seals += amount;
				}
				RunScopedStateService.SyncCardRestrictionsFromComponents(card);
				var cardData = card.GetComponent<CardData>();
				LoggingService.Append("SealManagementSystem.OnApplySeal", new System.Text.Json.Nodes.JsonObject { ["cardId"] = cardData?.Card.CardId ?? "unknown", ["message"] = "card has been sealed" });
			}
		}

		private void ApplySealToTopOfDrawPile(int amount)
		{
			var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
			if (deckEntity == null) return;
			var deck = deckEntity.GetComponent<Deck>();
			if (deck?.DrawPile == null) return;

			var cardsToSeal = deck.DrawPile
				.Where(c => c.GetComponent<Pledge>() == null) // Pledged cards are immune
				.Where(c => (c.GetComponent<CardData>()?.Card.IsWeapon ?? false) == false)
				.Take(1) // Take from top of draw pile
				.ToList();

			foreach (var card in cardsToSeal)
			{
				var sealedComp = card.GetComponent<Sealed>();
				if (sealedComp == null)
				{
					EntityManager.AddComponent(card, new Sealed { Owner = card, Seals = amount });
				}
				else
				{
					sealedComp.Seals += amount;
				}
				RunScopedStateService.SyncCardRestrictionsFromComponents(card);
				var cardData = card.GetComponent<CardData>();
				LoggingService.Append("SealManagementSystem.OnCardMoved", new System.Text.Json.Nodes.JsonObject { ["cardId"] = cardData?.Card.CardId ?? "unknown", ["message"] = "card has been sealed from draw pile" });
			}
		}

		/// <summary>
		/// When a card is played, all sealed cards in hand lose 1 seal.
		/// </summary>
		private void OnCardPlayed(CardPlayedEvent evt)
		{
			LoggingService.Append("SealManagementSystem.OnCardPlayed", new System.Text.Json.Nodes.JsonObject
			{
				["cardId"] = evt.Card?.Id ?? -1
			});
			var sealedInHand = GetSealedCardsInHand();
			foreach (var card in sealedInHand)
			{
				RemoveSeals(card, 1, "card played");
			}
		}

		/// <summary>
		/// When a sealed card is used to block (moves from AssignedBlock to DiscardPile), it loses 1 seal.
		/// </summary>
		private void OnCardMoved(CardMoved evt)
		{
			LoggingService.Append("SealManagementSystem.OnCardMoved", new System.Text.Json.Nodes.JsonObject
			{
				["cardId"] = evt.Card?.Id ?? -1,
				["from"] = evt.From.ToString(),
				["to"] = evt.To.ToString()
			});
			if (evt.From == CardZoneType.AssignedBlock && evt.To == CardZoneType.DiscardPile)
			{
				var sealedComp = evt.Card.GetComponent<Sealed>();
				if (sealedComp != null)
				{
					RemoveSeals(evt.Card, 1, "used to block");
				}
			}
		}

		/// <summary>
		/// Modifies seals on all sealed cards across all zones (hand, deck, discard, exhaust).
		/// </summary>
		private void OnModifySeals(ModifySealsEvent evt)
		{
			LoggingService.Append("SealManagementSystem.OnModifySeals", new System.Text.Json.Nodes.JsonObject
			{
				["delta"] = evt.Delta
			});
			var allSealedCards = GetAllSealedCards();
			foreach (var card in allSealedCards)
			{
				var sealedComp = card.GetComponent<Sealed>();
				if (sealedComp != null)
				{
					sealedComp.Seals = Math.Max(0, sealedComp.Seals + evt.Delta);
					var cardData = card.GetComponent<CardData>();
					LoggingService.Append("SealManagementSystem.OnModifySeals", new System.Text.Json.Nodes.JsonObject { ["cardId"] = cardData?.Card.CardId ?? "unknown", ["delta"] = evt.Delta, ["newSealCount"] = sealedComp.Seals });
				}
			}
		}

		/// <summary>
		/// Shuffles all sealed cards from hand into the draw pile. Used by Basilisk Glare.
		/// </summary>
		private void OnShuffleSealedIntoDrawPile(ShuffleSealedIntoDrawPileEvent evt)
		{
			LoggingService.Append("SealManagementSystem.OnShuffleSealedIntoDrawPile", new System.Text.Json.Nodes.JsonObject
			{
				["sealedCount"] = GetSealedCardsInHand().Count()
			});
			var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
			if (deckEntity == null) return;

			var sealedInHand = GetSealedCardsInHand().ToList();
			foreach (var card in sealedInHand)
			{
				EventManager.Publish(new CardMoveRequested
				{
					Card = card,
					Deck = deckEntity,
					Destination = CardZoneType.DrawPile,
					Reason = "ShuffleSealedIntoDrawPile"
				});
				var cardData = card.GetComponent<CardData>();
				LoggingService.Append("SealManagementSystem.OnCardMoved", new System.Text.Json.Nodes.JsonObject { ["cardId"] = cardData?.Card.CardId ?? "unknown", ["message"] = "sealed card shuffled into draw pile" });
			}

			// Shuffle the draw pile
			EventManager.Publish(new DeckShuffleEvent { Deck = deckEntity });
		}

		private System.Collections.Generic.IEnumerable<Entity> GetSealedCardsInHand()
		{
			return GetComponentHelper.GetHandOfCards(EntityManager)
				.Where(c => c.GetComponent<Sealed>() != null);
		}

		private System.Collections.Generic.IEnumerable<Entity> GetAllSealedCards()
		{
			var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
			if (deckEntity == null) return Enumerable.Empty<Entity>();

			var deck = deckEntity.GetComponent<Deck>();
			if (deck == null) return Enumerable.Empty<Entity>();

			var allCards = new System.Collections.Generic.List<Entity>();

			if (deck.Hand != null) allCards.AddRange(deck.Hand);
			if (deck.DrawPile != null) allCards.AddRange(deck.DrawPile);
			if (deck.DiscardPile != null) allCards.AddRange(deck.DiscardPile);
			if (deck.ExhaustPile != null) allCards.AddRange(deck.ExhaustPile);

			return allCards.Where(c => c.GetComponent<Sealed>() != null);
		}

		private void RemoveSeals(Entity card, int amount, string reason)
		{
			var sealedComp = card.GetComponent<Sealed>();
			if (sealedComp == null) return;

			sealedComp.Seals -= amount;
			var cardData = card.GetComponent<CardData>();
			LoggingService.Append("SealManagementSystem.RemoveSeals", new System.Text.Json.Nodes.JsonObject { ["cardId"] = cardData?.Card.CardId ?? "unknown", ["amountRemoved"] = amount, ["reason"] = reason, ["sealCount"] = sealedComp.Seals });

			if (sealedComp.Seals <= 0)
			{
				FreeCard(card);
			}
		}

		private void FreeCard(Entity card)
		{
			EntityManager.RemoveComponent<Sealed>(card);
			RunScopedStateService.SyncCardRestrictionsFromComponents(card);
			var cardData = card.GetComponent<CardData>();
			LoggingService.Append("SealManagementSystem.FreeCard", new System.Text.Json.Nodes.JsonObject { ["cardId"] = cardData?.Card.CardId ?? "unknown" });
		}
	}
}
