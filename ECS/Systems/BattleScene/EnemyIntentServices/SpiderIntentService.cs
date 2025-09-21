using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Data.Attacks;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Systems
{
	public class SpiderIntentService : IEnemyIntentService
	{
		private readonly Random _random = new Random();

		public IEnumerable<string> SelectForTurn(Entity enemy, EnemyArsenal arsenal, int turnNumber)
		{
			var availableIds = (arsenal?.AttackIds != null && arsenal.AttackIds.Count > 0)
				? arsenal.AttackIds.ToList()
				: new List<string> { "rafterfall_ambush", "suffocating_silk", "mandible_breaker", "fang_feint" };
			if (availableIds.Count == 0) return Enumerable.Empty<string>();

			// 20% chance to use the generic fallback attack
			if (_random.NextDouble() < 0.20)
			{
				return ["rafterfall_ambush", "have_no_mercy"];
			}

			var defs = AttackDefinitionCache.GetAll();
			bool HasDef(string id) => defs.TryGetValue(id, out var _);
			string GetPos(string id)
			{
				return defs.TryGetValue(id, out var def) && !string.IsNullOrEmpty(def.positionType)
					? def.positionType
					: "Linker";
			}

			// Identify categories
			var startersOrLinkers = availableIds.Where(id => HasDef(id) && (GetPos(id) == "Starter" || GetPos(id) == "Linker")).ToList();
			var enders = availableIds.Where(id => HasDef(id) && GetPos(id) == "Ender").ToList();

			string ChooseRandom(IReadOnlyList<string> list) => list.Count == 0 ? null : list[_random.Next(list.Count)];
			var first = ChooseRandom(startersOrLinkers);
			if (first == null)
			{
				// Fallback to any available when no starter/linker
				first = ChooseRandom(availableIds);
			}
			var second = ChooseRandom(enders);
			if (second == null)
			{
				// Fallback to any ender or any available if none found
				var anyEnder = availableIds.Where(id => GetPos(id) == "Ender").ToList();
				second = ChooseRandom(anyEnder);
				if (second == null)
				{
					second = ChooseRandom(availableIds);
				}
			}

			var result = new List<string>();
			if (first != null) result.Add(first);
			if (second != null && second != first) result.Add(second);

			// Small chance to add a third quick strike (fang_feint) at a random index
			bool hasFeint = availableIds.Contains("fang_feint");
			if (hasFeint && _random.NextDouble() < 0.10)
			{
				if (!result.Contains("fang_feint"))
				{
					int insertIndex = _random.Next(result.Count + 1);
					result.Insert(insertIndex, "fang_feint");
				}
			}

			if (result.Count == 0)
			{
				return [availableIds[0]];
			}

			return result;
		}
	}
}



