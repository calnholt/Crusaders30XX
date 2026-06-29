using Crusaders30XX.ECS.Components;

namespace Crusaders30XX.ECS.Objects.Equipment
{
    public class ScarletTreads : EquipmentBase
    {
        public ScarletTreads()
        {
            Id = "scarlet_treads";
            Name = "Scarlet Treads";
            Slot = EquipmentSlot.Legs;
            Block = 1;
            Uses = 2;
            Color = CardData.CardColor.Red;
            CanActivate = () => false;
        }
    }
}
