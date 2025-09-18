using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Data.Attacks;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Systems
{
	public interface IEnemyIntentService
	{
		IEnumerable<string> SelectForTurn(Entity enemy, EnemyArsenal arsenal, int turnNumber);
	}

	public class DemonIntentService : IEnemyIntentService
	{
		private readonly Random _random = new Random();

		public IEnumerable<string> SelectForTurn(Entity enemy, EnemyArsenal arsenal, int turnNumber)
		{
			var availableIds = arsenal.AttackIds.ToList();
			if (availableIds.Count == 0) return Enumerable.Empty<string>();

			// Load attack definitions to inspect position types
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
			var enders = availableIds.Where(id => HasDef(id) && GetPos(id) == "Ender" && id != "infernal_execution").ToList();
			bool hasInfernal = availableIds.Contains("infernal_execution");
			bool hasGouging = availableIds.Contains("gouging_jab");

			// Decide: either one attack (infernal_execution) or two (starter/linker + ender)
			bool chooseOne = _random.Next(2) == 0 && hasInfernal; // 50% chance when available
			if (chooseOne)
			{
				return ["infernal_execution"]; // single big finisher
			}

			// Build two-attack combo
			string ChooseRandom(IReadOnlyList<string> list) => list.Count == 0 ? null : list[_random.Next(list.Count)];
			var first = ChooseRandom(startersOrLinkers);
			if (first == null)
			{
				// Fallback to any non-infernal when no starters/linkers
				var fallback = availableIds.Where(id => id != "infernal_execution").ToList();
				first = ChooseRandom(fallback);
			}
			var second = ChooseRandom(enders);
			if (second == null)
			{
				// Fallback to any ender or any available if none found
				var anyEnder = availableIds.Where(id => GetPos(id) == "Ender" && id != "infernal_execution").ToList();
				second = ChooseRandom(anyEnder);
				if (second == null)
				{
					second = ChooseRandom(availableIds);
				}
			}

			var result = new List<string>();
			if (first != null) result.Add(first);
			if (second != null && second != first) result.Add(second);

			// 10% chance to add a third attack (gouging_jab) at a random index
			if (hasGouging && _random.NextDouble() < 0.10)
			{
				if (!result.Contains("gouging_jab"))
				{
					int insertIndex = _random.Next(result.Count + 1);
					result.Insert(insertIndex, "gouging_jab");
				}
			}

			// As a final safety, if nothing could be selected, default to infernal or any available
			if (result.Count == 0)
			{
				if (hasInfernal) return ["infernal_execution"];
				return [availableIds[0]];
			}

			return result;
		}
	}
}



