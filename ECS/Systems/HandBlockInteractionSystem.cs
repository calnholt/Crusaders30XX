using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Crusaders30XX.ECS.Systems
{
	[Crusaders30XX.Diagnostics.DebugTab("Combat Debug")]
	public class HandBlockInteractionSystem : Core.System
	{
		public HandBlockInteractionSystem(EntityManager entityManager) : base(entityManager) { }

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			// Operates on hand cards; return empty and drive via Update
			return System.Array.Empty<Entity>();
		}

		protected override void UpdateEntity(Entity entity, Microsoft.Xna.Framework.GameTime gameTime) { }

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);
			// Only during Block phase
			var phaseState = EntityManager.GetEntitiesWithComponent<BattlePhaseState>().FirstOrDefault()?.GetComponent<BattlePhaseState>();
			if (phaseState == null || phaseState.Phase != BattlePhase.Block) return;
			// Need a current intent context
			var enemy = EntityManager.GetEntitiesWithComponent<AttackIntent>().FirstOrDefault();
			var pa = enemy?.GetComponent<AttackIntent>()?.Planned?.FirstOrDefault();
			if (pa == null || string.IsNullOrEmpty(pa.ContextId)) return;

			var mouse = Mouse.GetState();
			bool click = mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released;
			if (!click) { _prevMouse = mouse; return; }

			// Hit-test hand cards
			var deck = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault()?.GetComponent<Deck>();
			if (deck == null) { _prevMouse = mouse; return; }
			// Iterate topmost first (reverse Z)
			var handOrdered = deck.Hand.OrderByDescending(e => e.GetComponent<Transform>()?.ZOrder ?? 0).ToList();
			foreach (var card in handOrdered)
			{
				var ui = card.GetComponent<UIElement>();
				var data = card.GetComponent<CardData>();
				if (ui == null || data == null) continue;
				if (!ui.Bounds.Contains(mouse.Position)) continue;
				// Toggle assignment: simple +5/-5 for now; color from card
				int blockVal = System.Math.Max(1, data.BlockValue);
				// Check current aggregate to decide toggle
				int currentAssigned = 0;
				var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
				var progress = player?.GetComponent<BlockProgress>();
				if (progress != null && progress.Counters.TryGetValue(pa.ContextId, out var counters) && counters != null)
				{
					currentAssigned = counters.TryGetValue("assignedBlockTotal", out var v) ? v : 0;
				}
				// If this card likely already assigned, remove; otherwise add
				// We approximate by checking presence of some amount; in a full impl we would track per-card entries
				bool assigning = currentAssigned < blockVal;
				int delta = assigning ? blockVal : -blockVal;
				string color = data.Color.ToString();
				EventManager.Publish(new BlockAssignmentChanged { ContextId = pa.ContextId, Card = card, DeltaBlock = delta, Color = color });
				// Emit BlockCardPlayed for condition leaves if adding
				if (delta > 0)
				{
					EventManager.Publish(new BlockCardPlayed { Card = card, Color = color });
				}
				// Move card out of hand and attach animation component when assigning; return when unassigning
				var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
				var t = card.GetComponent<Transform>();
				if (deckEntity != null && t != null)
				{
					if (assigning)
					{
						EventManager.Publish(new CardMoveRequested
						{
							Card = card,
							Deck = deckEntity,
							Destination = CardZoneType.AssignedBlock,
							ContextId = pa.ContextId,
							Reason = "AssignBlock"
						});
					}
					else
					{
						EventManager.Publish(new CardMoveRequested
						{
							Card = card,
							Deck = deckEntity,
							Destination = CardZoneType.Hand,
							ContextId = pa.ContextId,
							Reason = "UnassignReturn"
						});
					}
				}
				break; // Only one card per click
			}
			_prevMouse = mouse;
		}

		private MouseState _prevMouse;
	}
}


