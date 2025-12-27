using System;
using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Handles intimidate effects from enemy attacks.
	/// During the block phase, if an attack has effectsOnAttack with type=Intimidate,
	/// randomly picks cards from the player's hand and adds the Intimidated component.
	/// At the end of the enemy turn, removes all Intimidate components.
	/// </summary>
	public class IntimidateManagementSystem : Core.System
	{
		public IntimidateManagementSystem(EntityManager entityManager) : base(entityManager)
		{
			EventManager.Subscribe<ChangeBattlePhaseEvent>(OnPhaseChanged);
			EventManager.Subscribe<IntimidateEvent>(OnIntimidate);
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return Array.Empty<Entity>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		private void OnPhaseChanged(ChangeBattlePhaseEvent evt)
		{
			// When entering the Block phase, apply intimidate effects
			if (evt.Current == SubPhase.EnemyEnd)
			{
				RemoveAllIntimidateEffects();
			}
		}

		private void OnIntimidate(IntimidateEvent evt)
		{
			ApplyIntimidateEffects(evt.Amount);
		}

		private void ApplyIntimidateEffects(int amount)
		{
			// Get player's hand
			var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
			if (deckEntity == null) return;

			var deck = deckEntity.GetComponent<Deck>();
			if (deck == null || deck.Hand == null || deck.Hand.Count == 0) return;

			// For each intimidate effect, randomly pick cards
				
				// Get cards that are not already intimidated
				var availableCards = deck.Hand
					.Where(c => c.GetComponent<Intimidated>() == null)
					.ToList();

				if (availableCards.Count == 0) return;

				// Randomly select cards to intimidate
				var random = new Random();
				var cardsToIntimidate = availableCards
					.OrderBy(x => random.Next())
					.Take(amount)
					.ToList();

				// Add Intimidated component to selected cards
				foreach (var card in cardsToIntimidate)
				{
					EntityManager.AddComponent(card, new Intimidated { Owner = card });
					Console.WriteLine($"[IntimidateManagementSystem] Card {card.GetComponent<CardData>().Card.CardId} has been intimidated!");
				}
		}

		private void RemoveAllIntimidateEffects()
		{
			// Get all cards with Intimidated component
			var intimidatedCards = EntityManager.GetEntitiesWithComponent<Intimidated>().ToList();

			foreach (var card in intimidatedCards)
			{
				EntityManager.RemoveComponent<Intimidated>(card);
				Console.WriteLine($"[IntimidateManagementSystem] Removed intimidation from card {card.GetComponent<CardData>().Card.CardId}");
			}
		}
	}
}

