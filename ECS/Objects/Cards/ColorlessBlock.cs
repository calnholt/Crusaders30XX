namespace Crusaders30XX.ECS.Objects.Cards
{
    public class ColorlessBlock : CardBase
    {
        public ColorlessBlock()
        {
            CardId = "colorless_3_block";
            Rarity = Rarity.Common;
            Name = "Protect";
            Block = 3;
            Animation = "Block";
            Type = CardType.Block;
            CanAddToLoadout = false;
        }
    }
}
