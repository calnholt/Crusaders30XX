using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class Sacrifice : CardBase
    {
        private int DrawAmount = 2;
        private int TemperanceAmount = 1;
        private int PenanceAmount = 1;
        public Sacrifice()
        {
            CardId = "sacrifice";
            Name = "Sacrifice";
            Target = "Player";
            Text = $"Draw {DrawAmount} cards, gain {TemperanceAmount} temperance, and gain {PenanceAmount} penance.";
            IsFreeAction = true;
            Animation = "Buff";
            Type = CardType.Prayer;
            Block = 2;

            OnPlay = (entityManager, card) =>
            {
                EventManager.Publish(new RequestDrawCardsEvent { Count = DrawAmount });
                EventManager.Publish(new ModifyTemperanceEvent { Delta = TemperanceAmount });
                EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Player"), Type = AppliedPassiveType.Penance, Delta = PenanceAmount });
            };
        }
    }
}

