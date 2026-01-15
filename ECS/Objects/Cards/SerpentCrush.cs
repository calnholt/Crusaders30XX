using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class SerpentCrush : CardBase
    {
        private int CourageCost = 4;
        private int ActionPointAmount = 1;
        private int DrawAmount = 1;
        public SerpentCrush()
        {
            CardId = "serpent_crush";
            Name = "Serpent Crush";
            Target = "Enemy";
            Text = $"As an additional cost, lose {CourageCost} courage. Gain {ActionPointAmount} action point and draw {DrawAmount} card.";
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
                EventManager.Publish(new ModifyCourageRequestEvent { Delta = -CourageCost, Type = ModifyCourageType.Spent });
                EventManager.Publish(new ModifyActionPointsEvent { Delta = ActionPointAmount });
                EventManager.Publish(new RequestDrawCardsEvent { Count = DrawAmount });
            };

            CanPlay = (entityManager, card) =>
            {
                var player = entityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
                var courageCmp = player?.GetComponent<Courage>();
                int courage = courageCmp?.Amount ?? 0;
                if (courage < CourageCost)
                {
                    EventManager.Publish(new CantPlayCardMessage { Message = $"Requires {CourageCost} courage!" });
                    return false;
                }
                return true;
            };
        }

        
    }
}
