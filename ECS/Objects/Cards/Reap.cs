using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Systems;
using static Crusaders30XX.ECS.Components.CardData;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class Reap : CardBase
    {
        private int DamageBonus = 4;
        public Reap()
        {
            CardId = "reap";
            Name = "Reap";
            Target = "Player";
            Cost = ["Any","Any"];
            Text = $"This attack gains +{DamageBonus} damage if two red cards were discarded to play this.";
            Animation = "Attack";
            Block = 3;
            Damage = 8;

            OnPlay = (entityManager, card) =>
            {
                EventManager.Publish(new ModifyHpRequestEvent { 
                  Source = entityManager.GetEntity("Player"), 
                  Target = entityManager.GetEntity("Enemy"), 
                  Delta = -GetDerivedDamage(entityManager, card), 
                  DamageType = ModifyTypeEnum.Attack 
                });
            };

            GetConditionalDamage = (entityManager, card) =>
            {
                var cacheEntity = entityManager.GetEntitiesWithComponent<LastPaymentCache>().FirstOrDefault();
                var paymentCards = cacheEntity?.GetComponent<LastPaymentCache>()?.PaymentCards;
                var redCards = 0;
                if (paymentCards != null && paymentCards.Count > 0)
                {
                    foreach (var paymentCard in paymentCards)
                    {
                        if (paymentCard.GetComponent<CardData>().Color == CardColor.Red) redCards++;
                    }
                }
                
                return redCards == 2 ? DamageBonus : 0;
            };
        }
    }
}