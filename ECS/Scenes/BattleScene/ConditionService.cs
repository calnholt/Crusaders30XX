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
		public static bool Evaluate(Condition node, EntityManager entityManager)
		{
			if (node == null) return OnHit(entityManager);
			return EvaluateType(node.type, entityManager);
		}

		private static bool EvaluateType(string type, EntityManager entityManager)
		{
			switch (type)
			{
				case "OnHit":
					return OnHit(entityManager);
				default:
					return false;
			}
		}

		private static bool OnHit(EntityManager entityManager)
		{
			var progress = entityManager.GetEntitiesWithComponent<EnemyAttackProgress>().FirstOrDefault().GetComponent<EnemyAttackProgress>();
			return progress.AssignedBlockTotal + progress.AegisTotal >= progress.BaseDamage;
		}

	}
}


