using System;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Factories;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class PouchOfKunai : CardBase
    {
        public PouchOfKunai()
        {
            CardId = "pouch_of_kunai";
            Name = "Pouch of Kunai";
            Target = "Player";
            Text = "Put {2} to {4} Kunai cards in your hand.";
            Cost = ["White"];
            Animation = "Buff";
            Type = "Spell";
            Block = 3;

            OnPlay = (entityManager, card) =>
            {
                var count = Random.Shared.Next(ValuesParse[0], ValuesParse[1] + 1);
                var deckEntity = entityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
                for (int j = 0; j < count; j++)
                {
                    var kunai = EntityFactory.CreateCardFromDefinition(entityManager, "kunai", CardData.CardColor.White, false, j + 1);
                    EventManager.Publish(new CardMoveRequested { Card = kunai, Deck = deckEntity, Destination = CardZoneType.Hand, Reason = "PouchOfKunai" });
                }
            };
        }
    }
}

