using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Factories;
using Crusaders30XX.ECS.Services;

namespace Crusaders30XX.ECS.Objects.Temperance
{
    public class FlingFling : TemperanceBase
    {
        public FlingFling()
        {
            Id = "fling_fling";
            Name = "Fling Fling";
            Target = "Player";
            Text = "Add 2 Kunai cards to your hand.";
            Threshold = 3;
        }

        public override void Activate(EntityManager entityManager)
        {
            PublishTrigger(entityManager);
            var deckEntity = entityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
            for (int i = 0; i < 2; i++)
            {
                var kunai = EntityFactory.CreateCardFromDefinition(entityManager, "kunai", CardData.CardColor.White, false, i + 1);
                EventManager.Publish(new CardMoveRequested { Card = kunai, Deck = deckEntity, Destination = CardZoneType.Hand, Reason = "FlingFling" });
            }
        }
    }
}
