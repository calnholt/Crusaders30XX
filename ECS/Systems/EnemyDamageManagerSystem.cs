using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;

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
        }

        protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
        {
            return System.Array.Empty<Entity>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

        private void OnApplyEffect(ApplyEffect e)
        {
            if ((e.EffectType ?? string.Empty) != "Damage") return;
            int incoming = System.Math.Max(0, e.Amount);
            var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
            if (player == null) return;

            // 1) Consume assigned block for current processing context first
            var apcE = EntityManager.GetEntitiesWithComponent<AttackProcessingContext>().FirstOrDefault();
            var apc = apcE?.GetComponent<AttackProcessingContext>();
            if (apc != null && apc.RemainingAssignedBlock > 0 && incoming > 0)
            {
                int useAssigned = System.Math.Min(apc.RemainingAssignedBlock, incoming);
                apc.RemainingAssignedBlock -= useAssigned;
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
                EventManager.Publish(new ModifyHpEvent { Target = player, Delta = -incoming });
            }
        }
    }
}


