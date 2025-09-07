using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// Centralizes updates to EquipmentUsedState in response to EquipmentUseResolved events.
    /// </summary>
    public class EquipmentUsedManagementSystem : Core.System
    {
        public EquipmentUsedManagementSystem(EntityManager entityManager) : base(entityManager)
        {
            EventManager.Subscribe<EquipmentUseResolved>(OnEquipmentUseResolved);
        }

        protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
        {
            return System.Array.Empty<Entity>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

        private void OnEquipmentUseResolved(EquipmentUseResolved evt)
        {
            if (evt == null || string.IsNullOrEmpty(evt.EquipmentId)) return;
            var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
            if (player == null) return;
            var state = player.GetComponent<EquipmentUsedState>();
            if (state == null)
            {
                state = new EquipmentUsedState();
                EntityManager.AddComponent(player, state);
            }
            int current = 0;
            state.UsesByEquipmentId.TryGetValue(evt.EquipmentId, out current);
            long next = (long)current + evt.Delta;
            state.UsesByEquipmentId[evt.EquipmentId] = next < 0 ? 0 : (int)next;
        }
    }
}


