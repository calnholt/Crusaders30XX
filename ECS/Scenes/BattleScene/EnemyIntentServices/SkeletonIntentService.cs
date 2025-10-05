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
	public class SkeletonIntentService : IEnemyIntentService
	{
		private static readonly System.Random _random = new System.Random();

		public IEnumerable<string> SelectForTurn(Entity enemy, EnemyArsenal arsenal, int turnNumber)
		{
			int random = Random.Shared.Next(0, 100);
      var linkers = new List<string> { "bone_strike", "sweep", "slash", "calcify" };
      if (random <= 50)
      {
        return ArrayUtils.TakeRandomWithReplacement(linkers, 3);
      }
      random = Random.Shared.Next(0, 100);
      var linker = ArrayUtils.TakeRandomWithReplacement(linkers, 1);
      if (random >= 90)
      {
        return ArrayUtils.Shuffled(linker.Append("have_no_mercy"));
      }
			return ArrayUtils.Shuffled(linker.Append("skull_crusher"));
		}
	}
}



