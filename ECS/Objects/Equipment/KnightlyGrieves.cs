using Crusaders30XX.ECS.Components;

namespace Crusaders30XX.ECS.Objects.Equipment
{
    public class KnightlyGrieves : EquipmentBase
    {
        public KnightlyGrieves()
        {
            Id = "knightly_grieves";
            Name = "Knightly Grieves";
            Slot = EquipmentSlot.Legs;
            Block = 2;
            Uses = 2;
            Color = CardData.CardColor.Black;
            CanActivate = () => false;
        }
    }
}