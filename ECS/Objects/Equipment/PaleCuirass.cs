using Crusaders30XX.ECS.Components;

namespace Crusaders30XX.ECS.Objects.Equipment
{
    public class PaleCuirass : EquipmentBase
    {
        public PaleCuirass()
        {
            Id = "pale_cuirass";
            Name = "Pale Cuirass";
            Slot = EquipmentSlot.Chest;
            Block = 2;
            Uses = 1;
            Color = CardData.CardColor.White;
            CanActivate = () => false;
        }
    }
}
