using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class UnburdenedStrike : CardBase
    {
        private int DamageBonus = 3;

        public UnburdenedStrike()
        {
            CardId = "unburdened_strike";
            Name = "Unburdened Strike";
            Target = "Enemy";
            Text = $"If no cards were discarded to play this, this gains +{DamageBonus} damage.";
            Cost = ["White", "Any"];
            Animation = "Attack";
            Damage = 8;
            Block = 2;

            OnPlay = (entityManager, card) =>
            {
                var bonusDamage = 0;
                var cacheEntity = entityManager.GetEntitiesWithComponent<LastPaymentCache>().FirstOrDefault();
                var paymentCards = cacheEntity?.GetComponent<LastPaymentCache>()?.PaymentCards;
                if (paymentCards == null || paymentCards.Count == 0)
                {
                    bonusDamage = DamageBonus;
                }

                EventManager.Publish(new ModifyHpRequestEvent
                {
                    Source = entityManager.GetEntity("Player"),
                    Target = entityManager.GetEntity("Enemy"),
                    Delta = -GetDerivedDamage(entityManager, card) - bonusDamage,
                    AttackCard = card,
                    DamageType = ModifyTypeEnum.Attack
                });
            };

        }
    }
}
