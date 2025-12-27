using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Objects.EnemyAttacks;

namespace Crusaders30XX.ECS.Objects.Enemies;

public class HaveNoMercy : EnemyAttackBase
{
  public HaveNoMercy()
  {
    Id = "have_no_mercy";
    Name = "Have No Mercy";
    Damage = 7;

    OnAttackReveal = (entityManager) =>
    {
      EventManager.Publish(new MarkedForSpecificDiscardEvent { Amount = 1 });
    };
  }
}