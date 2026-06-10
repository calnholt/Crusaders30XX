using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class RallyTheFaithful : CardBase
    {
        private int MightAmount = 1;

        public RallyTheFaithful()
        {
            CardId = "rally_the_faithful";
            Name = "Rally the Faithful";
            Target = "Player";
            Text = $"Gain {MightAmount} might.";
            IsFreeAction = true;
            Animation = "Buff";
            Type = CardType.Prayer;
            Block = 3;

            OnPlay = (entityManager, card) =>
            {
                var player = entityManager.GetEntity("Player");
                EventManager.Publish(new ApplyPassiveEvent
                {
                    Target = player,
                    Type = AppliedPassiveType.Might,
                    Delta = MightAmount
                });
            };
        }
    }
}
