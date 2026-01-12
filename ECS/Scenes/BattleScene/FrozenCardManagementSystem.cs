using System;
using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Handles frozen effects from enemy attacks.
	/// When an attack has effectsOnNotBlocked with type=Frozen,
	/// randomly picks cards from the player's deck/discard pile/hand and adds the Frozen component.
	/// Frozen cards cannot be played during the action phase.
	/// When a frozen card is used to block, it removes the frozen component.
	/// At the end of the enemy turn, removes all Frozen components.
	/// </summary>
	public class FrozenCardManagementSystem : Core.System
	{
		public FrozenCardManagementSystem(EntityManager entityManager) : base(entityManager)
		{
			EventManager.Subscribe<FreezeCardsEvent>(OnFreezeCards);
      EventManager.Subscribe<CardMoved>(OnCardMoved);
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return Array.Empty<Entity>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		private void OnCardMoved(CardMoved evt)
		{
			if (evt.To == CardZoneType.DiscardPile && evt.From == CardZoneType.AssignedBlock)
			{
				evt.Card.RemoveComponent<Frozen>();
			}
		}

		private void OnFreezeCards(FreezeCardsEvent evt)
		{
			switch (evt.Type)
			{
				case FreezeType.HandAndDrawPile:
					ApplyFrozenEffectHandAndDrawPile(evt.Amount);
					break;
				case FreezeType.Hand:
					ApplyFrozenEffectHand(evt.Amount);
					break;
				case FreezeType.TopXCards:
					ApplyFrozenEffectTopXCards(evt.Amount);
					break;
			}
		}

		private void ApplyFrozenEffectHand(int amount)
		{
			var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
			if (deckEntity == null) return;
			var deck = deckEntity.GetComponent<Deck>();
			if (deck == null) return;
			var cardsToFreeze = deck.Hand
				.Where(c => c.GetComponent<Frozen>() == null && (c.GetComponent<CardData>()?.Card.IsWeapon ?? false) == false && c.GetComponent<Pledge>() == null)
				.OrderBy(x => new Random().Next())
				.Take(amount)
				.ToList();
			foreach (var card in cardsToFreeze)
			{
				EntityManager.AddComponent(card, new Frozen { Owner = card });
			}
		}

		private void ApplyFrozenEffectTopXCards(int amount)
		{
			// Get player's deck
			var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
			if (deckEntity == null) return;
			var deck = deckEntity.GetComponent<Deck>();
			if (deck == null) return;
			var drawPile = deck.DrawPile;
			if (drawPile == null) return;
			var cardsToFreeze = drawPile
				.Where(c => c.GetComponent<Frozen>() == null && (c.GetComponent<CardData>()?.Card.IsWeapon ?? false) == false)
				.OrderBy(x => new Random().Next())
				.Take(amount)
				.ToList();
			foreach (var card in cardsToFreeze)
			{
				EntityManager.AddComponent(card, new Frozen { Owner = card });
			}
		}

		private void ApplyFrozenEffectHandAndDrawPile(int amount)
		{
			// Get player's deck
			var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
			if (deckEntity == null) return;

			var deck = deckEntity.GetComponent<Deck>();
			if (deck == null) return;

			// Collect all available cards from DrawPile, DiscardPile, and Hand
			var availableCards = new System.Collections.Generic.List<Entity>();
			
			if (deck.DrawPile != null)
			{
				availableCards.AddRange(deck.DrawPile.Where(c => c.GetComponent<Frozen>() == null && (c.GetComponent<CardData>()?.Card.IsWeapon ?? false) == false));
			}
			
			// if (deck.DiscardPile != null)
			// {
			// 	availableCards.AddRange(deck.DiscardPile.Where(c => c.GetComponent<Frozen>() == null));
			// }
			
			if (deck.Hand != null)
			{
				availableCards.AddRange(deck.Hand.Where(c => c.GetComponent<Frozen>() == null && (c.GetComponent<CardData>()?.Card.IsWeapon ?? false) == false && c.GetComponent<Pledge>() == null));
			}

			if (availableCards.Count == 0)
			{
				Console.WriteLine("[FrozenCardManagementSystem] No available cards to freeze");
				return;
			}

			// Randomly select cards to freeze
			var random = new Random();
			var cardsToFreeze = availableCards
				.OrderBy(x => random.Next())
				.Take(amount)
				.ToList();

			// Add Frozen component to selected cards
			foreach (var card in cardsToFreeze)
			{
				EntityManager.AddComponent(card, new Frozen { Owner = card });
				var cardData = card.GetComponent<CardData>();
				Console.WriteLine($"[FrozenCardManagementSystem] Card {cardData?.Card.CardId ?? "unknown"} has been frozen!");
			}
		}

		private void RemoveAllFrozenEffects()
		{
			// Get all cards with Frozen component
			var frozenCards = EntityManager.GetEntitiesWithComponent<Frozen>().ToList();

			foreach (var card in frozenCards)
			{
				EntityManager.RemoveComponent<Frozen>(card);
				var cardData = card.GetComponent<CardData>();
				Console.WriteLine($"[FrozenCardManagementSystem] Removed frozen from card {cardData?.Card.CardId ?? "unknown"}");
			}
		}

	}
}


