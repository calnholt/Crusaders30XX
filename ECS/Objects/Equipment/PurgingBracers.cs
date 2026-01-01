using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Systems;

namespace Crusaders30XX.ECS.Objects.Equipment
{
  public class PurgingBracers : EquipmentBase
  {
    private readonly int Cost = 1;
    private readonly int Aggression = 8;
    public PurgingBracers()
    {
      Id = "purging_bracers";
      Name = "Purging Bracers";
      Slot = EquipmentSlot.Arms;
      Block = 2;
      Uses = 1;

      Color = CardData.CardColor.White;
      Text = $"Gain {Aggression} aggression. Lose {Cost} use. Free action.";
    }

    public override void Activate()
    {
      EventManager.Publish(new ApplyPassiveEvent { Target = EntityManager.GetEntity("Player"), Type = AppliedPassiveType.Aggression, Delta = 8 });
      EventQueue.EnqueueRule(new QueuedStartBuffAnimation(true));
      EventQueue.EnqueueRule(new QueuedWaitBuffComplete(true));
      RemainingUses--;
    }
  }
}