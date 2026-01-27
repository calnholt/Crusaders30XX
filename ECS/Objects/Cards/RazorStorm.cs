using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards;

public class RazorStorm : CardBase
{
  private int NumOfHits = 3;
  public RazorStorm()
  {
    CardId = "razor_storm";
    Name = "Razor Storm";
    Target = "Enemy";
    Text = $"Attacks {NumOfHits} times.";
    Animation = "Attack";
    Damage = 2;
    Block = 2;
    IsFreeAction = true;

    OnPlay = (entityManager, card) =>
    {
      var time = 0.5f;
      var numOfHits = NumOfHits;
      EventManager.Publish(new EndTurnDisplayEvent { ShowButton = false });
      TimerScheduler.Schedule(time * numOfHits, () =>
      {
        EventManager.Publish(new EndTurnDisplayEvent { ShowButton = true });
      });
      for (int j = 0; j < numOfHits; j++)
      {
        TimerScheduler.Schedule(time + (j * time), () =>
        {
          EventManager.Publish(new ModifyHpRequestEvent
          {
            Source = entityManager.GetEntity("Player"),
            Target = entityManager.GetEntity("Enemy"),
            Delta = -GetDerivedDamage(entityManager, card),
            DamageType = ModifyTypeEnum.Attack
          });
        });
      }
    };
  }
}