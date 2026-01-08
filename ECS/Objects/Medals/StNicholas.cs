using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Medals
{
    public class StNicholas : MedalBase
    {
        public bool Activated { get; set; } = false;
        public int HpIncrease { get; set; } = 2;
        public int FrozenCards { get; set; } = 8;
        public StNicholas()
        {
            Id = "st_nicholas";
            Name = "St. Nicholas the Bishop";
            Text = $"At the start of the quest, increase your max HP by {HpIncrease} and {FrozenCards} random cards from you deck become frozen.";
        }

        public override void Initialize(EntityManager entityManager, Entity medalEntity)
        {
            EventManager.Subscribe<ChangeBattlePhaseEvent>(OnChangeBattlePhaseEvent);
            EntityManager = entityManager;
            MedalEntity = medalEntity;
        }

        public override void Activate()
        {
            EventManager.Publish(new IncreaseMaxHpEvent { Target = EntityManager.GetEntity("Player"), Delta = HpIncrease });
            EventManager.Publish(new FreezeCardsEvent { Amount = FrozenCards, Type = FreezeType.HandAndDrawPile });
        }

        private void OnChangeBattlePhaseEvent(ChangeBattlePhaseEvent evt)
        {
            if (evt.Current == SubPhase.StartBattle && !Activated)
            {
                EmitActivateEvent();
                Activated = true;
            }
        }
    }
}