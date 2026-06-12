using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class Seize : CardBase
    {
        private int DamageBonus = 2;
        private bool _courageLostThisPhase;

        public Seize()
        {
            CardId = "seize";
            Name = "Seize";
            Target = "Enemy";
            Damage = 2;
            Block = 3;
            Text = $"If you have lost courage during this action phase, this gains +{DamageBonus} damage.";
            Animation = "Attack";

            OnPlay = (entityManager, card) =>
            {
                var damage = GetDerivedDamage(entityManager, card);
                EventManager.Publish(new ModifyHpRequestEvent { 
                    Source = entityManager.GetEntity("Player"), 
                    Target = entityManager.GetEntity("Enemy"), 
                    Delta = -damage, 
                    AttackCard = card,
 
                    DamageType = ModifyTypeEnum.Attack 
                });
            };

            GetConditionalDamage = (entityManager, card) =>
                _courageLostThisPhase ? DamageBonus : 0;
        }

        public override void Initialize(EntityManager entityManager, Entity cardEntity)
        {
            base.Initialize(entityManager, cardEntity);
            EventManager.Subscribe<ModifyCourageRequestEvent>(OnModifyCourage);
            EventManager.Subscribe<SetCourageEvent>(OnSetCourage);
            EventManager.Subscribe<ChangeBattlePhaseEvent>(OnChangeBattlePhase);
        }

        private bool IsActionPhase()
        {
            var phase = EntityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault()?.GetComponent<PhaseState>();
            return phase?.Sub == SubPhase.Action;
        }

        private void OnModifyCourage(ModifyCourageRequestEvent evt)
        {
            if (!IsActionPhase()) return;
            if (evt.Delta < 0)
                _courageLostThisPhase = true;
        }

        private void OnSetCourage(SetCourageEvent evt)
        {
            if (!IsActionPhase()) return;

            var player = EntityManager.GetEntity("Player");
            var courage = player?.GetComponent<Courage>();
            if (courage == null) return;

            if (evt.Amount < courage.Amount)
                _courageLostThisPhase = true;
        }

        private void OnChangeBattlePhase(ChangeBattlePhaseEvent evt)
        {
            if (evt.Current == SubPhase.Action || evt.Previous == SubPhase.Action)
                _courageLostThisPhase = false;
        }

        public override void Dispose()
        {
            EventManager.Unsubscribe<ModifyCourageRequestEvent>(OnModifyCourage);
            EventManager.Unsubscribe<SetCourageEvent>(OnSetCourage);
            EventManager.Unsubscribe<ChangeBattlePhaseEvent>(OnChangeBattlePhase);
            base.Dispose();
        }
    }
}
