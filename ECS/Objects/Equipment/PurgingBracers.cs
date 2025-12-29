using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Equipment
{
  public class PurgingBracers : EquipmentBase
  {
    private int Cost = 1;
    private int Aggression = 8;
    public PurgingBracers()
    {
      Id = "purging_bracers";
      Name = "Purging Bracers";
      Slot = EquipmentSlot.Arms;
      Block = 3;
      Uses = 1;

      Color = CardData.CardColor.White;
      Text = $"Gain {Aggression} aggression. Lose {Cost} use. Free action.";
    }

    public override void Activate()
    {
      EventManager.Publish(new ApplyPassiveEvent { Target = EntityManager.GetEntity("Player"), Type = AppliedPassiveType.Aggression, Delta = 8 });
    }
  }
}