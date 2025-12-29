using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Systems;

namespace Crusaders30XX.ECS.Objects.Equipment
{
  public class HelmOfSeeing : EquipmentBase
  {
    private readonly int Cost = 1;
    private readonly int Cards = 1;
    public HelmOfSeeing()
    {
      Id = "helm_of_seeing";
      Name = "Helm of Seeing";
      Slot = EquipmentSlot.Head;
      Block = 4;
      Uses = 1;
      Color = CardData.CardColor.Red;
      Text = $"Draw {Cards} card. Lose {Cost} use. Free action.";
    }

    public override void Activate()
    {
      EventManager.Publish(new RequestDrawCardsEvent { Count = Cards });
      EventQueue.EnqueueRule(new QueuedStartBuffAnimation(true));
      EventQueue.EnqueueRule(new QueuedWaitBuffComplete(true));
      RemainingUses--;
    }
  }
}