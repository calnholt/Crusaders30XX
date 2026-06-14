using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Temperance
{
    public class IronResolve : TemperanceBase
    {
        public int VigorAmount = 1;
        public IronResolve()
        {
            Id = "iron_resolve";
            Name = "Iron Resolve";
            Target = "Player";
            Text = $"Gain {VigorAmount} vigor.";
            Threshold = 3;
        }

        public override void Activate(EntityManager entityManager)
        {
            PublishTrigger(entityManager);
            var player = GetPlayer(entityManager);
            EventManager.Publish(new ApplyPassiveEvent
            {
                Target = player,
                Type = AppliedPassiveType.Vigor,
                Delta = VigorAmount
            });
        }
    }
}
