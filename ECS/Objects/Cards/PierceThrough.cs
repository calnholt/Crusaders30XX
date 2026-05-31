using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class PierceThrough : CardBase
    {
        public PierceThrough()
        {
            CardId = "pierce_through";
            Name = "Pierce Through";
            Target = "Enemy";
            Text = "Remove 1 guard from the enemy.";
            Animation = "Attack";
            Damage = 10;
            Cost = ["Black", "Red"];
            IsFreeAction = false;

            OnPlay = (entityManager, card) =>
            {
                var player = entityManager.GetEntity("Player");
                var enemy = entityManager.GetEntity(Target);

                EventManager.Publish(new RemoveGuardEvent
                {
                    Enemy = enemy,
                    Value = 1
                });

                EventManager.Publish(new ModifyHpRequestEvent
                {
                    Source = player,
                    Target = enemy,
                    Delta = -GetDerivedDamage(entityManager, card),
                    AttackCard = card,

                    DamageType = ModifyTypeEnum.Attack
                });
            };
        }
    }
}