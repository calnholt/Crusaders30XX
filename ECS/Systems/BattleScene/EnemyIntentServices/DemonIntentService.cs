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
		public IEnumerable<string> SelectForTurn(Entity enemy, EnemyArsenal arsenal, int turnNumber)
		{
			var ids = arsenal.AttackIds.ToList();
			if (ids.Count == 0) return Enumerable.Empty<string>();

			if (turnNumber >= 6)
			{
				return ["bite", "swipe"];
			}

			int cycle = 3;
			int phase = ((turnNumber % cycle) + cycle) % cycle; // 0,1,2 cycling
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
	}
}



