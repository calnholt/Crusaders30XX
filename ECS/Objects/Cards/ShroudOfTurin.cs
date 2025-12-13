using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Factories;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class ShroudOfTurin : CardBase
    {
        public ShroudOfTurin()
        {
            CardId = "shroud_of_turin";
            Name = "Shroud of Turin";
            Target = "Player";
            Text = "Choose a card from your hand - create an exact copy of the chosen card and put it into to your hand. Gain 1 temperance.";
            IsFreeAction = true;
            Animation = "Buff";
            Type = "Spell";
            Block = 3;
            SpecialAction = "SelectOneCardFromHand";

            OnPlay = (entityManager, card) =>
            {
                var paymentCards = entityManager.GetEntitiesWithComponent<LastPaymentCache>().FirstOrDefault()?.GetComponent<LastPaymentCache>().PaymentCards.ToList();
                var paymentCard = paymentCards[0];
                var copy = EntityFactory.CreateCardFromDefinition(entityManager, paymentCard.GetComponent<CardData>().Card.CardId, paymentCard.GetComponent<CardData>().Color, false);
                EventManager.Publish(new CardMoveRequested { Card = copy, Deck = entityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault(), Destination = CardZoneType.Hand, Reason = "ShroudCopy" });
                EventManager.Publish(new ModifyTemperanceEvent { Delta = 1 });
            };

            CanPlay = (entityManager, card) =>
            {
                var deckEntity = entityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
                var deck = deckEntity?.GetComponent<Deck>();
                if (deck == null) return false;
                var cardsInHand = deck.Hand.FindAll(c => {
                    var cd = c.GetComponent<CardData>();
                    if (cd == null) return false;
                    var cardObj = CardFactory.Create(cd.Card.CardId);
                    return !cardObj.IsWeapon && cd.Card.CardId != cardObj.CardId;
                });
                if (cardsInHand.Count == 0)
                {
                    EventManager.Publish(new CantPlayCardMessage { Message = $"Requires at least one card in hand!" });
                    return false;
                }
                return true;  
            };
        }
    }
}

