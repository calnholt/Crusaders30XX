using System.Collections.Generic;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class Courageous : CardBase
    {
        private int CourageBonus = 3;
        public Courageous()
        {
            CardId = "courageous";
            Name = "Courageous";
            Target = "Player";
            Text = $"Gain {CourageBonus} courage. End your turn.";
            IsFreeAction = true;
            Type = CardType.Prayer;
            Block = 2;

            OnPlay = (entityManager, card) =>
            {
                EventManager.Publish(new ModifyCourageRequestEvent { Delta = CourageBonus, Type = ModifyCourageType.Gain });
                TimerScheduler.Schedule(0.1f, () => {
                    EventManager.Publish(new DebugCommandEvent { Command = "EndTurn" });
                });
            };
        }
    }
}