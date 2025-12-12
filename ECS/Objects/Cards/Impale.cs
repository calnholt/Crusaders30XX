using System.Collections.Generic;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class Impale : CardBase
    {
        public Impale()
        {
            CardId = "impale";
            Name = "Impale";
            Target = "Enemy";
            Text = "As an additional cost, lose {2} courage.";
            IsFreeAction = true;
            Animation = "Attack";
            Type = "Attack";
            Damage = 15;
            Block = 3;

            OnPlay = (entityManager, card) =>
            {
                EventManager.Publish(new ModifyCourageEvent { Delta = -ValuesParse[0] });
                EventManager.Publish(new ModifyHpRequestEvent
                {
                    Source = entityManager.GetEntity("Player"),
                    Target = entityManager.GetEntity(Target),
                    Delta = -Damage,
                    DamageType = ModifyTypeEnum.Attack
                });
            };
        }
    }
}
