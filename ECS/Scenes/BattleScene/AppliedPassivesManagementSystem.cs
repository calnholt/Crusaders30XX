using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Crusaders30XX.Diagnostics;
using System;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// Processes start-of-turn applied passives for player and enemy.
    /// Currently supports Burn: deals 1 damage to the owner per stack at start of their turn.
    /// </summary>
    public class AppliedPassivesManagementSystem : Core.System
    {
        public AppliedPassivesManagementSystem(EntityManager entityManager) : base(entityManager)
        {
            EventManager.Subscribe<ChangeBattlePhaseEvent>(OnChangeBattlePhase);
            EventManager.Subscribe<ApplyPassiveEvent>(OnApplyPassive);
            EventManager.Subscribe<ApplyEffect>(OnApplyEffect);
            EventManager.Subscribe<RemovePassive>(OnRemovePassive);
            EventManager.Subscribe<UpdatePassive>(OnUpdatePassive);
            EventManager.Subscribe<LoadSceneEvent>(OnLoadScene);
            EventManager.Subscribe<RemoveAllPassives>(OnRemoveAllPassives);
        }

        private void OnApplyEffect(ApplyEffect effect)
        {
            var typeName = effect.EffectType ?? string.Empty;
            if (!Enum.TryParse<AppliedPassiveType>(typeName, true, out var passiveType)) return;
			EventManager.Publish(new ApplyPassiveEvent { Delta = effect.Amount, Target = effect.Target, Type = passiveType });
        }

        protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
        {
            // No per-frame updates; event-driven
            return System.Array.Empty<Entity>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

        private void OnChangeBattlePhase(ChangeBattlePhaseEvent evt)
        {
            var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
            var enemy = EntityManager.GetEntitiesWithComponent<Enemy>().FirstOrDefault();
            if (evt == null) return;
            if (evt.Current == SubPhase.PlayerEnd)
            {
                RemoveTurnPassives(player);
            }
            else if (evt.Current == SubPhase.EnemyStart)
            {
                ApplyStartOfTurnPassives(enemy);
            }
            else if (evt.Current == SubPhase.PlayerStart)
            {
                ApplyStartOfTurnPassives(player);
            }
            else if (evt.Current == SubPhase.PreBlock)
            {
                ApplyStartOfPreBlockPassives(enemy);
            }
        }

        private void OnLoadScene(LoadSceneEvent @event)
        {
            // this is bad, make a direct event call to clean
            if (@event.Scene != SceneId.Battle) return;
            var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
            var ap = player.GetComponent<AppliedPassives>();
            if (ap == null) return;
            foreach (var passive in ap.Passives)
            {
                if (passive.Key == AppliedPassiveType.Penance) continue;
                EventManager.Publish(new RemovePassive { Owner = player, Type = passive.Key });
            }
        }

        private void ApplyStartOfTurnPassives(Entity owner)
        {
            var ap = owner.GetComponent<AppliedPassives>();
            if (ap == null) return;
            if (ap == null || ap.Passives == null || ap.Passives.Count == 0) return;
            var hasInferno = ap.Passives.TryGetValue(AppliedPassiveType.Inferno, out int infernoStacks);
            if (hasInferno && infernoStacks > 0)
            {
                EventQueueBridge.EnqueueTriggerAction("AppliedPassivesManagementSystem.ApplyStartOfTurnPassives.Inferno", () =>
                {
                    EventManager.Publish(new ApplyPassiveEvent { Target = owner, Type = AppliedPassiveType.Burn, Delta = infernoStacks });
                    EventManager.Publish(new PassiveTriggered { Owner = owner, Type = AppliedPassiveType.Inferno });
                }, .5f);
            }
            if ((ap.Passives.TryGetValue(AppliedPassiveType.Burn, out int burnStacks) || hasInferno) && (burnStacks > 0 || infernoStacks > 0))
            {
                EventQueueBridge.EnqueueTriggerAction("AppliedPassivesManagementSystem.ApplyStartOfTurnPassives.Burn", () =>
                {
                    EventManager.Publish(new ModifyHpRequestEvent { Source = owner, Target = owner, Delta = -(burnStacks + infernoStacks), DamageType = ModifyTypeEnum.Effect });
                    EventManager.Publish(new PassiveTriggered { Owner = owner, Type = AppliedPassiveType.Burn });
                }, .5f);
            }
            if (ap.Passives.TryGetValue(AppliedPassiveType.Webbing, out int webbingStacks) && webbingStacks > 0)
            {
                EventQueueBridge.EnqueueTriggerAction("AppliedPassivesManagementSystem.ApplyStartOfTurnPassives.Webbing", () =>
                {
                    EventManager.Publish(new ApplyPassiveEvent { Target = owner, Type = AppliedPassiveType.Slow, Delta = webbingStacks });
                    EventManager.Publish(new PassiveTriggered { Owner = owner, Type = AppliedPassiveType.Webbing });
                }, .5f);
            }
        }

        private void ApplyStartOfPreBlockPassives(Entity owner)
        {
            var ap = owner.GetComponent<AppliedPassives>();
            if (ap == null || ap.Passives == null || ap.Passives.Count == 0) return;

            if (ap.Passives.TryGetValue(AppliedPassiveType.Stun, out int stunStacks) && stunStacks > 0)
            {
                var intent = owner.GetComponent<AttackIntent>();
                if (intent == null || intent.Planned == null || intent.Planned.Count == 0) return;
                var count = stunStacks > intent.Planned.Count ? intent.Planned.Count : stunStacks;
                for (int i = 0; i < count; i++)
                {
                    EventQueueBridge.EnqueueTriggerAction("AppliedPassivesManagementSystem.ApplyStartOfTurnPassives.Stun", () =>
                    {
                        EventManager.Publish(new ShowStunnedOverlay { ContextId = owner.GetComponent<AttackIntent>()?.Planned?.FirstOrDefault()?.ContextId });
                        EventManager.Publish(new PassiveTriggered { Owner = owner, Type = AppliedPassiveType.Stun });
                        EventManager.Publish(new UpdatePassive { Owner = owner, Type = AppliedPassiveType.Stun, Delta = -1 });
                        var ctx = intent.Planned[0].ContextId;
                        intent.Planned.RemoveAt(0);
                        if (intent.Planned.Count == 0)
                        {
                            EventQueue.Clear();
                            EventQueue.EnqueueRule(new EventQueueBridge.QueuedPublish<ChangeBattlePhaseEvent>(
                                "Rule.ChangePhase.EnemyEnd",
                                new ChangeBattlePhaseEvent { Current = SubPhase.EnemyEnd }
                            ));
                            EventQueue.EnqueueRule(new EventQueueBridge.QueuedPublish<ChangeBattlePhaseEvent>(
                                "Rule.ChangePhase.PlayerStart",
                                new ChangeBattlePhaseEvent { Current = SubPhase.PlayerStart }
                            ));
                            EventQueue.EnqueueRule(new EventQueueBridge.QueuedPublish<ChangeBattlePhaseEvent>(
                                "Rule.ChangePhase.Action",
                                new ChangeBattlePhaseEvent { Current = SubPhase.Action }
                            ));
                        }
                    }, .4f);
                }
            }
        }

        private void OnApplyPassive(ApplyPassiveEvent e)
        {
            if (e == null || e.Target == null) return;
            var ap = e.Target.GetComponent<AppliedPassives>();
            if (ap == null)
            {
                // Create if missing on the target entity
                EntityManager.AddComponent(e.Target, new AppliedPassives());
                ap = e.Target.GetComponent<AppliedPassives>();
            }
            if (ap == null) return;

            ap.Passives.TryGetValue(e.Type, out int current);
            int next = current + e.Delta;
            if (next <= 0)
            {
                // remove from dictionary if zero or less
                if (ap.Passives.ContainsKey(e.Type)) ap.Passives.Remove(e.Type);
            }
            else
            {
                ap.Passives[e.Type] = next;
            }
        }

        private void OnRemovePassive(RemovePassive e)
        {
            if (e == null || e.Owner == null) return;
            var ap = e.Owner.GetComponent<AppliedPassives>();
            if (ap == null || ap.Passives == null) return;
            if (ap.Passives.ContainsKey(e.Type))
            {
                ap.Passives.Remove(e.Type);
            }
        }

        private void OnUpdatePassive(UpdatePassive e)
        {
            if (e == null || e.Owner == null) return;
            var ap = e.Owner.GetComponent<AppliedPassives>();
            if (ap == null || ap.Passives == null) return;
            ap.Passives[e.Type] = ap.Passives[e.Type] + e.Delta;
            if (ap.Passives[e.Type] <= 0)
            {
                ap.Passives.Remove(e.Type);
            }
        }

        private void OnRemoveAllPassives(RemoveAllPassives e)
        {
            if (e == null || e.Owner == null) return;
            var ap = e.Owner.GetComponent<AppliedPassives>();
            if (ap == null || ap.Passives == null) return;
            ap.Passives.Clear();
        }

        private void RemoveTurnPassives(Entity owner)
        {
            EventManager.Publish(new RemovePassive { Owner = owner, Type = AppliedPassiveType.DowseWithHolyWater });
        }
    }
}


