using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class Ravage : CardBase
    {
        public Ravage()
        {
            CardId = "ravage";
            Name = "Ravage";
            Target = "Enemy";
            Text = "As an additional cost, mill {4} cards.";
            Animation = "Attack";
            Type = "Attack";
            Damage = 25;
            Block = 3;

            OnPlay = (entityManager, card) =>
            {
                var player = entityManager.GetEntity("Player");
                var enemy = entityManager.GetEntity("Enemy");
                for (int j = 0; j < ValuesParse[0]; j++)
                {
                    EventManager.Publish(new MillCardEvent { });
                }
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

