using System;
using System.Collections.Generic;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Objects.Enemies;
using Crusaders30XX.ECS.Systems;

namespace Crusaders30XX.ECS.Objects.EnemyAttacks;

public class Sentinel : EnemyBase
{
	private int SentinelStacks = 1;

	public Sentinel(EnemyDifficulty difficulty = EnemyDifficulty.Easy) : base(difficulty)
	{
		Id = "sentinel";
		Name = "Sentinel";
		MaxHealth = 24 + (int)difficulty * 2;

		OnStartOfBattle = (entityManager) =>
		{
			var enemy = entityManager.GetEntity("Enemy");
			entityManager.AddComponent(enemy, new GuardQueue());
			EventQueueBridge.EnqueueTriggerAction("Sentinel.OnStartOfBattle", () =>
			{
				EventManager.Publish(new ApplyPassiveEvent { Target = enemy, Type = AppliedPassiveType.Sentinel, Delta = SentinelStacks });
			}, AppliedPassivesManagementSystem.Duration);
		};
	}

	public override IEnumerable<string> GetAttackIds(EntityManager entityManager, int turnNumber)
	{
		int roll = Random.Shared.Next(0, 3);
		return roll switch
		{
			0 => ["sentinel_slam"],
			1 => ["twin_strike", "twin_strike"],
			_ => ["rapid_jab", "rapid_jab", "rapid_jab"],
		};
	}
}

public class SentinelSlam : EnemyAttackBase
{
	public SentinelSlam()
	{
		Id = "sentinel_slam";
		Name = "Sentinel Slam";
		Damage = 9;
	}
}

public class TwinStrike : EnemyAttackBase
{
	public TwinStrike()
	{
		Id = "twin_strike";
		Name = "Twin Strike";
		Damage = 5;
	}
}

public class RapidJab : EnemyAttackBase
{
	public RapidJab()
	{
		Id = "rapid_jab";
		Name = "Rapid Jab";
		Damage = 3;
	}
}
