using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class Kunai : CardBase
    {
        private int RequiredAttackHits = 4;

        public Kunai()
        {
            CardId = "kunai";
            Name = "Kunai";
            Target = "Enemy";
            Text = $"Wounds the enemy if you have dealt attack damage {RequiredAttackHits} times this action phase. Exhaust on play or at the end of your turn";
            IsFreeAction = true;
            Animation = "Attack";
            Damage = 1;
            ExhaustsOnEndTurn = true;
            CanAddToLoadout = false;
            IsToken = true;
            CardTooltip = "kunai";

            OnPlay = (entityManager, card) =>
            {
                var player = entityManager.GetEntity("Player");
                var enemy = entityManager.GetEntity("Enemy");
                var battleState = player?.GetComponent<BattleStateInfo>();
                if (battleState != null && battleState.PlayerActionPhaseAttackHits >= RequiredAttackHits)
                {
                    EventManager.Publish(new ApplyPassiveEvent { Target = enemy, Type = AppliedPassiveType.Wounded, Delta = +1 });
                }
                EventManager.Publish(new ModifyHpRequestEvent
                {
                    Source = player,
                    Target = enemy,
                    Delta = -GetDerivedDamage(entityManager, card),
                    AttackCard = card,
                    DamageType = ModifyTypeEnum.Attack
                });
                entityManager.AddComponent(card, new MarkedForExhaust { Owner = card });
            };
        }
    }
}
