using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Data.Attacks;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Systems
{
	public class SuccubusIntentService : IEnemyIntentService
	{
		private static readonly System.Random _random = new System.Random();

		public IEnumerable<string> SelectForTurn(Entity enemy, EnemyArsenal arsenal, int turnNumber)
		{
			// Allowed attack ids for Succubus
			var allowed = new List<string> { "velvet_fangs", "bewitching_kiss", "soul_siphon", "enthralling_gaze" };
			var pool = (arsenal?.AttackIds != null && arsenal.AttackIds.Count > 0)
				? allowed.Where(id => arsenal.AttackIds.Contains(id)).ToList()
				: allowed;
			if (pool.Count == 0) return Enumerable.Empty<string>();

			// Decide number of attacks: 3 or 4. Chance of 4 increases with turn number
			double p4 = Math.Min(0.85, 0.30 + 0.08 * Math.Max(0, turnNumber)); // 30% base, +8% per turn, capped at 85%
			int count = (_random.NextDouble() < p4) ? 4 : 3;

			// Weights (duplicates allowed). Give enthralling_gaze a higher chance
			var weights = new Dictionary<string, double>();
			foreach (var id in pool)
			{
				double w = (id == "enthralling_gaze") ? 2.0 : 1.0;
				weights[id] = w;
			}

			// Sample with replacement according to weights
			var selected = new List<string>(count);
			double total = weights.Values.Sum();
			for (int i = 0; i < count; i++)
			{
				double r = _random.NextDouble() * total;
				double acc = 0;
				string pick = pool[0];
				foreach (var kv in weights)
				{
					acc += kv.Value;
					if (r <= acc) { pick = kv.Key; break; }
				}
				selected.Add(pick);
			}

			// Shuffle order
			for (int i = selected.Count - 1; i > 0; i--)
			{
				int j = _random.Next(i + 1);
				(var a, var b) = (selected[i], selected[j]);
				selected[i] = b;
				selected[j] = a;
			}

			return selected;
		}
	}
}



