using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Temperance
{
    public class Radiance : TemperanceBase
    {
        public Radiance()
        {
            Id = "radiance";
            Name = "Radiance";
            Target = "Enemy";
            Text = "Stun the enemy.";
            Threshold = 4;
        }

        public override void Activate(EntityManager entityManager)
        {
            PublishTrigger(entityManager);
            var enemy = GetEnemy(entityManager);
            EventManager.Publish(new ApplyPassiveEvent { Target = enemy, Type = AppliedPassiveType.Stun, Delta = 1 });
        }
    }
}
