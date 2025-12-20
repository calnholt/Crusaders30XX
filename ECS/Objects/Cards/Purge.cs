using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Systems;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class Purge : CardBase
    {
        public Purge()
        {
            CardId = "purge";
            Name = "Purge";
            Target = "Player";
            Cost = ["Any","Any","Any"];
            Text = "Each attack card discarded to play this gains +{2} damage for the rest of the quest.";
            Animation = "Attack";
            Block = 3;
            Damage = 35;

            OnPlay = (entityManager, card) =>
            {
                EventManager.Publish(new ModifyHpRequestEvent { 
                    Source = entityManager.GetEntity("Player"), 
                    Target = entityManager.GetEntity("Enemy"), 
                    Delta = -GetDerivedDamage(entityManager, card), 
                    DamageType = ModifyTypeEnum.Attack 
                });
                
                // Get payment cards with proper null checking
                var cacheEntity = entityManager.GetEntitiesWithComponent<LastPaymentCache>().FirstOrDefault();
                var paymentCards = cacheEntity?.GetComponent<LastPaymentCache>()?.PaymentCards;
                
                if (paymentCards != null && paymentCards.Count > 0)
                {
                    foreach (var paymentCard in paymentCards)
                    {
                        if (paymentCard.GetComponent<CardData>().Card.Type != CardType.Attack) continue;
                        AttackDamageValueService.ApplyDelta(paymentCard, +ValuesParse[0], "Purge");
                    }
                }
            };
        }
    }
}