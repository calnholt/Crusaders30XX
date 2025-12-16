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
            Text = "Choose a card from your hand - create an exact copy of the chosen card and put it into to your hand. Gain 1 temperance and exhaust this.";
            IsFreeAction = true;
            Animation = "Buff";
            Type = CardType.Prayer;
            Block = 3;
            SpecialAction = "SelectOneCardFromHand";

            OnPlay = (entityManager, card) =>
            {
                // Get payment cards with proper null checking
                var cacheEntity = entityManager.GetEntitiesWithComponent<LastPaymentCache>().FirstOrDefault();
                var paymentCards = cacheEntity?.GetComponent<LastPaymentCache>()?.PaymentCards;
                
                if (paymentCards != null && paymentCards.Count > 0)
                {
                    var paymentCard = paymentCards[0];
                    var copy = EntityFactory.CloneEntity(entityManager, paymentCard);
                    entityManager.AddComponent(copy, new MarkedForExhaust { Owner = copy });
                    EventManager.Publish(new CardMoveRequested { Card = copy, Deck = entityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault(), Destination = CardZoneType.Hand, Reason = "ShroudCopy" });
                }
                
                EventManager.Publish(new ModifyTemperanceEvent { Delta = 1 });
            };

            CanPlay = (entityManager, card) =>
            {
                var deckEntity = entityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
                var deck = deckEntity?.GetComponent<Deck>();
                if (deck == null) return false;
                var cardsInHand = deck.Hand.FindAll(c => {
                    if (c == card) return false; // Exclude the card being played
                    var cd = c.GetComponent<CardData>();
                    if (cd == null) return false;
                    return !cd.Card.IsWeapon;
                });
                if (cardsInHand.Count < 1)
                {
                    EventManager.Publish(new CantPlayCardMessage { Message = $"Requires at least one card in hand!" });
                    return false;
                }
                return true;  
            };
        }
    }
}

