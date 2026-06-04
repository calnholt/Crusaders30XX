using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Temperance
{
    public class AngelicAura : TemperanceBase
    {
        public AngelicAura()
        {
            Id = "angelic_aura";
            Name = "Angelic Aura";
            Target = "Player";
            Text = "Gain 4 aegis.";
            Threshold = 3;
        }

        public override void Activate(EntityManager entityManager)
        {
            PublishTrigger(entityManager);
            var player = GetPlayer(entityManager);
            EventManager.Publish(new ApplyPassiveEvent { Target = player, Type = AppliedPassiveType.Aegis, Delta = 4 });
        }
    }
}
