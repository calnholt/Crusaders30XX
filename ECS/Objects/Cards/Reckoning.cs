using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class Reckoning : CardBase
    {
        public Reckoning()
        {
            CardId = "reckoning";
            Name = "Reckoning";
            Target = "Enemy";
            Cost = ["Any", "Any"];
            Animation = "Attack";
            Damage = 10;
            Block = 2;

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
            };
        }
    }
}
