using Crusaders30XX.ECS.Components;

namespace Crusaders30XX.ECS.Objects.Equipment
{
    public class PaleGreaves : EquipmentBase
    {
        public PaleGreaves()
        {
            Id = "pale_greaves";
            Name = "Pale Greaves";
            Slot = EquipmentSlot.Legs;
            Block = 2;
            Uses = 1;
            Color = CardData.CardColor.White;
            CanActivate = () => false;
        }
    }
}
