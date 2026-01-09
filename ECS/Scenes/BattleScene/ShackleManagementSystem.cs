using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Crusaders30XX.ECS.Services;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Handles shackle effects on cards.
	/// Shackled cards are linked: assigning one as block assigns all shackled cards in hand.
	/// Unassigning one unassigns all shackled cards currently assigned.
	/// All shackle components are removed at the end of the enemy turn.
	/// </summary>
	public class ShackleManagementSystem : Core.System
	{
		private bool _isProcessing = false;

		public ShackleManagementSystem(EntityManager entityManager) : base(entityManager)
		{
			EventManager.Subscribe<ShackleEvent>(OnShackle);
			EventManager.Subscribe<BlockAssignmentAdded>(OnBlockAssignmentAdded);
			EventManager.Subscribe<UnassignCardAsBlockRequested>(OnUnassignCardAsBlockRequested);
			EventManager.Subscribe<ChangeBattlePhaseEvent>(OnPhaseChanged);
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			return Array.Empty<Entity>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		private void OnShackle(ShackleEvent evt)
		{
			var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
			if (deckEntity == null) return;

			var deck = deckEntity.GetComponent<Deck>();
			if (deck == null || deck.Hand == null || deck.Hand.Count == 0) return;

			// Get cards in hand that are not already shackled
			var availableCards = deck.Hand
				.Where(c => c.GetComponent<Shackle>() == null)
				.ToList();

			if (availableCards.Count == 0) return;

			var random = new Random();
			var cardsToShackle = availableCards
				.OrderBy(x => random.Next())
				.Take(evt.Amount)
				.ToList();

			foreach (var card in cardsToShackle)
			{
				EntityManager.AddComponent(card, new Shackle { Owner = card });
				Console.WriteLine($"[ShackleManagementSystem] Card {card.Id} has been shackled!");
			}
		}

		private void OnBlockAssignmentAdded(BlockAssignmentAdded evt)
		{
			if (_isProcessing) return;
			if (evt.Card == null || evt.Card.GetComponent<Shackle>() == null) return;

			var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
			if (deckEntity == null) return;
			var deck = deckEntity.GetComponent<Deck>();

			// Find other shackled cards in hand
			var otherShackledInHand = deck.Hand
				.Where(c => c != evt.Card && c.GetComponent<Shackle>() != null)
				.ToList();

			if (otherShackledInHand.Count == 0) return;

			_isProcessing = true;
			try
			{
				foreach (var card in otherShackledInHand)
				{
					// Skip if already assigned (though they should be in hand)
					if (card.GetComponent<AssignedBlockCard>() != null) continue;

					int blockVal = BlockValueService.GetTotalBlockValue(card);
					var data = card.GetComponent<CardData>();
					string color = data?.Color.ToString() ?? "White";

					var t = card.GetComponent<Transform>();
					if (t != null)
					{
						var startPos = t.Position;
						EventManager.Publish(new CardMoveRequested
						{
							Card = card,
							Deck = deckEntity,
							Destination = CardZoneType.AssignedBlock,
							ContextId = evt.ContextId,
							Reason = "ShackleAssignBlock"
						});

						var abc = card.GetComponent<AssignedBlockCard>();
						if (abc != null)
						{
							abc.ReturnTargetPos = startPos;
						}
					}
					EventManager.Publish(new BlockAssignmentAdded 
					{ 
						ContextId = evt.ContextId, 
						Card = card, 
						Color = color, 
						DeltaBlock = blockVal 
					});
				}
			}
			finally
			{
				_isProcessing = false;
			}
		}

		private void OnUnassignCardAsBlockRequested(UnassignCardAsBlockRequested evt)
		{
			if (_isProcessing) return;
			if (evt.CardEntity == null || evt.CardEntity.GetComponent<Shackle>() == null) return;

			// Find other shackled cards that are currently assigned
			var otherShackledAssigned = EntityManager.GetEntitiesWithComponent<AssignedBlockCard>()
				.Where(e => e != evt.CardEntity && e.GetComponent<Shackle>() != null)
				.ToList();

			if (otherShackledAssigned.Count == 0) return;

			_isProcessing = true;
			try
			{
				foreach (var card in otherShackledAssigned)
				{
					EventManager.Publish(new UnassignCardAsBlockRequested { CardEntity = card });
				}
			}
			finally
			{
				_isProcessing = false;
			}
		}

		private void OnPhaseChanged(ChangeBattlePhaseEvent evt)
		{
			if (evt.Current == SubPhase.EnemyEnd)
			{
				RemoveAllShackles();
			}
		}

		private void RemoveAllShackles()
		{
			var shackledEntities = EntityManager.GetEntitiesWithComponent<Shackle>().ToList();
			foreach (var entity in shackledEntities)
			{
				EntityManager.RemoveComponent<Shackle>(entity);
				Console.WriteLine($"[ShackleManagementSystem] Removed shackle from entity {entity.Id}");
			}
		}
	}
}
