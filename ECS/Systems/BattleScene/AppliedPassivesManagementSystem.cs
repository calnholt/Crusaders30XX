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

        }

        private void OnApplyEffect(ApplyEffect effect)
        {
            var typeName = effect.EffectType ?? string.Empty;
            if (!Enum.TryParse<AppliedPassiveType>(typeName, true, out var passiveType)) return;
            OnApplyPassive(new ApplyPassiveEvent { Delta = effect.Amount, Owner = effect.Target, Type = passiveType });
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
        }

        private void ApplyStartOfTurnPassives(Entity owner)
        {
            var ap = owner.GetComponent<AppliedPassives>();
            if (ap == null || ap.Passives == null || ap.Passives.Count == 0) return;

            // Burn: deal 1 damage to the owner per stack
            if (ap.Passives.TryGetValue(AppliedPassiveType.Burn, out int burnStacks) && burnStacks > 0)
            {
                EventQueueBridge.EnqueueTriggerAction("AppliedPassivesManagementSystem.ApplyStartOfTurnPassives.Burn", () =>
                {
                    EventManager.Publish(new ModifyHpEvent { Source = owner, Target = owner, Delta = -burnStacks, DamageType = ModifyTypeEnum.Effect });
                    EventManager.Publish(new PassiveTriggered { Owner = owner, Type = AppliedPassiveType.Burn });
                }, .15f);
            }
        }

        private void OnApplyPassive(ApplyPassiveEvent e)
        {
            if (e == null || e.Owner == null) return;
            var ap = e.Owner.GetComponent<AppliedPassives>();
            if (ap == null)
            {
                // Create if missing on the target entity
                EntityManager.AddComponent(e.Owner, new AppliedPassives());
                ap = e.Owner.GetComponent<AppliedPassives>();
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

        private void RemoveTurnPassives(Entity owner)
        {
            EventManager.Publish(new RemovePassive { Owner = owner, Type = AppliedPassiveType.DowseWithHolyWater });
        }
    }
}


