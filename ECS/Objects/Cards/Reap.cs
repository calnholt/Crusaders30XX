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
        private int DamageBonus = 5;
        public Reap()
        {
            CardId = "reap";
            Name = "Reap";
            Target = "Player";
            Cost = ["Any","Any"];
            Text = $"This attack gains +{DamageBonus} damage for each red card discarded to play this.";
            Animation = "Attack";
            Block = 3;
            Damage = 25;

            OnPlay = (entityManager, card) =>
            {
                // Get payment cards with proper null checking
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

                EventManager.Publish(new ModifyHpRequestEvent { 
                  Source = entityManager.GetEntity("Player"), 
                  Target = entityManager.GetEntity("Enemy"), 
                  Delta = -(Damage + (redCards > 0 ? redCards * DamageBonus : 0)), 
                  DamageType = ModifyTypeEnum.Attack 
                });
            };

            GetConditionalDamage = (entityManager, card) =>
            {
                var selectedForPayment = entityManager.GetEntitiesWithComponent<SelectedForPayment>();
                int redCards = 0;
                
                foreach (var entity in selectedForPayment)
                {
                    var cardData = entity.GetComponent<CardData>();
                    if (cardData != null && cardData.Color == CardColor.Red)
                    {
                        redCards++;
                    }
                }
                
                return redCards * DamageBonus;
            };
        }
    }
}