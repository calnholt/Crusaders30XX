using System.Collections.Generic;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class Courageous : CardBase
    {
        public Courageous()
        {
            CardId = "courageous";
            Name = "Courageous";
            Target = "Player";
            Text = "Gain {3} courage. End your turn.";
            IsFreeAction = true;
            Type = "Spell";
            Block = 2;

            OnPlay = (entityManager, card) =>
            {
                EventManager.Publish(new ModifyCourageEvent { Delta = ValuesParse[0] });
                EventManager.Publish(new DebugCommandEvent { Command = "EndTurn" });
            };
        }
    }
}