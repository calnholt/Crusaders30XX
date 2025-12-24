using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class SerpentCrush : CardBase
    {
        public SerpentCrush()
        {
            CardId = "serpent_crush";
            Name = "Serpent Crush";
            Target = "Enemy";
            Cost = ["Any"];
            Text = "As an additional cost, lose {4} courage. Gain {1} action point.";
            IsFreeAction = true;
            Animation = "Attack";
            Damage = 25;
            Type = CardType.Attack;
            Block = 3;

            OnPlay = (entityManager, card) =>
            {
                var player = entityManager.GetEntity("Player");
                var enemy = entityManager.GetEntity("Enemy");
                EventManager.Publish(new ModifyCourageEvent { Delta = -ValuesParse[0] });
                EventManager.Publish(new ModifyActionPointsEvent { Delta = ValuesParse[1] });
                EventManager.Publish(new ModifyHpRequestEvent { 
                    Source = player, 
                    Target = enemy, 
                    Delta = -GetDerivedDamage(entityManager, card), 
                    DamageType = ModifyTypeEnum.Attack 
                });
            };

            CanPlay = (entityManager, card) =>
            {
                var player = entityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
                var courageCmp = player?.GetComponent<Courage>();
                int courage = courageCmp?.Amount ?? 0;
                if (courage < ValuesParse[0])
                {
                    EventManager.Publish(new CantPlayCardMessage { Message = $"Requires {ValuesParse[0]} courage!" });
                    return false;
                }
                return true;
            };
        }

        
    }
}
