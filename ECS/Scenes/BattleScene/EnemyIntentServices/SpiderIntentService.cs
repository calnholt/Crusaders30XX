using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Data.Attacks;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Utils;

namespace Crusaders30XX.ECS.Systems
{
	public class SpiderIntentService : IEnemyIntentService
	{
		private readonly Random _random = new Random();

		public IEnumerable<string> SelectForTurn(Entity enemy, EnemyArsenal arsenal, int turnNumber)
		{
			var enders = new List<string> { "mandible_breaker", "rafterfall_ambush", "ten_limbs_of_death" };
			var linkers = new List<string> { "suffocating_silk", "fang_feint" };
			return ArrayUtils.Shuffled(new List<string> { enders[Random.Shared.Next(0, enders.Count)], linkers[Random.Shared.Next(0, linkers.Count)] });
		}
	}
}



