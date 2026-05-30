using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Services;
using Microsoft.Xna.Framework;
using System;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Manages the player's Action Points (AP):
	/// - Resets to 1 when entering the Action phase
	/// - Applies ModifyActionPointsEvent deltas with clamping at 0
	/// </summary>
	public class ActionPointManagementSystem : Core.System
	{
		public ActionPointManagementSystem(EntityManager entityManager) : base(entityManager)
		{
			EventManager.Subscribe<ChangeBattlePhaseEvent>(OnChangePhase);
			EventManager.Subscribe<ModifyActionPointsEvent>(OnModifyAp);
			EventManager.Subscribe<SetActionPointsEvent>(OnSetActionPoints);
			LoggingService.Append("ActionPointManagementSystem.ctor", new System.Text.Json.Nodes.JsonObject { ["message"] = "subscribed to ChangeBattlePhaseEvent, ModifyActionPointsEvent" });
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return Array.Empty<Entity>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		private void OnChangePhase(ChangeBattlePhaseEvent evt)
		{
			LoggingService.Append("ActionPointManagementSystem.OnChangePhase", new System.Text.Json.Nodes.JsonObject
			{
				["phase"] = evt.Current.ToString()
			});
			if (evt.Current == SubPhase.PlayerStart) {
				LoggingService.Append("ActionPointManagementSystem.OnChangePhase.PlayerStart", new System.Text.Json.Nodes.JsonObject { ["action"] = "set AP to 1" });
				var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
				var ap = player.GetComponent<ActionPoints>();
				ap.Current = 1;
			}
		}

		private void OnModifyAp(ModifyActionPointsEvent evt)
		{
			LoggingService.Append("ActionPointManagementSystem.OnModifyAp", new System.Text.Json.Nodes.JsonObject
			{
				["delta"] = evt.Delta
			});
			var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
			if (player == null) return;
			var ap = player.GetComponent<ActionPoints>();
			if (ap == null)
			{
				ap = new ActionPoints { Current = 0 };
				EntityManager.AddComponent(player, ap);
			}
			int next = ap.Current + evt.Delta;
			ap.Current = next < 0 ? 0 : next;
		}

		private void OnSetActionPoints(SetActionPointsEvent evt)
		{
			LoggingService.Append("ActionPointManagementSystem.OnSetActionPoints", new System.Text.Json.Nodes.JsonObject
			{
				["amount"] = evt.Amount
			});
			var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
			if (player == null) return;
			var ap = player.GetComponent<ActionPoints>();
			if (ap == null)
			{
				ap = new ActionPoints { Current = 0 };
				EntityManager.AddComponent(player, ap);
			}
			ap.Current = evt.Amount < 0 ? 0 : evt.Amount;
		}
	}
}


