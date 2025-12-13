using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class Tempest : CardBase
    {
        public Tempest()
        {
            CardId = "tempest";
            Name = "Tempest";
            Target = "Enemy";
            Text = "Gain {3} temperance.";
            Animation = "Attack";
            Cost = ["White"];
            Type = "Attack";
            Damage = 18;
            Block = 3;

            OnPlay = (entityManager, card) =>
            {
                var player = entityManager.GetEntity("Player");
                var enemy = entityManager.GetEntity("Enemy");
                EventManager.Publish(new ModifyHpRequestEvent { 
                    Source = player, 
                    Target = enemy, 
                    Delta = -GetDerivedDamage(entityManager, card), 
                    DamageType = ModifyTypeEnum.Attack 
                });
                EventManager.Publish(new ModifyTemperanceEvent { Delta = ValuesParse[0] });
            };
        }
    }
}

