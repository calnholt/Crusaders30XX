using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;

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
    }
}
