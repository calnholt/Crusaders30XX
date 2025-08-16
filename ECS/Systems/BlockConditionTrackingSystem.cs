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
			EventManager.Subscribe<BlockCardPlayed>(OnBlockCardPlayed);
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			// Stateless; returns empty
			return Array.Empty<Entity>();
		}

		protected override void UpdateEntity(Entity entity, Microsoft.Xna.Framework.GameTime gameTime) { }

		private void OnBlockCardPlayed(BlockCardPlayed e)
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
			}
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


