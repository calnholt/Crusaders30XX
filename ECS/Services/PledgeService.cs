using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Objects.Cards;

namespace Crusaders30XX.ECS.Services
{
    public static class PledgeService
    {
        public static bool HasPledgedCardInHand(EntityManager entityManager)
        {
            var deck = entityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault()?.GetComponent<Deck>();
            if (deck?.Hand == null) return false;
            return deck.Hand.Any(card => card.GetComponent<Pledge>() != null);
        }

        public static Entity TryFindPriorTurnPledgedCardInHand(EntityManager entityManager)
        {
            var deck = entityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault()?.GetComponent<Deck>();
            if (deck?.Hand == null) return null;

            return deck.Hand.FirstOrDefault(card =>
            {
                var pledge = card.GetComponent<Pledge>();
                return pledge != null && pledge.CanPlay;
            });
        }

        public static void ApplyPledgeToHandCard(EntityManager entityManager, Entity card, bool markPledgedThisActionPhase = true)
        {
            if (card == null) return;
            if (card.GetComponent<Pledge>() != null) return;

            entityManager.AddComponent(card, new Pledge { Owner = card, CanPlay = false });
            EventManager.Publish(new PledgeAddedEvent { Card = card });
            var cardData = card.GetComponent<CardData>();
            cardData?.Card?.OnPledged?.Invoke(entityManager, card);

            if (markPledgedThisActionPhase)
            {
                PledgeAvailabilityService.SetPledgedThisActionPhase(entityManager, true);
            }
        }

        public static void RemovePledgeFromCard(EntityManager entityManager, Entity card)
        {
            if (card == null || card.GetComponent<Pledge>() == null) return;
            entityManager.RemoveComponent<Pledge>(card);
        }
    }
}
