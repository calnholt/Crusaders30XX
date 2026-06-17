using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class QuickWit : CardBase
    {
        private int CourageCost = 1;
        public QuickWit()
        {
            CardId = "quick_wit";
            Name = "Quick Wit";
            Target = "Enemy";
            Text = $"As an additional cost, lose {CourageCost} courage. Draw 1 random card from your discard pile.";
            Animation = "Attack";
            Damage = 2;
            Block = 3;
            IsFreeAction = true;

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
                EventManager.Publish(new DrawRandomCardFromDiscardEvent { Amount = 1 });
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