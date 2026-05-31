using System;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Medals
{
    public class StFrancisDeSales : MedalBase
    {
        public StFrancisDeSales()
        {
            Id = "st_francis_de_sales";
            Name = "St. Francis de Sales";
            Text = "At the start of battle, gain 1 temperance.";
        }

        public override void Initialize(EntityManager entityManager, Entity medalEntity)
        {
            EventManager.Subscribe<ChangeBattlePhaseEvent>(OnChangeBattlePhaseEvent);
            EntityManager = entityManager;
            MedalEntity = medalEntity;
        }

        private void OnChangeBattlePhaseEvent(ChangeBattlePhaseEvent evt)
        {
            if (evt.Current == SubPhase.StartBattle)
            {
                EmitActivateEvent();
            }
        }

        public override void Activate()
        {
            EventManager.Publish(new ModifyTemperanceEvent { Delta = 1 });
        }

        public override void Dispose()
        {
            EventManager.Unsubscribe<ChangeBattlePhaseEvent>(OnChangeBattlePhaseEvent);
        }
    }
}
