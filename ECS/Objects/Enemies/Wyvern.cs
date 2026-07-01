using System.Collections.Generic;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Objects.EnemyAttacks;
using Crusaders30XX.ECS.Systems;

namespace Crusaders30XX.ECS.Objects.Enemies
{
    public class Wyvern : EnemyBase
    {
        public Wyvern(EnemyDifficulty difficulty = EnemyDifficulty.Easy) : base(difficulty)
        {
            Id = "wyvern";
            Name = "Wyvern";
            HP = 33;

            OnStartOfBattle = (entityManager) =>
            {
                EventQueueBridge.EnqueueTriggerAction("Wyvern.OnStartOfBattle", () =>
                {
                    EventManager.Publish(new ApplyPassiveEvent
                    {
                        Target = entityManager.GetEntity("Enemy"),
                        Type = AppliedPassiveType.Plunder,
                        Delta = 1
                    });
                }, AppliedPassivesManagementSystem.Duration);
            };
        }

        public override IEnumerable<string> GetAttackIds(EntityManager entityManager, int turnNumber)
        {
            if (turnNumber % 2 == 0)
                return ["wyvern_threat"];
            return ["wyvern_strike"];
        }
    }
}

public class WyvernStrike : EnemyAttackBase
{
    public WyvernStrike()
    {
        Id = "wyvern_strike";
        Name = "Talon Swipe";
        Damage = 10;
    }
}

public class WyvernThreat : EnemyAttackBase
{
    public WyvernThreat()
    {
        Id = "wyvern_threat";
        Name = "Rend & Ruin";
        Damage = 10;
        ConditionType = ConditionType.OnBlockedByAtLeast2Cards;
        Text = EnemyAttackTextHelper.GetText(EnemyAttackTextType.Custom, conditionType: ConditionType, customText: "Discards the plundered card.");

        OnAttackHit = (entityManager) =>
        {
            EventManager.Publish(new PlunderForceDiscardEvent());
        };
    }
}
