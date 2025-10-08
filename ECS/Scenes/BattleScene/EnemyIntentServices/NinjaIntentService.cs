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
	public class NinjaIntentService : IEnemyIntentService
	{
		private static readonly System.Random _random = new System.Random();

		public IEnumerable<string> SelectForTurn(Entity enemy, EnemyArsenal arsenal, int turnNumber)
		{
            var hasSliceAndDice = false;
            var attacks = new List<string> {"slice"};
			int random = Random.Shared.Next(0, 100);
            if (random >= 90)
            {
                return ["slice", "dice", "sharpen_blade", "nightveil_guillotine"];
            }
			random = Random.Shared.Next(0, 100);
			if (random >= 50)
			{
				attacks.Add("dice");
                hasSliceAndDice = true;
			}
            // give shadow_step higher weight - maybe improve array util function?
            var linkers = new List<string> {"dusk_flick", "cloaked_reaver", "silencing_stab", "sharpen_blade", "shadow_step", "shadow_step", "shadow_step"};
			var count = (Random.Shared.Next(0, 100) >= 50 ? 1 : 0) + 2;
            attacks.AddRange(ArrayUtils.TakeRandomWithReplacement(linkers, count));
            var shuffledAttacks = ArrayUtils.Shuffled(attacks);
            random = Random.Shared.Next(0, 100);
            if (random >= 80 && hasSliceAndDice)
            {
                return shuffledAttacks.Append("nightveil_guillotine");
            }
            else if (random >= 60)
            {
                return shuffledAttacks.Append("have_no_mercy");
            }
            else if (random >= 50)
            {
                shuffledAttacks.Append(ArrayUtils.TakeRandomWithReplacement(linkers, 1).FirstOrDefault());
            }
            return shuffledAttacks;
		}
	}
}



