using System;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Objects.EnemyAttacks;
using Crusaders30XX.ECS.Systems;
using Xunit;

namespace Crusaders30XX.Tests;

public class EnemyDamageThresholdTests : IDisposable
{
    public EnemyDamageThresholdTests()
    {
        EventManager.Clear();
    }

    public void Dispose()
    {
        EventManager.Clear();
    }

    [Theory]
    [InlineData(5, 0, 0, false, 1)]
    [InlineData(5, 1, 0, false, 0)]
    [InlineData(5, 0, 1, false, 0)]
    [InlineData(5, 0, 1, true, 1)]
    public void Threshold_effect_uses_final_damage_after_prevention(
        int damage,
        int assignedBlock,
        int aegis,
        bool ignoresAegis,
        int expectedTriggers)
    {
        var attack = new ThresholdAttack(damage, minimumDamage: 5, ignoresAegis);
        var entityManager = CreateCombat(attack, assignedBlock, aegis);

        ResolveAttack(entityManager);

        Assert.Equal(expectedTriggers, attack.ThresholdTriggerCount);
    }

    [Fact]
    public void Threshold_effect_does_not_run_when_special_effect_fully_prevents_damage()
    {
        var attack = new ThresholdAttack(damage: 5, minimumDamage: 5, ignoresAegis: false);
        var entityManager = CreateCombat(attack, assignedBlock: 0, aegis: 0, fullyPreventedBySpecial: true);

        ResolveAttack(entityManager);

        Assert.Equal(0, attack.ThresholdTriggerCount);
    }

    [Fact]
    public void Threshold_and_on_hit_effects_can_both_run_once()
    {
        var attack = new ThresholdAttack(damage: 5, minimumDamage: 5, ignoresAegis: false, useOnHit: true);
        var entityManager = CreateCombat(attack, assignedBlock: 0, aegis: 0);

        ResolveAttack(entityManager);

        Assert.Equal(1, attack.ThresholdTriggerCount);
        Assert.Equal(1, attack.OnHitTriggerCount);
    }

    [Fact]
    public void Damage_threshold_text_describes_minimum_damage()
    {
        Assert.Equal(
            "If this attack deals 5 or more damage - Gain 2 burn.",
            EnemyAttackTextHelper.GetDamageThresholdText(5, "Gain 2 burn."));
    }

    private static EntityManager CreateCombat(
        ThresholdAttack attack,
        int assignedBlock,
        int aegis,
        bool fullyPreventedBySpecial = false)
    {
        var entityManager = new EntityManager();
        var enemy = entityManager.CreateEntity("Enemy");
        var player = entityManager.CreateEntity("Player");
        var progressEntity = entityManager.CreateEntity("EnemyAttackProgress[test-context]");

        entityManager.AddComponent(player, new Player());
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
        entityManager.AddComponent(progressEntity, new EnemyAttackProgress
        {
            ContextId = "test-context",
            Enemy = enemy,
            AttackId = attack.Id,
            AssignedBlockTotal = assignedBlock,
            AegisTotal = aegis,
            BaseDamage = attack.Damage,
            FullyPreventedBySpecial = fullyPreventedBySpecial,
            IgnoresAegis = attack.IgnoresAegis
        });

        _ = new AttackResolutionSystem(entityManager);
        _ = new EnemyDamageManagerSystem(entityManager);

        return entityManager;
    }

    private static void ResolveAttack(EntityManager entityManager)
    {
        EventManager.Publish(new ResolveAttack { ContextId = "test-context" });
        EventManager.Publish(new EnemyAttackImpactNow { ContextId = "test-context" });
    }

    private sealed class ThresholdAttack : EnemyAttackBase
    {
        public int ThresholdTriggerCount { get; private set; }
        public int OnHitTriggerCount { get; private set; }

        public ThresholdAttack(int damage, int minimumDamage, bool ignoresAegis, bool useOnHit = false)
        {
            Id = "threshold_test";
            Name = "Threshold Test";
            Damage = damage;
            MinimumDamageToTriggerEffect = minimumDamage;
            IgnoresAegis = ignoresAegis;
            ConditionType = useOnHit ? ConditionType.OnHit : ConditionType.None;
            OnDamageThresholdMet = _ => ThresholdTriggerCount++;
            OnAttackHit = _ => OnHitTriggerCount++;
        }
    }
}
