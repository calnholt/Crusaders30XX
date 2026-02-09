using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class GlacialMaul : CardBase
    {
        private int DamageBonus = 2;
        private int ActionPointGain = 1;
        public GlacialMaul()
        {
            CardId = "glacial_maul";
            Name = "Glacial Maul";
            Target = "Enemy";
            Text = $"Frozen. When a frozen card is discarded to pay for this card, this attack gains +{DamageBonus} damage and gain {ActionPointGain}AP.";
            Cost = ["Any"];
            Animation = "Attack";
            Damage = 3;
            IsWeapon = true;

            OnCreate = (entityManager, card) =>
            {
                card.AddComponent(new Frozen());
            };

            OnPlay = (entityManager, card) =>
            {
                var player = entityManager.GetEntity("Player");
                var enemy = entityManager.GetEntity("Enemy");

                var cacheEntity = entityManager.GetEntitiesWithComponent<LastPaymentCache>().FirstOrDefault();
                var paymentCards = cacheEntity?.GetComponent<LastPaymentCache>()?.PaymentCards;
                int frozenCount = 0;
                if (paymentCards != null)
                {
                    foreach (var paymentCard in paymentCards)
                    {
                        if (paymentCard.GetComponent<Frozen>() != null) frozenCount++;
                    }
                }

                if (frozenCount > 0)
                {
                    EventManager.Publish(new ModifyActionPointsEvent { Delta = ActionPointGain * frozenCount });
                }

                EventManager.Publish(new ModifyHpRequestEvent
                {
                    Source = player,
                    Target = enemy,
                    Delta = -GetDerivedDamage(entityManager, card),
                    DamageType = ModifyTypeEnum.Attack
                });
            };

            GetConditionalDamage = (entityManager, card) =>
            {
                var cacheEntity = entityManager.GetEntitiesWithComponent<LastPaymentCache>().FirstOrDefault();
                var paymentCards = cacheEntity?.GetComponent<LastPaymentCache>()?.PaymentCards;
                int frozenCount = 0;
                if (paymentCards != null)
                {
                    foreach (var paymentCard in paymentCards)
                    {
                        if (paymentCard.GetComponent<Frozen>() != null) frozenCount++;
                    }
                }
                return DamageBonus * frozenCount;
            };
        }
    }
}
