using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace Crusaders30XX.ECS.Systems
{
	[Crusaders30XX.Diagnostics.DebugTab("Combat Debug")]
	public class HandBlockInteractionSystem : Core.System
	{
		public HandBlockInteractionSystem(EntityManager entityManager) : base(entityManager) { }

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			// Operates on hand cards; return empty and drive via Update
			return System.Array.Empty<Entity>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);
			// Only during Block phase
			var phaseState = EntityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault();
			if (phaseState == null) return;
			var phase = phaseState.GetComponent<PhaseState>();
			if (phase.Sub != SubPhase.Block) return;
			// Need a current intent context
			var enemy = EntityManager.GetEntitiesWithComponent<AttackIntent>().FirstOrDefault();
			var pa = enemy?.GetComponent<AttackIntent>()?.Planned?.FirstOrDefault();
			if (pa == null || string.IsNullOrEmpty(pa.ContextId)) return;

			// Hit-test hand cards
			var deck = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault()?.GetComponent<Deck>();
			// Iterate topmost first (reverse Z)
			var handOrdered = deck.Hand.OrderByDescending(e => e.GetComponent<Transform>()?.ZOrder ?? 0).ToList();
			foreach (var card in handOrdered)
			{
				var ui = card.GetComponent<UIElement>();
				var data = card.GetComponent<CardData>();
				if (ui == null || data == null) continue;
				if (!ui.IsClicked) continue;
                // Skip weapons: they cannot be assigned as block
				try
				{
					string id = data.CardId ?? string.Empty;
					if (!string.IsNullOrEmpty(id) && Data.Cards.CardDefinitionCache.TryGet(id, out var def))
					{
						if (def.isWeapon) { break; }
					}
				}
				catch { }
				// Skip intimidated cards: they cannot be used to block
				if (card.GetComponent<Intimidated>() != null)
				{
					EventManager.Publish(new CantPlayCardMessage { Message = "Can't block with intimidated cards!" });
					break;
				}
				// Assign this card as block (always assign from hand); color from card
				int blockVal = BlockValueService.GetBlockValue(card);
				string color = data.Color.ToString();
				// Move card out of hand into AssignedBlock zone; unassign is handled by clicking assigned banner
				var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
				var t = card.GetComponent<Transform>();
				if (deckEntity != null && t != null)
				{
					// Record the click-time hand position as the return target on the AssignedBlockCard
					var startPos = t.Position;
					EventManager.Publish(new CardMoveRequested
					{
						Card = card,
						Deck = deckEntity,
						Destination = CardZoneType.AssignedBlock,
						ContextId = pa.ContextId,
						Reason = "AssignBlock"
					});
					// After move request (handled synchronously), set ReturnTargetPos so unassign knows where to go
					var abc = card.GetComponent<AssignedBlockCard>();
					if (abc != null)
					{
						abc.ReturnTargetPos = startPos;
					}
				}
				EventManager.Publish(new BlockAssignmentAdded { ContextId = pa.ContextId, Card = card, Color = color, DeltaBlock = blockVal });
				break; // Only one card per click
			}
		}

		private MouseState _prevMouse;
	}
}


