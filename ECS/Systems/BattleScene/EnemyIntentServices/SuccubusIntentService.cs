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
			var availableIds = (arsenal?.AttackIds != null && arsenal.AttackIds.Count > 0)
				? arsenal.AttackIds.ToList()
				: new List<string> { "velvet_fangs", "bewitching_kiss", "soul_siphon", "enthralling_gaze", "crushing_adoration", "teasing_nip" };
			if (availableIds.Count == 0) return Enumerable.Empty<string>();

			var defs = AttackDefinitionCache.GetAll();
			bool HasDef(string id) => defs.TryGetValue(id, out var _);
			string GetPos(string id)
			{
				return defs.TryGetValue(id, out var def) && !string.IsNullOrEmpty(def.positionType)
					? def.positionType
					: "Linker";
			}

			var startersOrLinkers = availableIds.Where(id => HasDef(id) && (GetPos(id) == "Starter" || GetPos(id) == "Linker")).ToList();
			var enders = availableIds.Where(id => HasDef(id) && GetPos(id) == "Ender" && id != "crushing_adoration").ToList();
			bool hasFinisher = availableIds.Contains("crushing_adoration");
			bool hasJab = availableIds.Contains("teasing_nip");

			// Either one attack (big finisher) or two (starter/linker + ender, not big finisher)
			bool chooseOne = _random.Next(2) == 0 && hasFinisher;
			if (chooseOne)
			{
				return ["crushing_adoration"]; // single big finisher
			}

			string ChooseRandom(IReadOnlyList<string> list) => list.Count == 0 ? null : list[_random.Next(list.Count)];
			var first = ChooseRandom(startersOrLinkers);
			if (first == null)
			{
				var fallback = availableIds.Where(id => id != "crushing_adoration").ToList();
				first = ChooseRandom(fallback);
			}
			var second = ChooseRandom(enders);
			if (second == null)
			{
				var anyEnder = availableIds.Where(id => GetPos(id) == "Ender" && id != "crushing_adoration").ToList();
				second = ChooseRandom(anyEnder);
				if (second == null)
				{
					second = ChooseRandom(availableIds);
				}
			}

			var result = new List<string>();
			if (first != null) result.Add(first);
			if (second != null && second != first) result.Add(second);

			// 10% chance to add a third attack (teasing_nip) at a random index
			if (hasJab && _random.NextDouble() < 0.10)
			{
				if (!result.Contains("teasing_nip"))
				{
					int insertIndex = _random.Next(result.Count + 1);
					result.Insert(insertIndex, "teasing_nip");
				}
			}

			if (result.Count == 0)
			{
				if (hasFinisher) return ["crushing_adoration"];
				return [availableIds[0]];
			}

			return result;
		}
	}
}



