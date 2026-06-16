using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class Smite : CardBase
    {
        private int TemperanceUpgradeAmount = 1;
        public Smite()
        {
            CardId = "smite";
            Rarity = Rarity.Starter;
            Name = "Smite";
            Target = "Enemy";
            Animation = "Attack";
            Damage = 3;
            Block = 3;

            OnPlay = (entityManager, card) =>
            {
                var player = entityManager.GetEntity("Player");
                var enemy = entityManager.GetEntity("Enemy");
                EventManager.Publish(new ModifyHpRequestEvent {
                    Source = player,
                    Target = enemy,
                    Delta = -GetDerivedDamage(entityManager, card),
                    AttackCard = card,

                    DamageType = ModifyTypeEnum.Attack
                });
                if (IsUpgraded)
                {
                    EventManager.Publish(new ModifyTemperanceEvent {
                        Delta = TemperanceUpgradeAmount
                    });
                }
            };

            OnPledged = (entityManager, card) =>
            {
                if (IsUpgraded) 
                {
                    EventManager.Publish(new ModifyTemperanceEvent {
                        Delta = TemperanceUpgradeAmount
                    });
                }
            };

            OnUpgrade = (entityManager, card) =>
            {
                Text = $"When this is pledged, gain {TemperanceUpgradeAmount} temperance.";
            };
        }
    }
}
