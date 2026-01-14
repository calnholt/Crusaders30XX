using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class Stun : CardBase
    {
        private int CourageCost = 1;
        public Stun()
        {
            CardId = "stun";
            Name = "Stun";
            Target = "Enemy";
            Text = $"As an additional cost, lose {CourageCost} courage. Stun the enemy.";
            Cost = ["Red"];
            Animation = "Attack";
            Type = CardType.Prayer;
            IsFreeAction = true;
            Block = 2;

            OnPlay = (entityManager, card) =>
            {
                EventManager.Publish(new ModifyCourageRequestEvent { Delta = -CourageCost, Type = ModifyCourageType.Spent });
                var enemy = entityManager.GetEntity("Enemy");
                EventManager.Publish(new ApplyPassiveEvent { Target = enemy, Type = AppliedPassiveType.Stun, Delta = 1 });
            };

            CanPlay = (entityManager, card) =>
            {
                var enemy = entityManager.GetEntity("Enemy");
                var courage = entityManager.GetEntitiesWithComponent<Courage>().FirstOrDefault();
                var courageCmp = courage?.GetComponent<Courage>();
                if (courageCmp?.Amount < CourageCost)
                {
                    EventManager.Publish(new CantPlayCardMessage { Message = $"Requires {CourageCost} courage!" });
                    return false;
                }
                return true;
            };
        }
    }
}

