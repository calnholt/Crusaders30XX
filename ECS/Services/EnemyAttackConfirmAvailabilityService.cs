using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Objects.EnemyAttacks;

namespace Crusaders30XX.ECS.Services
{
	public static class EnemyAttackConfirmAvailabilityService
	{
		public static bool CanConfirmCurrentAttack(
			EntityManager entityManager,
			string contextId,
			ISet<string> confirmedContextIds = null)
		{
			return CanResolveCurrentAttackConfirm(entityManager, contextId, confirmedContextIds);
		}

		public static bool CanRequestCurrentAttackConfirm(
			EntityManager entityManager,
			string contextId,
			ISet<string> confirmedContextIds = null)
		{
			if (entityManager == null || string.IsNullOrEmpty(contextId)) return false;
			if (BattleInputGate.IsBattleInputFrozen(entityManager)) return false;

			var phase = entityManager.GetEntitiesWithComponent<PhaseState>()
				.FirstOrDefault()
				?.GetComponent<PhaseState>();
			if (phase?.Sub != SubPhase.Block) return false;
			if (confirmedContextIds != null && confirmedContextIds.Contains(contextId)) return false;
			if (!BattleInputGate.IsTutorialActionAllowed(entityManager, TutorialAction.ConfirmBlocks)) return false;

			var planned = GetPlannedAttack(entityManager, contextId);
			if (planned?.AttackDefinition == null) return false;

			int activeBlockCount = CountActiveAssignedBlockers(entityManager, contextId);
			return MeetsAttackBlockRequirement(planned.AttackDefinition.ConditionType, activeBlockCount);
		}

		public static bool CanResolveCurrentAttackConfirm(
			EntityManager entityManager,
			string contextId,
			ISet<string> confirmedContextIds = null)
		{
			return CanRequestCurrentAttackConfirm(entityManager, contextId, confirmedContextIds)
				&& !IsAnyBlockAssignmentAnimating(entityManager);
		}

		public static int CountActiveAssignedBlockers(EntityManager entityManager, string contextId)
		{
			if (entityManager == null || string.IsNullOrEmpty(contextId)) return 0;

			return entityManager.GetEntitiesWithComponent<AssignedBlockCard>()
				.Select(entity => entity.GetComponent<AssignedBlockCard>())
				.Count(assignment => assignment != null
					&& assignment.ContextId == contextId
					&& assignment.Phase != AssignedBlockCard.PhaseState.Returning);
		}

		public static bool IsAnyBlockAssignmentAnimating(EntityManager entityManager)
		{
			if (entityManager == null) return false;

			return entityManager.GetEntitiesWithComponent<AssignedBlockCard>()
				.Select(entity => entity.GetComponent<AssignedBlockCard>())
				.Any(assignment => assignment != null
					&& assignment.Phase != AssignedBlockCard.PhaseState.Idle);
		}

		private static PlannedAttack GetPlannedAttack(EntityManager entityManager, string contextId)
		{
			var enemy = entityManager.GetEntitiesWithComponent<AttackIntent>()
				.FirstOrDefault(entity => entity.GetComponent<AttackIntent>()
					?.Planned
					?.Any(attack => attack.ContextId == contextId) == true);

			return enemy
				?.GetComponent<AttackIntent>()
				?.Planned
				?.FirstOrDefault(attack => attack.ContextId == contextId);
		}

		private static bool MeetsAttackBlockRequirement(ConditionType conditionType, int activeBlockCount)
		{
			return conditionType switch
			{
				ConditionType.MustBeBlockedByAtLeast1Card => activeBlockCount >= 1,
				ConditionType.MustBeBlockedByAtLeast2Cards => activeBlockCount >= 2,
				ConditionType.MustBeBlockedByExactly1Card => activeBlockCount == 1,
				ConditionType.MustBeBlockedByExactly2Cards => activeBlockCount == 2,
				_ => true,
			};
		}
	}
}
