using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// Applies incoming damage to the player: subtracts from StoredBlock first, then from HP.
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
            int amt = System.Math.Max(0, e.Amount);
            _pendingDamage += amt;
        }

        private int _pendingDamage;

        private void OnImpactNow(EnemyAttackImpactNow e)
        {
            System.Console.WriteLine($"[EnemyDamageManagerSystem] EnemyAttackImpactNow context={e.ContextId} pending={_pendingDamage}");
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
                int useAssigned = System.Math.Min(assigned, incoming);
                incoming -= useAssigned;
            }

            // 2) Then consume StoredBlock
            if (incoming > 0)
            {
                var stored = player.GetComponent<StoredBlock>();
                int sb = stored?.Amount ?? 0;
                int use = System.Math.Min(sb, incoming);
                if (use > 0)
                {
                    EventManager.Publish(new ModifyStoredBlock { Delta = -use });
                    incoming -= use;
                }
            }

            // 3) Remaining goes to HP
            if (incoming > 0)
            {
                EventManager.Publish(new ModifyHpEvent { Source = enemy, Target = player, Delta = -incoming });
            }
        }
    }
}


