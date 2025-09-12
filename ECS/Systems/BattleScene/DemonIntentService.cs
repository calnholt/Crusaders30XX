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
		void Plan(Entity enemy, EnemyArsenal arsenal, AttackIntent current, NextTurnAttackIntent next, int turnNumber, Dictionary<string, AttackDefinition> attackDefs);
	}

	public class DemonIntentService : IEnemyIntentService
	{
		public void Plan(Entity enemy, EnemyArsenal arsenal, AttackIntent current, NextTurnAttackIntent next, int turnNumber, Dictionary<string, AttackDefinition> attackDefs)
		{
			next.Planned.Clear();

			var ids = arsenal.AttackIds.ToList();
			if (ids.Count == 0) return;

			IEnumerable<string> SelectForTurn(int t)
			{
        if (t >= 6) {
          return ["bite", "swipe"];
        }
				int cycle = 3;
				int phase = ((t % cycle) + cycle) % cycle; // 0,1,2 cycling
				switch (phase)
				{
					case 0: // turn 1
						return ["bite"];
					case 1: // turn 2
						return ["swipe"];
					case 2: // turn 3
						return ["bite", "swipe"];
					default:
						return ["bite"];
				}
			}

			void AddPlanned(IEnumerable<string> attackIds, dynamic target)
			{
				int index = (target.Planned is List<PlannedAttack> l) ? l.Count : 0;
				foreach (var id in attackIds)
				{
					if (!attackDefs.TryGetValue(id, out var def)) continue;
					string ctx = Guid.NewGuid().ToString("N");
					target.Planned.Add(new PlannedAttack
					{
						AttackId = id,
						ResolveStep = System.Math.Max(1, index + 1),
						ContextId = ctx,
						WasBlocked = false
					});
					EventManager.Publish(new IntentPlanned
					{
						AttackId = id,
						ContextId = ctx,
						Step = System.Math.Max(1, index + 1),
						TelegraphText = def.name
					});
					index++;
				}
			}

			// If current is empty (e.g., first turn), plan current for this turn
			if (current.Planned.Count == 0)
			{
				AddPlanned(SelectForTurn(Math.Max(0, turnNumber - 1)), current);
			}

			// Plan next-turn preview here
			AddPlanned(SelectForTurn(Math.Max(0, turnNumber)), next);
		}
	}
}



