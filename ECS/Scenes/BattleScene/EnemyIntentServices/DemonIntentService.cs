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

			if (Random.Shared.Next(0, 100) <= 25)
			{
				return ["infernal_execution"];
			}
			var combinations = new List<List<string>>
			{
				new List<string> { "razor_maw" },
				new List<string> { "razor_maw", "punishing_swipe" },
				new List<string> { "razor_maw", "scorching_claw" },
				new List<string> { "razor_maw", "rending_claw" },
				new List<string> { "punishing_swipe", "punishing_swipe" },
				new List<string> { "scorching_claw", "scorching_claw" },
				new List<string> { "rending_claw", "scorching_claw" },
				new List<string> { "gouge", "gouge", "punishing_swipe" },
				new List<string> { "gouge", "punishing_swipe", "punishing_swipe" },
				new List<string> { "punishing_swipe", "punishing_swipe", "punishing_swipe" },
				new List<string> { "gouge", "gouge", "gouge" },
				new List<string> { "gouge", "razor_maw" },
				new List<string> { "gouge", "rending_claw" },

			};			
			int random = Random.Shared.Next(0, combinations.Count);
			return ArrayUtils.Shuffled(combinations[random]);
		}
	}
}



