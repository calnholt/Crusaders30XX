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
				.Where(c => c.GetComponent<Sealed>() == null)
				.Where(c => c.GetComponent<Pledge>() == null) // Pledged cards are immune
				.OrderBy(x => new Random().Next())
				.Take(amount)
				.ToList();

			foreach (var card in cardsToSeal)
			{
				EntityManager.AddComponent(card, new Sealed { Owner = card, Seals = 3 });
				var cardData = card.GetComponent<CardData>();
				Console.WriteLine($"[SealManagementSystem] Card {cardData?.Card.CardId ?? "unknown"} has been sealed!");
			}
		}

		private void ApplySealToTopOfDrawPile(int amount)
		{
			var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
			if (deckEntity == null) return;
			var deck = deckEntity.GetComponent<Deck>();
			if (deck?.DrawPile == null) return;

			var cardsToSeal = deck.DrawPile
				.Where(c => c.GetComponent<Sealed>() == null)
				.Where(c => c.GetComponent<Pledge>() == null) // Pledged cards are immune
				.Where(c => (c.GetComponent<CardData>()?.Card.IsWeapon ?? false) == false)
				.Take(amount) // Take from top of draw pile
				.ToList();

			foreach (var card in cardsToSeal)
			{
				EntityManager.AddComponent(card, new Sealed { Owner = card, Seals = 3 });
				var cardData = card.GetComponent<CardData>();
				Console.WriteLine($"[SealManagementSystem] Card {cardData?.Card.CardId ?? "unknown"} has been sealed (from draw pile)!");
			}
		}

		/// <summary>
		/// When a card is played, all sealed cards in hand lose 1 seal.
		/// </summary>
		private void OnCardPlayed(CardPlayedEvent evt)
		{
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
			var allSealedCards = GetAllSealedCards();
			foreach (var card in allSealedCards)
			{
				var sealedComp = card.GetComponent<Sealed>();
				if (sealedComp != null)
				{
					sealedComp.Seals = Math.Max(0, sealedComp.Seals + evt.Delta);
					var cardData = card.GetComponent<CardData>();
					Console.WriteLine($"[SealManagementSystem] Card {cardData?.Card.CardId ?? "unknown"} seals modified by {evt.Delta}, now {sealedComp.Seals}");
				}
			}
		}

		/// <summary>
		/// Shuffles all sealed cards from hand into the draw pile. Used by Basilisk Glare.
		/// </summary>
		private void OnShuffleSealedIntoDrawPile(ShuffleSealedIntoDrawPileEvent evt)
		{
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
				Console.WriteLine($"[SealManagementSystem] Sealed card {cardData?.Card.CardId ?? "unknown"} shuffled into draw pile!");
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
			Console.WriteLine($"[SealManagementSystem] Card {cardData?.Card.CardId ?? "unknown"} lost {amount} seal(s) ({reason}), now {sealedComp.Seals}");

			if (sealedComp.Seals <= 0)
			{
				FreeCard(card);
			}
		}

		private void FreeCard(Entity card)
		{
			EntityManager.RemoveComponent<Sealed>(card);
			var cardData = card.GetComponent<CardData>();
			Console.WriteLine($"[SealManagementSystem] Card {cardData?.Card.CardId ?? "unknown"} has been freed from seal!");
		}
	}
}
