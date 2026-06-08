using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Factories;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class HiddenKunai : CardBase
    {
        public HiddenKunai()
        {
            CardId = "hidden_kunai";
            Name = "Hidden Kunai";
            Text = "Add a kunai to your hand.";
            Block = 3;
            Animation = "Block";
            Type = CardType.Block;

            OnBlock = (entityManager, card) =>
            {
                var deckEntity = entityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
                var kunai = EntityFactory.CreateCardFromDefinition(entityManager, "kunai", CardData.CardColor.White, false, 1);
                EventManager.Publish(new CardMoveRequested { Card = kunai, Deck = deckEntity, Destination = CardZoneType.Hand, Reason = "HiddenKunai" });
            };
        }
    }
}
