using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Data.Attacks;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Systems
{
	public class OgreIntentService : IEnemyIntentService
	{
		private static readonly System.Random _random = new System.Random();

		public IEnumerable<string> SelectForTurn(Entity enemy, EnemyArsenal arsenal, int turnNumber)
		{
			int random = Random.Shared.Next(0, 100);
			if (random <= 20)
			{
				return ["slam_trunk", "fake_out"];
			}
			if (random <= 40)
			{
				return ["slam_trunk", "thud"];
			}
			if (random <= 60)
			{
				return ["tree_stomp"];
			}
			if (random <= 80)
			{
				return ["pummel_into_submission"];
			}
			return ["slam_trunk", "have_no_mercy"];
		}
	}
}



