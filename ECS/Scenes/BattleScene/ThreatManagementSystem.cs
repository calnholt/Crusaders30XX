using System;
using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Crusaders30XX.ECS.Data.Locations;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// Manages threat for enemies:
    /// - Increases threat by +1 at end of enemy turn
    /// - Reduces threat by 1 when enemy takes attack damage
    /// - Applies Aggression passive equal to threat at start of enemy turn
    /// </summary>
    public class ThreatManagementSystem : Core.System
    {
        public ThreatManagementSystem(EntityManager entityManager) : base(entityManager)
        {
            EventManager.Subscribe<ChangeBattlePhaseEvent>(OnChangeBattlePhase);
            EventManager.Subscribe<ModifyHpRequestEvent>(OnModifyHpRequest);
        }

        private bool IsThreatDisabled()
        {
            var queuedEntity = EntityManager.GetEntity("QueuedEvents");
            var queued = queuedEntity?.GetComponent<QueuedEvents>();
            if (queued != null && LocationDefinitionCache.TryGet(queued.LocationId, out var def))
            {
                var poi = def.pointsOfInterest != null && queued.QuestIndex >= 0 && queued.QuestIndex < def.pointsOfInterest.Count 
                    ? def.pointsOfInterest[queued.QuestIndex] 
                    : null;
                return poi?.id == "desert_1";
            }
            return false;
        }

        protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
        {
            // No per-frame updates; event-driven
            return Array.Empty<Entity>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

        private void OnChangeBattlePhase(ChangeBattlePhaseEvent evt)
        {
            if (IsThreatDisabled()) return;

            if (evt.Current == SubPhase.EnemyEnd)
            {
                // Increase threat by +1 at end of enemy turn
                var enemies = EntityManager.GetEntitiesWithComponent<Enemy>()
                    .Where(e => e.HasComponent<Threat>())
                    .ToList();
                
                foreach (var enemy in enemies)
                {
                    var threat = enemy.GetComponent<Threat>();
                    if (threat != null)
                    {
                        threat.Amount = Math.Max(0, Math.Min(3, threat.Amount + 1));
                        EventManager.Publish(new ModifyThreatEvent { Target = enemy, Delta = 1 });
                        Console.WriteLine($"[ThreatManagementSystem] Enemy threat increased to {threat.Amount} at end of turn");
                    }
                }
            }
            else if (evt.Current == SubPhase.EnemyStart)
            {
                // Apply Aggression passive equal to threat at start of enemy turn
                var enemies = EntityManager.GetEntitiesWithComponent<Enemy>()
                    .Where(e => e.HasComponent<Threat>())
                    .ToList();
                
                foreach (var enemy in enemies)
                {
                    var threat = enemy.GetComponent<Threat>();
                    if (threat != null && threat.Amount > 0)
                    {
                        EventQueueBridge.EnqueueTriggerAction("ThreatManagementSystem.ApplyAggression", () =>
                        {
                            EventManager.Publish(new ApplyPassiveEvent 
                            { 
                                Target = enemy, 
                                Type = AppliedPassiveType.Aggression, 
                                Delta = threat.Amount 
                            });
                            EventManager.Publish(new JigglePulseEvent { Target = EntityManager.GetEntity("UI_ThreatTooltip") });
                            Console.WriteLine($"[ThreatManagementSystem] Applied {threat.Amount} Aggression to enemy at start of turn");
                        }, .5f);
                    }
                }
            }
        }

        private void OnModifyHpRequest(ModifyHpRequestEvent e)
        {
            if (IsThreatDisabled()) return;

            // Reduce threat by 1 when enemy takes attack damage
            if (e.DamageType == ModifyTypeEnum.Attack && e.Target != null && e.Target.HasComponent<Enemy>() && e.Delta < 0)
            {
                var threat = e.Target.GetComponent<Threat>();
                if (threat != null)
                {
                    int before = threat.Amount;
                    threat.Amount = Math.Max(0, threat.Amount - 1);
                    if (before != threat.Amount)
                    {
                        EventManager.Publish(new ModifyThreatEvent { Target = e.Target, Delta = -1 });
                        Console.WriteLine($"[ThreatManagementSystem] Enemy threat reduced from {before} to {threat.Amount} due to attack damage");
                    }
                }
            }
        }
    }
}

