using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class Dagger : CardBase
    {
        public Dagger()
        {
            CardId = "dagger";
            Name = "Dagger";
            Target = "Enemy";
            Text = "As an additional cost, lose {2} courage.";
            IsFreeAction = true;
            Animation = "Attack";
            Type = "Attack";
            Damage = 10;

            OnPlay = (entityManager, card) =>
            {
                var player = entityManager.GetEntity("Player");
                var enemy = entityManager.GetEntity("Enemy");
                EventManager.Publish(new ModifyCourageEvent { Delta = -ValuesParse[0] });
                EventManager.Publish(new ModifyHpRequestEvent { 
                    Source = player, 
                    Target = enemy, 
                    Delta = -Damage, 
                    DamageType = ModifyTypeEnum.Attack 
                });
            };
        }
    }
}

