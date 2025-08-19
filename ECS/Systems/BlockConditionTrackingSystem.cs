using System;
using System.Linq;
using System.Collections.Generic;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Subscribes to player action events (e.g., BlockCardPlayed) and updates BlockProgress
	/// counters per active PlannedAttack context.
	/// </summary>
	public class BlockConditionTrackingSystem : Core.System
	{
		public BlockConditionTrackingSystem(EntityManager em) : base(em)
		{
			EventManager.Subscribe<BlockAssignmentAdded>(OnBlockAssignmentAdded);
			EventManager.Subscribe<BlockAssignmentRemoved>(OnBlockAssignmentRemoved);
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			// Stateless; returns empty
			return Array.Empty<Entity>();
		}

		protected override void UpdateEntity(Entity entity, Microsoft.Xna.Framework.GameTime gameTime) { }

		private void OnBlockAssignmentAdded(BlockAssignmentAdded e)
		{
			if (string.IsNullOrWhiteSpace(e.Color)) return;
			string color = NormalizeColorKey(e.Color);
			string counterKey = $"played_{color}"; // e.g., played_Red

			var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
			if (player == null) return;
			var progress = player.GetComponent<BlockProgress>();
			if (progress == null)
			{
				progress = new BlockProgress();
				EntityManager.AddComponent(player, progress);
			}

			// Collect active planned contexts from all enemies
			var contexts = new List<string>();
			foreach (var enemy in EntityManager.GetEntitiesWithComponent<AttackIntent>())
			{
				var intent = enemy.GetComponent<AttackIntent>();
				if (intent == null || intent.Planned == null) continue;
				foreach (var pa in intent.Planned)
				{
					if (!string.IsNullOrEmpty(pa.ContextId)) contexts.Add(pa.ContextId);
				}
			}

			foreach (var ctx in contexts)
			{
				if (!progress.Counters.TryGetValue(ctx, out var counters))
				{
					counters = new Dictionary<string, int>();
					progress.Counters[ctx] = counters;
				}
				counters[counterKey] = counters.TryGetValue(counterKey, out var val) ? val + 1 : 1;
				// Only increment generic played_cards on actual add event, not on BlockCardPlayed to avoid double count
				int currentAssigned = counters.TryGetValue("assignedBlockTotal", out var cur) ? cur : 0;
				int nextAssigned = currentAssigned + (e.DeltaBlock > 0 ? e.DeltaBlock : 0);
				counters["assignedBlockTotal"] = nextAssigned;
				counters["played_cards"] = counters.TryGetValue("played_cards", out var played) ? played + 1 : 1;
			}
		}

		private void OnBlockAssignmentRemoved(BlockAssignmentRemoved e)
		{
			if (e == null || string.IsNullOrEmpty(e.ContextId)) return;
			var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
			if (player == null) return;
			var progress = player.GetComponent<BlockProgress>();
			if (progress == null)
			{
				progress = new BlockProgress();
				EntityManager.AddComponent(player, progress);
			}
			if (!progress.Counters.TryGetValue(e.ContextId, out var counters) || counters == null)
			{
				counters = new Dictionary<string, int>();
				progress.Counters[e.ContextId] = counters;
			}

			// Maintain running total of assigned block for this context
			int currentAssigned = counters.TryGetValue("assignedBlockTotal", out var cur) ? cur : 0;
			int nextAssigned = currentAssigned + e.DeltaBlock;
			if (nextAssigned < 0) nextAssigned = 0;
			counters["assignedBlockTotal"] = nextAssigned;

			// Adjust color play count so leaf conditions revert on unassign
			if (!string.IsNullOrWhiteSpace(e.Color))
			{
				string color = NormalizeColorKey(e.Color);
				string counterKey = $"played_{color}";
				if (e.DeltaBlock < 0)
				{
					int curCount = counters.TryGetValue(counterKey, out var v) ? v : 0;
					int next = curCount - 1;
					if (next < 0) next = 0;
					counters[counterKey] = next;
				}
				// For positive DeltaBlock, OnBlockCardPlayed already incremented played_{color}
			}
			int pc = counters.TryGetValue("played_cards", out var played) ? played - 1 : 0;
			if (pc < 0) pc = 0;
			counters["played_cards"] = pc;
		}

		private static string NormalizeColorKey(string color)
		{
			// Normalize to TitleCase keys: Red, White, Black
			string c = color.Trim().ToLowerInvariant();
			switch (c)
			{
				case "r":
				case "red": return "Red";
				case "w":
				case "white": return "White";
				case "b":
				case "black": return "Black";
				default: return char.ToUpperInvariant(color[0]) + color.Substring(1);
			}
		}
	}
}


