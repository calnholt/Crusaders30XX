using System;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Medals
{
    public class StMichael : MedalBase
    {
        public StMichael()
        {
            Id = "st_michael";
            Name = "St. Michael the Archangel";
            Text = "At the start of battle, gain 1 courage.";
        }

        public override void Initialize(EntityManager entityManager, Entity medalEntity)
        {
            EventManager.Subscribe<ChangeBattlePhaseEvent>(OnChangeBattlePhaseEvent);
            EntityManager = entityManager;
            MedalEntity = medalEntity;
        }
        public override void Activate()
        {
            EventManager.Publish(new ModifyCourageRequestEvent { Delta = 1, Reason = Id, Type = ModifyCourageType.Gain });
        }

        private void OnChangeBattlePhaseEvent(ChangeBattlePhaseEvent evt)
        {
            if (evt.Current == SubPhase.StartBattle)
            {
                EmitActivateEvent();
            }
        }

        public override void Dispose()
        {
            EventManager.Unsubscribe<ChangeBattlePhaseEvent>(OnChangeBattlePhaseEvent);
            Console.WriteLine($"[StMichael] Unsubscribed from ChangeBattlePhaseEvent");
        }

    }
}