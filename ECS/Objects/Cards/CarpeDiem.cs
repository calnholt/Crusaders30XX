using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class CarpeDiem : CardBase
    {
        private const int CourageGain = 4;

        public CarpeDiem()
        {
            CardId = "carpe_diem";
            Name = "Carpe Diem";
            Target = "Player";
            Text = $"Gain {CourageGain} courage. At the end of the turn, lose all courage.";
            IsFreeAction = true;
            Type = CardType.Prayer;
            Block = 2;
            Animation = "Buff";

            OnPlay = (entityManager, card) =>
            {
                EventManager.Publish(new ModifyCourageRequestEvent { Delta = CourageGain, Type = ModifyCourageType.Gain });
                var player = entityManager.GetEntity("Player");
                EventManager.Publish(new ApplyPassiveEvent
                {
                    Target = player,
                    Type = AppliedPassiveType.CarpeDiem,
                    Delta = 1
                });
            };
        }
    }
}
