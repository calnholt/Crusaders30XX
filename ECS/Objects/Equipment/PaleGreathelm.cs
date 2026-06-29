using Crusaders30XX.ECS.Components;

namespace Crusaders30XX.ECS.Objects.Equipment
{
    public class PaleGreathelm : EquipmentBase
    {
        public PaleGreathelm()
        {
            Id = "pale_greathelm";
            Name = "Pale Greathelm";
            Slot = EquipmentSlot.Head;
            Block = 2;
            Uses = 1;
            Color = CardData.CardColor.White;
            CanActivate = () => false;
        }
    }
}
