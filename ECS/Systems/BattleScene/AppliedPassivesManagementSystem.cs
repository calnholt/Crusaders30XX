using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;

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
        }

        protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
        {
            // No per-frame updates; event-driven
            return System.Array.Empty<Entity>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

        private void OnChangeBattlePhase(ChangeBattlePhaseEvent evt)
        {
            if (evt == null) return;
            if (evt.Current == SubPhase.EnemyStart)
            {
                var enemy = EntityManager.GetEntitiesWithComponent<Enemy>().FirstOrDefault();
                if (enemy == null) return;
                ApplyStartOfTurnPassives(enemy);
            }
            else if (evt.Current == SubPhase.PlayerStart)
            {
                var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
                if (player == null) return;
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
                EventManager.Publish(new ModifyHpEvent { Target = owner, Delta = -burnStacks });
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

            int current = 0;
            ap.Passives.TryGetValue(e.Type, out current);
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
    }
}


