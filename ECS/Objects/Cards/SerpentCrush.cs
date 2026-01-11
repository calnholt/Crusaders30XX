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
            Text = "As an additional cost, lose {4} courage. Gain {1} action point and draw {1} card.";
            IsFreeAction = true;
            Animation = "Attack";
            Damage = 12;
            Type = CardType.Attack;
            Block = 3;

            OnPlay = (entityManager, card) =>
            {
                EventManager.Publish(new ModifyHpRequestEvent { 
                    Source = entityManager.GetEntity("Player"), 
                    Target = entityManager.GetEntity("Enemy"), 
                    Delta = -GetDerivedDamage(entityManager, card), 
                    DamageType = ModifyTypeEnum.Attack 
                });
                EventManager.Publish(new ModifyCourageRequestEvent { Delta = -ValuesParse[0] });
                EventManager.Publish(new ModifyActionPointsEvent { Delta = ValuesParse[1] });
                EventManager.Publish(new RequestDrawCardsEvent { Count = ValuesParse[2] });
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
