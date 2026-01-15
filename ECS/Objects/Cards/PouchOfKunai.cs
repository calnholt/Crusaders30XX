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
        private int MinKunai = 2;
        private int MaxKunai = 4;
        public PouchOfKunai()
        {
            CardId = "pouch_of_kunai";
            Name = "Pouch of Kunai";
            Target = "Player";
            Text = $"Put {MinKunai} to {MaxKunai} Kunai cards in your hand.";
            Cost = ["White"];
            Animation = "Buff";
            Type = CardType.Prayer;
            Block = 3;

            OnPlay = (entityManager, card) =>
            {
                var count = Random.Shared.Next(MinKunai, MaxKunai + 1);
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

