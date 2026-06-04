using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Temperance
{
    public abstract class TemperanceBase
    {
        public string Id { get; protected set; } = string.Empty;
        public string Name { get; protected set; } = string.Empty;
        public string Target { get; protected set; } = "Player";
        public string Text { get; protected set; } = string.Empty;
        public int Threshold { get; protected set; } = 1;

        public virtual void Activate(EntityManager entityManager) { }

        protected Entity GetPlayer(EntityManager entityManager)
        {
            return entityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
        }

        protected Entity GetEnemy(EntityManager entityManager)
        {
            return entityManager.GetEntitiesWithComponent<Enemy>().FirstOrDefault();
        }

        protected void PublishTrigger(EntityManager entityManager)
        {
            var player = GetPlayer(entityManager);
            if (player == null) return;
            EventManager.Publish(new TriggerTemperance { Owner = player, AbilityId = Id });
        }
    }
}
