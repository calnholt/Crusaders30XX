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
			return EvaluateType(node.type, progress, entityManager);
		}

		private static bool EvaluateType(string type, EnemyAttackProgress progress, EntityManager entityManager)
		{
			switch (type)
			{
				case "OnHit":
					return OnHit(progress);
				case "OnBlockedBy1Card":
					return OnBlockedBy1Card(progress, entityManager);
				case "OnBlockedBy2Cards":
					return OnBlockedBy2Cards(progress, entityManager);
				default:
					return false;
			}
		}

		private static bool OnHit(EnemyAttackProgress progress)
		{
			if (progress == null) return false;
			return progress.AssignedBlockTotal + progress.AegisTotal >= progress.BaseDamage;
		}

		private static bool OnBlockedBy1Card(EnemyAttackProgress progress, EntityManager entityManager)
			=> OnBlockedByCards(1, progress, entityManager);

		private static bool OnBlockedBy2Cards(EnemyAttackProgress progress, EntityManager entityManager)
			=> OnBlockedByCards(2, progress, entityManager);

		/// <summary>
		/// Returns true if the attack was blocked using at least the required number of cards.
		/// Prefers the per-context EnemyAttackProgress snapshot; falls back to live AssignedBlockCard
		/// entities (optionally filtered by context) if progress is unavailable.
		/// </summary>
		private static bool OnBlockedByCards(int required, EnemyAttackProgress progress, EntityManager entityManager)
		{
			if (required <= 0) return true;

			// Primary source: snapshot from EnemyAttackProgress (per-context, stable across phases)
			if (progress != null)
			{
				if (progress.PlayedCards >= required) return true;
			}

			// Fallback: count live AssignedBlockCard entities, filtered by context if available
			var assigned = entityManager.GetEntitiesWithComponent<AssignedBlockCard>().ToList();
			if (assigned.Count == 0) return false;

			if (progress != null && !string.IsNullOrEmpty(progress.ContextId))
			{
				string ctx = progress.ContextId;
				int inContext = assigned
					.Select(e => e.GetComponent<AssignedBlockCard>())
					.Count(abc => abc != null && abc.ContextId == ctx);
				return inContext >= required;
			}

			// No progress/context: fall back to global count
			return assigned.Count >= required;
		}
	}
}


