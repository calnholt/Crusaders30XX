using System;
using System.Collections.Generic;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Objects.Enemies;
using Crusaders30XX.ECS.Utils;
using static Crusaders30XX.ECS.Systems.MustBeBlockedSystem;

namespace Crusaders30XX.ECS.Objects.EnemyAttacks;

public class Demon : EnemyBase
{
  public Demon()
  {
    Id = "demon";
    Name = "Demon";
    MaxHealth = 100;
  }

  public override IEnumerable<string> GetAttackIds(EntityManager entityManager, int turnNumber)
  {
    var combinations = new List<List<string>>
    {
      new List<string> { "razor_maw" },
      new List<string> { "scorching_claw" },
      new List<string> { "infernal_execution" },
    };
    int random = Random.Shared.Next(0, combinations.Count);
    return ArrayUtils.Shuffled(combinations[random]);
  }
}

public class RazorMaw : EnemyAttackBase
{
  public RazorMaw()
  {
    Id = "razor_maw";
    Name = "Razor Maw";
    Damage = 7;
    Text = EnemyAttackTextHelper.GetText(EnemyAttackTextType.Burn, 2);

    OnAttackHit = (entityManager) =>
    {
      EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Player"), Type = AppliedPassiveType.Burn, Delta = ValuesParse[0] });
    };
  }
}

public class ScorchingClaw : EnemyAttackBase
{
  public ScorchingClaw()
  {
    Id = "scorching_claw";
    Name = "Scorching Claw";
    Damage = 8;
    Text = EnemyAttackTextHelper.GetText(EnemyAttackTextType.Burn, 1);

    OnAttackHit = (entityManager) =>
    {
      EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Player"), Type = AppliedPassiveType.Burn, Delta = ValuesParse[0] });
    };
  }
}

public class InfernalExecution : EnemyAttackBase
{
  public InfernalExecution()
  {
    Id = "infernal_execution";
    Name = "Infernal Execution";
    Damage = 10;
    Text = EnemyAttackTextHelper.GetText(EnemyAttackTextType.MustBeBlockedByAtLeast, 1);

    OnAttackReveal = (entityManager) =>
    {
      EventManager.Publish(new MustBeBlockedEvent { Threshold = ValuesParse[0], Type = MustBeBlockedByType.AtLeast });
    };
  }
}
