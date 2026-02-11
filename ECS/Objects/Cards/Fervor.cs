using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class Fervor : CardBase
    {
        public Fervor()
        {
            CardId = "fervor";
            Name = "Fervor";
            Target = "Enemy";
            Cost = ["Red"];
            Animation = "Attack";
            Damage = 7;
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
            };
        }
    }
}
