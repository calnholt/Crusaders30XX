using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Components;
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
