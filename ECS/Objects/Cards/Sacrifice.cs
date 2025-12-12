using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class Sacrifice : CardBase
    {
        public Sacrifice()
        {
            CardId = "sacrifice";
            Name = "Sacrifice";
            Target = "Player";
            Text = "Draw {2} cards, gain {1} temperance, and gain {1} penance.";
            IsFreeAction = true;
            Animation = "Buff";
            Type = "Spell";
            Block = 2;

            OnPlay = (entityManager, card) =>
            {
                EventManager.Publish(new RequestDrawCardsEvent { Count = ValuesParse[0] });
                EventManager.Publish(new ModifyTemperanceEvent { Delta = ValuesParse[1] });
                EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Player"), Type = AppliedPassiveType.Penance, Delta = ValuesParse[2] });
            };
        }
    }
}

