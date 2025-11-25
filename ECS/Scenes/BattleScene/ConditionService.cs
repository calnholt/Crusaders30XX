using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Data.Attacks;

namespace Crusaders30XX.ECS.Systems
{
	public static class ConditionService
	{
		public static bool Evaluate(Condition node, EntityManager entityManager, EnemyAttackProgress progress)
		{
			if (node == null) return OnHit(progress);
			return EvaluateType(node.type, progress);
		}

		private static bool EvaluateType(string type, EnemyAttackProgress progress)
		{
			switch (type)
			{
				case "OnHit":
					return OnHit(progress);
				default:
					return false;
			}
		}

		private static bool OnHit(EnemyAttackProgress progress)
		{
			if (progress == null) return false;
			return progress.AssignedBlockTotal + progress.AegisTotal >= progress.BaseDamage;
		}

	}
}


