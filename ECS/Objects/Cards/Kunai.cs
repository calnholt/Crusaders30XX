using System;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class Kunai : CardBase
    {
        private int RequiredAttackHits = 4;
        private int _attackDamageDealtCount;

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
                if (_attackDamageDealtCount >= RequiredAttackHits)
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

        public override void Initialize(EntityManager entityManager, Entity cardEntity)
        {
            base.Initialize(entityManager, cardEntity);
            EventManager.Subscribe<ModifyHpEvent>(OnModifyHp);
            EventManager.Subscribe<ChangeBattlePhaseEvent>(OnChangeBattlePhase);
        }

        private void OnModifyHp(ModifyHpEvent evt)
        {
            var phase = EntityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault()?.GetComponent<PhaseState>();
            if (phase?.Sub != SubPhase.Action) return;
            if (evt.DamageType != ModifyTypeEnum.Attack) return;
            if (evt.Delta >= 0 || Math.Abs(evt.Delta) == 0) return;

            var player = EntityManager.GetEntity("Player");
            var enemy = EntityManager.GetEntity("Enemy");
            if (evt.Source != player || evt.Target != enemy) return;

            _attackDamageDealtCount++;
        }

        private void OnChangeBattlePhase(ChangeBattlePhaseEvent evt)
        {
            if (evt.Current == SubPhase.Action || evt.Previous == SubPhase.Action)
            {
                _attackDamageDealtCount = 0;
            }
        }

        public override void Dispose()
        {
            EventManager.Unsubscribe<ModifyHpEvent>(OnModifyHp);
            EventManager.Unsubscribe<ChangeBattlePhaseEvent>(OnChangeBattlePhase);
            base.Dispose();
        }
    }
}
