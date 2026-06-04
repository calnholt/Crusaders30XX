using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class Tempest : CardBase
    {
        private int TemperanceAmount = 4;
        public Tempest()
        {
            CardId = "tempest";
            Name = "Tempest";
            Target = "Enemy";
            Text = $"Gain {TemperanceAmount} temperance.";
            Animation = "Attack";
            Cost = ["White", "Any"];
            Damage = 7;
            Block = 2;

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
                EventManager.Publish(new ModifyTemperanceEvent { Delta = TemperanceAmount });
            };
        }
    }
}

