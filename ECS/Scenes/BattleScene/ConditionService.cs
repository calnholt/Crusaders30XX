using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Objects.EnemyAttacks;

namespace Crusaders30XX.ECS.Systems
{
	public static class ConditionService
	{
		public static bool Evaluate(ConditionType conditionType, EntityManager entityManager, EnemyAttackProgress progress)
		{
			switch (conditionType)
			{
				case ConditionType.OnHit:
					return OnHit(progress);
				case ConditionType.MustBeBlockedByAtLeast1Card:
					return MustBeBlockedByCards(1, progress, entityManager, false);
				case ConditionType.MustBeBlockedByAtLeast2Cards:
					return MustBeBlockedByCards(2, progress, entityManager, false);
				case ConditionType.MustBeBlockedByExactly1Card:
					return MustBeBlockedByCards(1, progress, entityManager, true);
				case ConditionType.MustBeBlockedByExactly2Cards:
					return MustBeBlockedByCards(2, progress, entityManager, true);
				case ConditionType.OnBlockedByAtLeast1Card:
					return OnBlockedByCards(1, progress);
				case ConditionType.OnBlockedByAtLeast2Cards:
					return OnBlockedByCards(2, progress);
				default:
					return true;
			}
		}

		private static bool OnHit(EnemyAttackProgress progress)
		{
			if (progress == null) return false;
			return progress.AssignedBlockTotal + progress.AegisTotal >= progress.BaseDamage;
		}

		private static bool OnBlockedByCards(int required, EnemyAttackProgress progress)
		{
			return progress.PlayedCards >= required;
		}

		/// <summary>
		/// Returns true if the attack was blocked using at least the required number of cards.
		/// Prefers the per-context EnemyAttackProgress snapshot; falls back to live AssignedBlockCard
		/// entities (optionally filtered by context) if progress is unavailable.
		/// </summary>
		private static bool MustBeBlockedByCards(int required, EnemyAttackProgress progress, EntityManager entityManager, bool exactly)
		{
			if (exactly)
			{
				return progress.PlayedCards == required;
			}
			return progress.PlayedCards >= required;
		}
	}
}


