using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
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
			Console.WriteLine("[ActionPointManagementSystem] Subscribed to ChangeBattlePhaseEvent, ModifyActionPointsEvent");
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return System.Array.Empty<Entity>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		private void OnChangePhase(ChangeBattlePhaseEvent evt)
		{
			if (evt.Current == SubPhase.PlayerStart) {
				Console.WriteLine($"[ActionPointManagementSystem] OnChangePhase - set AP to 1");
				var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
				var ap = player.GetComponent<ActionPoints>();
				ap.Current = 1;
			}
		}

		private void OnModifyAp(ModifyActionPointsEvent evt)
		{
			System.Console.WriteLine($"[ActionPointManagementSystem] OnModifyAp delta={evt.Delta}");
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
	}
}


