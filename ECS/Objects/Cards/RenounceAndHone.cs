using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Services;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class RenounceAndHone : CardBase
    {
        private int SharpenAmount = 6;

        public RenounceAndHone()
        {
            CardId = "renounce_and_hone";
            Name = "Renounce and Hone";
            Target = "Player";
            Text = "As an additional cost, discard your pledged card that was not pledged this turn. Gain sharpen 6.";
            Animation = "Buff";
            Type = CardType.Prayer;
            Block = 3;
            IsFreeAction = true;

            OnPlay = (entityManager, card) =>
            {
                var pledgedCard = PledgeService.TryFindPriorTurnPledgedCardInHand(entityManager);
                if (pledgedCard == null) return;

                var deckEntity = entityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
                PledgeService.RemovePledgeFromCard(entityManager, pledgedCard);
                EventManager.Publish(new CardMoveRequested
                {
                    Card = pledgedCard,
                    Deck = deckEntity,
                    Destination = CardZoneType.DiscardPile,
                    Reason = "RenounceAndHone"
                });

                var player = entityManager.GetEntity("Player");
                EventManager.Publish(new ApplyPassiveEvent
                {
                    Target = player,
                    Type = AppliedPassiveType.Sharpen,
                    Delta = SharpenAmount
                });
            };

            CanPlay = (entityManager, card) =>
            {
                return PledgeService.TryFindPriorTurnPledgedCardInHand(entityManager) != null;
            };

            OnCantPlay = (entityManager, card) =>
            {
                if (PledgeService.TryFindPriorTurnPledgedCardInHand(entityManager) == null)
                {
                    EventManager.Publish(new CantPlayCardMessage { Message = "Requires a pledged card from a prior turn!" });
                }
            };
        }
    }
}
