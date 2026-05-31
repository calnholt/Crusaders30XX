using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class ZealousVow : CardBase
    {
        private int SharpenAmount = 2;
        private int AggressionOnPledge = 2;

        public ZealousVow()
        {
            CardId = "zealous_vow";
            Name = "Zealous Vow";
            Target = "Player";
            Text = $"When this is pledged, gain {AggressionOnPledge} aggression. Sharpen {SharpenAmount}.";
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
                    Type = AppliedPassiveType.Sharpen,
                    Delta = SharpenAmount
                });
            };

            OnPledged = (entityManager, card) =>
            {
                var player = entityManager.GetEntity("Player");
                EventManager.Publish(new ApplyPassiveEvent
                {
                    Target = player,
                    Type = AppliedPassiveType.Aggression,
                    Delta = AggressionOnPledge
                });
            };
        }
    }
}
