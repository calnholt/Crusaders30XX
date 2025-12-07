using System;
using System.Collections.Generic;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;

namespace Crusaders30XX.ECS.Systems
{
	public class SandGolemIntentService : IEnemyIntentService
	{
		private readonly Random _random = new Random();

		public IEnumerable<string> SelectForTurn(Entity enemy, EnemyArsenal arsenal, int turnNumber)
		{
      return ["harden"];
		}
	}
} 