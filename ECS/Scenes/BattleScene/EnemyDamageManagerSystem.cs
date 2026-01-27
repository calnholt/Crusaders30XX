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
    /// Applies incoming damage to the player: consumes assigned block, then publishes ModifyHpRequestEvent.
    /// Listens to ApplyEffect(Damage) events.
    /// </summary>
    public class EnemyDamageManagerSystem : Core.System
    {
        public EnemyDamageManagerSystem(EntityManager entityManager) : base(entityManager)
        {
            EventManager.Subscribe<ApplyEffect>(OnApplyEffect);
            EventManager.Subscribe<EnemyAttackImpactNow>(OnImpactNow);
            Console.WriteLine("[EnemyDamageManagerSystem] Subscribed to ApplyEffect, EnemyAttackImpactNow");
        }

        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return Array.Empty<Entity>();
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
            int baseDamage = _pendingDamage;
            _pendingDamage = 0;

            // Resolve assigned block for this specific context
            int assignedBlock = 0;
            var prog = EntityManager.GetEntitiesWithComponent<EnemyAttackProgress>()
                .FirstOrDefault(ent => ent.GetComponent<EnemyAttackProgress>()?.ContextId == e.ContextId)
                ?.GetComponent<EnemyAttackProgress>();
            
            if (prog != null) assignedBlock = prog.AssignedBlockTotal;

            bool willHit = baseDamage > assignedBlock;

            // Phase 1: Pre-resolution (allows conditional effects to fire)
            EventManager.Publish(new ResolvingEnemyDamageEvent 
            { 
                ContextId = e.ContextId, 
                BaseDamage = baseDamage, 
                AssignedBlock = assignedBlock, 
                WillHit = willHit 
            });

            // Phase 2: Apply total damage
            int extraDamage = _pendingDamage;
            _pendingDamage = 0;
            int totalDamage = baseDamage + extraDamage;
            int finalDamage = totalDamage;
            bool wasHit = false;

            var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
            if (player != null && totalDamage > 0)
            {
                int useAssigned = Math.Min(assignedBlock, totalDamage);
                finalDamage -= useAssigned;

                if (finalDamage > 0)
                {
                    var enemy = EntityManager.GetEntitiesWithComponent<AttackIntent>().FirstOrDefault();
                    bool ignoresAegis = prog?.IgnoresAegis ?? false;
                    int effectiveAegis = ignoresAegis ? 0 : prog?.AegisTotal ?? 0;
                    wasHit = finalDamage - effectiveAegis > 0;
                    Console.WriteLine($"[EnemyDamageManagerSystem] ModifyHpRequestEvent finalDamage={finalDamage} aegisTotal={prog?.AegisTotal} ignoresAegis={ignoresAegis} wasHit={wasHit}");
                    EventManager.Publish(new ModifyHpRequestEvent
                    {
                        Source = enemy,
                        Target = player,
                        Delta = -finalDamage,
                        IgnoresAegis = ignoresAegis
                    });
                }
            }

            EventManager.Publish(new EnemyDamageAppliedEvent
            {
                ContextId = e.ContextId,
                FinalDamage = finalDamage,
                WasHit = wasHit
            });
        }
    }
}


