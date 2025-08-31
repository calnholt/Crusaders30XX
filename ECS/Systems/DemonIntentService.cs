using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Data.Attacks;
using Crusaders30XX.ECS.Core;

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
			current.Planned.Clear();
			next.Planned.Clear();

			// Example sequencing based on turnNumber using the arsenal order
			// Turn 1: first two attacks
			// Turn 2: second and first (swap)
			// Turn 3+: cycle through pairs
			var ids = arsenal.AttackIds.ToList();
			if (ids.Count == 0) return;

			IEnumerable<string> SelectForTurn(int t)
			{
				int phase = ((t % 3) + 3) % 3; // 0,1,2 cycling
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
				int index = 0;
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
					EventManager.Publish(new Events.IntentPlanned
					{
						AttackId = id,
						ContextId = ctx,
						Step = System.Math.Max(1, index + 1),
						TelegraphText = def.name
					});
					index++;
				}
			}

			AddPlanned(SelectForTurn(Math.Max(0, turnNumber - 1)), current);
			AddPlanned(SelectForTurn(Math.Max(0, turnNumber)), next);
		}
	}
}



