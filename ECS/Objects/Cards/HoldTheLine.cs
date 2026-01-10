using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class HoldTheLine : CardBase
    {
        public int Draw = 1;
        public HoldTheLine()
        {
            CardId = "hold_the_line";
            Name = "Hold the Line";
            Text = $"Draw {Draw} card{(Draw > 1 ? "s" : "")}.";
            Block = 2;
            Animation = "Block";
            Type = CardType.Block;

            OnBlock = (entityManager, card) =>
            {
                EventManager.Publish(new RequestDrawCardsEvent { Count = Draw });
            };
        }
    }
}