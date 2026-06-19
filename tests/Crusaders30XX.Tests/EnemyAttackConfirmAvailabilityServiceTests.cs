using System.Collections.Generic;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Objects.EnemyAttacks;
using Crusaders30XX.ECS.Services;
using Xunit;

namespace Crusaders30XX.Tests;

public class EnemyAttackConfirmAvailabilityServiceTests
{
	[Fact]
	public void Normal_attack_can_be_confirmed_with_zero_blockers()
	{
		var entityManager = CreateCombat(ConditionType.None);

		var canConfirm = EnemyAttackConfirmAvailabilityService.CanConfirmCurrentAttack(
			entityManager,
			"test-context");

		Assert.True(canConfirm);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	public void Must_block_at_least_two_cards_cannot_confirm_with_too_few_blockers(int blockerCount)
	{
		var entityManager = CreateCombat(ConditionType.MustBeBlockedByAtLeast2Cards);
		AddBlockers(entityManager, "test-context", blockerCount);

		var canConfirm = EnemyAttackConfirmAvailabilityService.CanConfirmCurrentAttack(
			entityManager,
			"test-context");

		Assert.False(canConfirm);
	}

	[Fact]
	public void Must_block_at_least_two_cards_can_confirm_with_two_idle_blockers()
	{
		var entityManager = CreateCombat(ConditionType.MustBeBlockedByAtLeast2Cards);
		AddBlockers(entityManager, "test-context", 2);

		var canConfirm = EnemyAttackConfirmAvailabilityService.CanConfirmCurrentAttack(
			entityManager,
			"test-context");

		Assert.True(canConfirm);
	}

	[Fact]
	public void Returning_blocker_does_not_allow_confirm()
	{
		var entityManager = CreateCombat(ConditionType.MustBeBlockedByAtLeast2Cards);
		AddBlockers(entityManager, "test-context", 1);
		AddBlocker(entityManager, "test-context", AssignedBlockCard.PhaseState.Returning);

		var canConfirm = EnemyAttackConfirmAvailabilityService.CanConfirmCurrentAttack(
			entityManager,
			"test-context");

		Assert.False(canConfirm);
	}

	[Theory]
	[InlineData(0, false)]
	[InlineData(1, true)]
	[InlineData(2, false)]
	public void Exact_one_card_requirement_requires_exactly_one_active_blocker(
		int blockerCount,
		bool expected)
	{
		var entityManager = CreateCombat(ConditionType.MustBeBlockedByExactly1Card);
		AddBlockers(entityManager, "test-context", blockerCount);

		var canConfirm = EnemyAttackConfirmAvailabilityService.CanConfirmCurrentAttack(
			entityManager,
			"test-context");

		Assert.Equal(expected, canConfirm);
	}

	[Fact]
	public void Confirmed_context_cannot_confirm_again()
	{
		var entityManager = CreateCombat(ConditionType.None);
		var confirmed = new HashSet<string> { "test-context" };

		var canConfirm = EnemyAttackConfirmAvailabilityService.CanConfirmCurrentAttack(
			entityManager,
			"test-context",
			confirmed);

		Assert.False(canConfirm);
	}

	private static EntityManager CreateCombat(ConditionType conditionType)
	{
		var entityManager = new EntityManager();
		var phase = entityManager.CreateEntity("PhaseState");
		var enemy = entityManager.CreateEntity("Enemy");
		var attack = new EnemyAttackBase
		{
			Id = "test-attack",
			Name = "Test Attack",
			Damage = 5,
			ConditionType = conditionType
		};

		entityManager.AddComponent(phase, new PhaseState { Sub = SubPhase.Block });
		entityManager.AddComponent(enemy, new AttackIntent
		{
			Planned =
			[
				new PlannedAttack
				{
					AttackId = attack.Id,
					ContextId = "test-context",
					AttackDefinition = attack
				}
			]
		});

		return entityManager;
	}

	private static void AddBlockers(EntityManager entityManager, string contextId, int count)
	{
		for (int i = 0; i < count; i++)
		{
			AddBlocker(entityManager, contextId, AssignedBlockCard.PhaseState.Idle);
		}
	}

	private static void AddBlocker(
		EntityManager entityManager,
		string contextId,
		AssignedBlockCard.PhaseState phase)
	{
		var card = entityManager.CreateEntity("Blocker");
		entityManager.AddComponent(card, new AssignedBlockCard
		{
			ContextId = contextId,
			Phase = phase,
			BlockAmount = 1
		});
	}
}
