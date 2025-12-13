using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class Stun : CardBase
    {
        public Stun()
        {
            CardId = "stun";
            Name = "Stun";
            Target = "Enemy";
            Text = "Stun the enemy.";
            Cost = ["Red"];
            IsFreeAction = true;
            Animation = "Attack";
            Type = "Spell";
            Block = 2;

            OnPlay = (entityManager, card) =>
            {
                var enemy = entityManager.GetEntity("Enemy");
                EventManager.Publish(new ApplyPassiveEvent { Target = enemy, Type = AppliedPassiveType.Stun, Delta = 1 });
            };
        }
    }
}

