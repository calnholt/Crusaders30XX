using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Systems;

namespace Crusaders30XX.ECS.Objects.Equipment
{
  public class PiercedHeartPlate : EquipmentBase
  {
    private readonly int Cost = 1;
    private readonly int Courage = 2;
    public PiercedHeartPlate()
    {
      Id = "pierced_heart_plate";
      Name = "Pierced Heart Plate";
      Slot = EquipmentSlot.Chest;
      Block = 4;
      Uses = 1;
      Color = CardData.CardColor.Black;
      Text = $"Gain {Courage} courage. Lose {Cost} use. Free action.";
    }

    public override void Activate()
    {
      EventManager.Publish(new ModifyCourageRequestEvent { Delta = Courage });
      EventQueue.EnqueueRule(new QueuedStartBuffAnimation(true));
      EventQueue.EnqueueRule(new QueuedWaitBuffComplete(true));
      RemainingUses--;
    }
  }
}