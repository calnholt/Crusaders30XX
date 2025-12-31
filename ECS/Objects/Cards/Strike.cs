using System;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class Strike : CardBase
    {
        public Strike()
        {
            CardId = "strike";
            Name = "Strike";
            Target = "Enemy";
            Text = "{50}% chance to gain {2} courage.";
            Animation = "Attack";
            Damage = 12;
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
                var chance = ValuesParse[0];
                var random = Random.Shared.Next(0, 100);
                if (random <= chance)
                {
                    EventManager.Publish(new ModifyCourageRequestEvent { Delta = ValuesParse[1] });
                }
            };
        }
    }
}

