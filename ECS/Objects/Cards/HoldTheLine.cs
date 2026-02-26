using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class HoldTheLine : CardBase
    {
        public int Courage = 1;
        public HoldTheLine()
        {
            CardId = "hold_the_line";
            Name = "Hold the Line";
            Text = $"Gain {Courage} courage.";
            Block = 3;
            Animation = "Block";
            Type = CardType.Block;

            OnBlock = (entityManager, card) =>
            {
                EventManager.Publish(new ModifyCourageRequestEvent { Delta = +Courage });
            };
        }
    }
}