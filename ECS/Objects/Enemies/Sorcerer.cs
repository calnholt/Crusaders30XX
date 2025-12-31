using System;
using System.Collections.Generic;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Objects.EnemyAttacks;
using Crusaders30XX.ECS.Utils;

namespace Crusaders30XX.ECS.Objects.Enemies;

public class Sorcerer : EnemyBase
{
  public Sorcerer()
  {
    Id = "sorcerer";
    Name = "Sorcerer";
    MaxHealth = 90;

    OnCreate = (entityManager) =>
    {
      EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Player"), Type = AppliedPassiveType.Intimidated, Delta = 1 });
      EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Player"), Type = AppliedPassiveType.MindFog, Delta = 1 });
    };
  }

  public override IEnumerable<string> GetAttackIds(EntityManager entityManager, int turnNumber)
  {
    return ArrayUtils.TakeRandomWithReplacement(new List<string> { "strange_force" }, 1);
  }

  public override void Dispose()
  {
    Console.WriteLine($"[Sorcerer] Dispose");
  }
}

public class StrangeForce : EnemyAttackBase
{
  public StrangeForce()
  {
    Id = "strange_force";
    Name = "Strange Force";
    Damage = 10;
    Text = "Draw [1] card.";

    OnAttackReveal = (entityManager) =>
    {
      EventManager.Publish(new RequestDrawCardsEvent { Count = ValuesParse[0] });
    };
  }
}
