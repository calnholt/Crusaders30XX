using System;
using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Data.Attacks;
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
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return System.Array.Empty<Entity>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		private void OnPhaseChanged(ChangeBattlePhaseEvent evt)
		{
			// When entering the Block phase, apply intimidate effects
			if (evt.Current == SubPhase.Block)
			{
				ApplyIntimidateEffects();
			}
			// At the end of the enemy turn, remove all intimidate effects
			else if (evt.Current == SubPhase.EnemyEnd)
			{
				RemoveAllIntimidateEffects();
			}
		}

		private void ApplyIntimidateEffects()
		{
			// Get the current attack
			var enemy = EntityManager.GetEntitiesWithComponent<AttackIntent>().FirstOrDefault();
			if (enemy == null) return;

			var intent = enemy.GetComponent<AttackIntent>();
			if (intent == null || intent.Planned == null || intent.Planned.Count == 0) return;

			var currentAttack = intent.Planned.FirstOrDefault();
			if (currentAttack == null) return;

			// Load attack definition
			var attackIntent = EntityManager.GetEntitiesWithComponent<AttackIntent>().FirstOrDefault().GetComponent<AttackIntent>();
			if (attackIntent == null) return;
			var def = attackIntent.Planned[0].AttackDefinition;

			// Check for intimidate effects
			if (def.effectsOnAttack == null || def.effectsOnAttack.Length == 0) return;

			var intimidateEffects = def.effectsOnAttack
				.Where(e => e.type == "Intimidate")
				.ToList();

			if (intimidateEffects.Count == 0) return;

			// Get player's hand
			var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
			if (deckEntity == null) return;

			var deck = deckEntity.GetComponent<Deck>();
			if (deck == null || deck.Hand == null || deck.Hand.Count == 0) return;

			// For each intimidate effect, randomly pick cards
			foreach (var effect in intimidateEffects)
			{
				int amount = effect.amount;
				
				// Get cards that are not already intimidated
				var availableCards = deck.Hand
					.Where(c => c.GetComponent<Intimidated>() == null)
					.ToList();

				if (availableCards.Count == 0) continue;

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
					Console.WriteLine($"[IntimidateManagementSystem] Card {card.GetComponent<CardData>().CardId} has been intimidated!");
				}
			}
		}

		private void RemoveAllIntimidateEffects()
		{
			// Get all cards with Intimidated component
			var intimidatedCards = EntityManager.GetEntitiesWithComponent<Intimidated>().ToList();

			foreach (var card in intimidatedCards)
			{
				EntityManager.RemoveComponent<Intimidated>(card);
				Console.WriteLine($"[IntimidateManagementSystem] Removed intimidation from card {card.GetComponent<CardData>().CardId}");
			}
		}
	}
}

