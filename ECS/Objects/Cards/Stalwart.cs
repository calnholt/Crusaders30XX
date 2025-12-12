using Crusaders30XX.ECS.Core;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class Stalwart : CardBase
    {
        public Stalwart()
        {
            CardId = "stalwart";
            Name = "Stalwart";
            Text = "As an additional cost when using this card to block, lose {2} courage.";
            Type = "Block";
            Block = 7;
            // Note: The courage cost is handled in BlockCardResolveService when the card is used to block
        }
    }
}

