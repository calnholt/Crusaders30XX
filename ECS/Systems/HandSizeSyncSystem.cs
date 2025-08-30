using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// Keeps Deck.MaxHandSize aligned with the player's MaxHandSize component value.
    /// </summary>
    public class HandSizeSyncSystem : Core.System
    {
        public HandSizeSyncSystem(EntityManager entityManager) : base(entityManager) { }

        protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<Deck>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
            var deck = entity.GetComponent<Deck>();
            if (deck == null) return;

            var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
            var mhs = player?.GetComponent<MaxHandSize>()?.Value;
            if (mhs.HasValue && mhs.Value > 0 && deck.MaxHandSize != mhs.Value)
            {
                deck.MaxHandSize = mhs.Value;
            }
        }
    }
}


