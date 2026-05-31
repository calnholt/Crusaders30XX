using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class Exaltation : CardBase
    {
        private int CourageCost = 3;
        public Exaltation()
        {
            CardId = "exaltation";
            Name = "Exaltation";
            Target = "Enemy";
            Text = $"As an additional cost, lose {CourageCost} courage.";
            Animation = "Attack";
            Damage = 6;
            Block = 3;

            OnPlay = (entityManager, card) =>
            {
                var player = entityManager.GetEntity("Player");
                var enemy = entityManager.GetEntity("Enemy");
                EventManager.Publish(new ModifyCourageRequestEvent { Delta = -CourageCost, Type = ModifyCourageType.Spent });
                EventManager.Publish(new ModifyHpRequestEvent
                {
                    Source = player,
                    Target = enemy,
                    Delta = -GetDerivedDamage(entityManager, card),
                    AttackCard = card,

                    DamageType = ModifyTypeEnum.Attack
                });
            };

            CanPlay = (entityManager, card) =>
            {
                var player = entityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
                var courageCmp = player?.GetComponent<Courage>();
                int courage = courageCmp?.Amount ?? 0;
                return courage >= CourageCost;
            };
            OnCantPlay = (entityManager, card) =>
            {
                var player = entityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
                var courageCmp = player?.GetComponent<Courage>();
                int courage = courageCmp?.Amount ?? 0;
                if (courage < CourageCost)
                    EventManager.Publish(new CantPlayCardMessage { Message = $"Requires {CourageCost} courage!" });
            };
        }
    }
}
