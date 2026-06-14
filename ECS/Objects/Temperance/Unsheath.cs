using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Temperance
{
    public class Unsheath : TemperanceBase
    {
        public int SharpenAmount = 5;
        public Unsheath()
        {
            Id = "unsheath";
            Name = "Unsheath";
            Target = "Player";
            Text = $"Gain {SharpenAmount} sharpen.";
            Threshold = 3;
        }

        public override void Activate(EntityManager entityManager)
        {
            PublishTrigger(entityManager);
            var player = GetPlayer(entityManager);
            EventManager.Publish(new ApplyPassiveEvent { Target = player, Type = AppliedPassiveType.Sharpen, Delta = SharpenAmount });
        }
    }
}
