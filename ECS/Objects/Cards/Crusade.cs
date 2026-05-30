using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class Crusade : CardBase
    {
        private int ActionPointGain = 1;
        private int AggressionGain = 3;
        public Crusade()
        {
            CardId = "crusade";
            Name = "Crusade";
            Target = "Enemy";
            Text = $"If this card is pledged when played, gain {ActionPointGain}AP and {AggressionGain} aggression.";
            Cost = ["Black"];
            Animation = "Attack";
            Damage = 6;
            Block = 2;

            OnPlay = (entityManager, card) =>
            {
                var player = entityManager.GetEntity("Player");
                var enemy = entityManager.GetEntity("Enemy");

                var isPledged = card.GetComponent<Pledge>() != null;

                EventManager.Publish(new ModifyHpRequestEvent
                {
                    Source = player,
                    Target = enemy,
                    Delta = -GetDerivedDamage(entityManager, card),
                    DamageType = ModifyTypeEnum.Attack
                });

                if (isPledged)
                {
                    EventManager.Publish(new ModifyActionPointsEvent { Delta = ActionPointGain });
                    EventManager.Publish(new ApplyPassiveEvent { Target = player, Type = AppliedPassiveType.Aggression, Delta = AggressionGain });
                }
            };
        }
    }
}
