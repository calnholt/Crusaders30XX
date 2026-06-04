using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Temperance
{
    public class MeasuredBreath : TemperanceBase
    {
        public MeasuredBreath()
        {
            Id = "measured_breath";
            Name = "Measured Breath";
            Target = "Player";
            Text = "Draw 1 card.";
            Threshold = 3;
        }

        public override void Activate(EntityManager entityManager)
        {
            PublishTrigger(entityManager);
            EventManager.Publish(new RequestDrawCardsEvent { Count = 1 });
        }
    }
}
