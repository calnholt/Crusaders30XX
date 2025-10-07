using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// Applies incoming damage to the player: subtracts from aegis first, then from HP.
    /// Listens to ApplyEffect(Damage) events.
    /// </summary>
    public class EnemyDamageManagerSystem : Core.System
    {
        public EnemyDamageManagerSystem(EntityManager entityManager) : base(entityManager)
        {
            EventManager.Subscribe<ApplyEffect>(OnApplyEffect);
            EventManager.Subscribe<EnemyAttackImpactNow>(OnImpactNow);
            System.Console.WriteLine("[EnemyDamageManagerSystem] Subscribed to ApplyEffect, EnemyAttackImpactNow");
        }

        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return System.Array.Empty<Entity>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

        private void OnApplyEffect(ApplyEffect e)
        {
            if ((e.EffectType ?? string.Empty) != "Damage") return;
            // Accumulate incoming damage; apply on EnemyAttackImpactNow
            Console.WriteLine($"[EnemyDamageManagerSystem] ApplyEffect {e.EffectType} {e.Amount} {e.Percentage}");
            if (e.Percentage != 100 && Random.Shared.Next(0, 100) > e.Percentage) return;
            int amt = Math.Max(0, e.Amount);
            _pendingDamage += amt;
        }

        private int _pendingDamage;

        private void OnImpactNow(EnemyAttackImpactNow e)
        {
            Console.WriteLine($"[EnemyDamageManagerSystem] EnemyAttackImpactNow context={e.ContextId} pending={_pendingDamage}");
            int incoming = _pendingDamage;
            _pendingDamage = 0;
            if (incoming <= 0) return;

            var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
            if (player == null) return;

            // 1) Consume assigned block for the current context from EnemyAttackProgress
            var enemy = EntityManager.GetEntitiesWithComponent<AttackIntent>().FirstOrDefault();
            var ctx = enemy?.GetComponent<AttackIntent>()?.Planned?.FirstOrDefault()?.ContextId;
            if (!string.IsNullOrEmpty(ctx) && incoming > 0)
            {
                var prog = EntityManager.GetEntitiesWithComponent<EnemyAttackProgress>()
                    .FirstOrDefault(e2 => e2.GetComponent<EnemyAttackProgress>()?.ContextId == ctx)?.GetComponent<EnemyAttackProgress>();
                int assigned = prog?.AssignedBlockTotal ?? 0;
                int useAssigned = Math.Min(assigned, incoming);
                incoming -= useAssigned;
            }

            // 2) Then consume aegis
            if (incoming > 0)
            {
                var passives = player?.GetComponent<AppliedPassives>()?.Passives;
                if (passives == null) return;
                int prevent = passives.TryGetValue(AppliedPassiveType.Aegis, out var aegis) ? aegis : 0;
                int use = Math.Min(prevent, incoming);
                if (use > 0)
                {
                    EventManager.Publish(new UpdatePassive { Owner = player, Type = AppliedPassiveType.Aegis, Delta = -use });
                    incoming -= use;
                }
            }

            // 3) Remaining goes to HP
            if (incoming > 0)
            {
                EventManager.Publish(new ModifyHpRequestEvent { Source = enemy, Target = player, Delta = -incoming });
            }
        }
    }
}


