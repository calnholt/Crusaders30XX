using System;
using System.Collections.Generic;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Objects.EnemyAttacks;
using Crusaders30XX.ECS.Services;
using Crusaders30XX.ECS.Systems;

namespace Crusaders30XX.ECS.Objects.Enemies
{
    public class FireSkeleton : EnemyBase
    {
        private int Armor = 3;
        public FireSkeleton()
        {
            Id = "fire_skeleton";
            Name = "Fire Skeleton";
            MaxHealth = 75;

            EventManager.Subscribe<ChangeBattlePhaseEvent>(OnChangeBattlePhase);

            OnStartOfBattle = (entityManager) =>
            {
              EventQueueBridge.EnqueueTriggerAction("FireSkeleton.OnStartOfBattle", () =>
              {
                EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Enemy"), Type = AppliedPassiveType.Armor, Delta = Armor });
              }, AppliedPassivesManagementSystem.Duration);
              EventQueueBridge.EnqueueTriggerAction("FireSkeleton.OnStartOfBattle", () =>
              {
                EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Player"), Type = AppliedPassiveType.Enflamed, Delta = 1 });
              }, AppliedPassivesManagementSystem.Duration);
            };
        }

        private void OnChangeBattlePhase(ChangeBattlePhaseEvent evt)
        {
          if (evt.Current != SubPhase.PlayerEnd) return;
          var courage = GetComponentHelper.GetCourage(EntityManager);
          if (courage == null || courage.Amount < 4) return;
          EventManager.Publish(new PassiveTriggered { Owner = EntityManager.GetEntity("Player"), Type = AppliedPassiveType.Enflamed });
          EventManager.Publish(new ApplyPassiveEvent { Target = EntityManager.GetEntity("Player"), Type = AppliedPassiveType.Burn, Delta = 1 });
        }

        public override IEnumerable<string> GetAttackIds(EntityManager entityManager, int turnNumber)
        {
            return new Skeleton().GetAttackIds(entityManager, turnNumber);
        }

        public override void Dispose()
        {
          EventManager.Unsubscribe<ChangeBattlePhaseEvent>(OnChangeBattlePhase);
          Console.WriteLine($"[FireSkeleton] Unsubscribed from ChangeBattlePhaseEvent");
        }

    }

}