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
            MaxHealth = 80;

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
