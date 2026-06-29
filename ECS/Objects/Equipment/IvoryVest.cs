using Crusaders30XX.ECS.Components;

namespace Crusaders30XX.ECS.Objects.Equipment
{
    public class IvoryVest : EquipmentBase
    {
        public IvoryVest()
        {
            Id = "ivory_vest";
            Name = "Ivory Vest";
            Slot = EquipmentSlot.Chest;
            Block = 1;
            Uses = 2;
            Color = CardData.CardColor.White;
            CanActivate = () => false;
        }
    }
}
