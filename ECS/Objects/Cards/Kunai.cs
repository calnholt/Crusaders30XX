using System;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class Kunai : CardBase
    {
        public Kunai()
        {
            CardId = "kunai";
            Name = "Kunai";
            Target = "Enemy";
            Text = "{25}% chance to wound the enemy. Exhaust on play or at the end of your turn";
            IsFreeAction = true;
            Animation = "Attack";
            Type = "Attack";
            Damage = 4;
            ExhaustsOnPlay = true;
            ExhaustsOnEndTurn = true;
            CanAddToLoadout = false;
            IsToken = true;
            CardTooltip = "kunai";

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
                    EventManager.Publish(new ApplyPassiveEvent { Target = enemy, Type = AppliedPassiveType.Wounded, Delta = +1 });
                }
            };
        }
    }
}

