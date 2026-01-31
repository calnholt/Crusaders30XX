using System.Collections.Generic;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Systems;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class Bulwark : CardBase
    {
        private int BlockBonus = 1;
        public Bulwark()
        {
            CardId = "bulwark";
            Name = "Bulwark";
            Target = "Enemy";
            Text = $"This card gains +{BlockBonus} block for the duration of the quest.";
            Block = 3;
            Damage = 1;
            IsFreeAction = true;
            Animation = "Attack";

            OnPlay = (entityManager, card) =>
            {
                EventManager.Publish(new ModifyHpRequestEvent { 
                    Source = entityManager.GetEntity("Player"), 
                    Target = entityManager.GetEntity("Enemy"), 
                    Delta = -Damage, 
                    DamageType = ModifyTypeEnum.Attack 
                });
                BlockValueService.ApplyDelta(card, BlockBonus, "Bulwark");
            };
        }
    }
}