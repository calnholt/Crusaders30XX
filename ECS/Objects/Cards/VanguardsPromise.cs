using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Services;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class VanguardsPromise : CardBase
    {
        public VanguardsPromise()
        {
            CardId = "vanguards_promise";
            Name = "Vanguard's Promise";
            Target = "Enemy";
            Text = "If you have no pledged card, pledge the top card of your deck.";
            Animation = "Attack";
            Damage = 2;
            Block = 2;
            IsFreeAction = true;

            OnPlay = (entityManager, card) =>
            {
                var player = entityManager.GetEntity("Player");
                var enemy = entityManager.GetEntity("Enemy");
                EventManager.Publish(new ModifyHpRequestEvent
                {
                    Source = player,
                    Target = enemy,
                    Delta = -GetDerivedDamage(entityManager, card),
                    AttackCard = card,
                    DamageType = ModifyTypeEnum.Attack
                });

                if (PledgeService.HasPledgedCardInHand(entityManager)) return;

                var deckEntity = entityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
                var deck = deckEntity?.GetComponent<Deck>();
                if (deck == null || deck.DrawPile.Count == 0) return;

                var topCard = deck.DrawPile[0];
                EventManager.Publish(new CardMoveRequested
                {
                    Card = topCard,
                    Deck = deckEntity,
                    Destination = CardZoneType.Hand,
                    Reason = "VanguardsPromise"
                });
                PledgeService.ApplyPledgeToHandCard(entityManager, topCard);
            };
        }
    }
}
