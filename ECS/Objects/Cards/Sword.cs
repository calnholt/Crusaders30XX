using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class Sword : CardBase
    {
        public Sword()
        {
            CardId = "sword";
            Name = "Sword";
            Target = "Enemy";
            Text = "Gain {1} courage.";
            Cost = ["Black", "Any"];
            Animation = "Attack";
            Type = "Attack";
            Damage = 17;
            IsWeapon = true;

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
                EventManager.Publish(new ModifyCourageEvent { Delta = +ValuesParse[0] });
            };
        }
    }
}

