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
	public interface IEnemyIntentService
	{
		IEnumerable<string> SelectForTurn(Entity enemy, EnemyArsenal arsenal, int turnNumber);
	}

	public class DemonIntentService : IEnemyIntentService
	{
		private readonly Random _random = new Random();

		public IEnumerable<string> SelectForTurn(Entity enemy, EnemyArsenal arsenal, int turnNumber)
		{

			var combinations = new List<List<string>>
			{
				new List<string> { "razor_maw" },
				new List<string> { "scorching_claw" },
				new List<string> { "infernal_execution" },

			};			
			int random = Random.Shared.Next(0, combinations.Count);
			return ArrayUtils.Shuffled(combinations[random]);
		}
	}
}



