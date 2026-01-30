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
	/// Cracks accumulate: +1 per card played (applies to ALL sealed cards in hand),
	/// +1 when the sealed card is used to block.
	/// At 3 cracks, the card is freed (Sealed removed).
	/// Pledged cards are immune to sealing.
	/// </summary>
	public class SealManagementSystem : Core.System
	{
		private const int CRACKS_TO_FREE = 3;

		public SealManagementSystem(EntityManager entityManager) : base(entityManager)
		{
			EventManager.Subscribe<SealCardsEvent>(OnSealCards);
			EventManager.Subscribe<CardMoved>(OnCardMoved);
			EventManager.Subscribe<CardPlayedEvent>(OnCardPlayed);
			EventManager.Subscribe<ModifySealCracksEvent>(OnModifySealCracks);
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
				EntityManager.AddComponent(card, new Sealed { Owner = card, Cracks = 0 });
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
				EntityManager.AddComponent(card, new Sealed { Owner = card, Cracks = 0 });
				var cardData = card.GetComponent<CardData>();
				Console.WriteLine($"[SealManagementSystem] Card {cardData?.Card.CardId ?? "unknown"} has been sealed (from draw pile)!");
			}
		}

		/// <summary>
		/// When a card is played, all sealed cards in hand gain +1 crack.
		/// </summary>
		private void OnCardPlayed(CardPlayedEvent evt)
		{
			var sealedInHand = GetSealedCardsInHand();
			foreach (var card in sealedInHand)
			{
				AddCracks(card, 1, "card played");
			}
		}

		/// <summary>
		/// When a sealed card is used to block (moves from AssignedBlock to DiscardPile), it gains +1 crack.
		/// </summary>
		private void OnCardMoved(CardMoved evt)
		{
			if (evt.From == CardZoneType.AssignedBlock && evt.To == CardZoneType.DiscardPile)
			{
				var sealedComp = evt.Card.GetComponent<Sealed>();
				if (sealedComp != null)
				{
					AddCracks(evt.Card, 1, "used to block");
				}
			}
		}

		/// <summary>
		/// Modifies cracks on all sealed cards in hand. Used by Serpent Strike to remove cracks.
		/// </summary>
		private void OnModifySealCracks(ModifySealCracksEvent evt)
		{
			var sealedInHand = GetSealedCardsInHand();
			foreach (var card in sealedInHand)
			{
				var sealedComp = card.GetComponent<Sealed>();
				if (sealedComp != null)
				{
					sealedComp.Cracks = Math.Max(0, sealedComp.Cracks + evt.Delta);
					var cardData = card.GetComponent<CardData>();
					Console.WriteLine($"[SealManagementSystem] Card {cardData?.Card.CardId ?? "unknown"} cracks modified by {evt.Delta}, now {sealedComp.Cracks}");
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

		private void AddCracks(Entity card, int amount, string reason)
		{
			var sealedComp = card.GetComponent<Sealed>();
			if (sealedComp == null) return;

			sealedComp.Cracks += amount;
			var cardData = card.GetComponent<CardData>();
			Console.WriteLine($"[SealManagementSystem] Card {cardData?.Card.CardId ?? "unknown"} gained {amount} crack(s) ({reason}), now {sealedComp.Cracks}/{CRACKS_TO_FREE}");

			if (sealedComp.Cracks >= CRACKS_TO_FREE)
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
