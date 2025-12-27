using System;
using System.Collections.Generic;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Objects.Enemies;
using Crusaders30XX.ECS.Objects.EnemyAttacks;
using static Crusaders30XX.ECS.Systems.MustBeBlockedSystem;

namespace Crusaders30XX.ECS.Objects.EnemyAttacks;

public class SandGolem : EnemyBase
{
  public SandGolem()
  {
    Id = "sand_golem";
    Name = "Sand Golem";
    MaxHealth = 70;
  }
  public override IEnumerable<string> GetAttackIds(EntityManager entityManager, int turnNumber)
  {
    return turnNumber % 2 == 0 ? ["sand_pound"] : ["sand_slam"];
  }
}

public class SandPound : EnemyAttackBase
{
  public SandPound()
  {
    Id = "sand_pound";
    Name = "Sand Pound";
    Damage = 7;
    ConditionType = ConditionType.OnBlockedByExactly1Card;
    Text = EnemyAttackTextHelper.GetText(EnemyAttackTextType.MustBeBlockedExactly, 1);

    OnAttackReveal = (entityManager) =>
    {
      EventManager.Publish(new MustBeBlockedEvent { Threshold = ValuesParse[0], Type = MustBeBlockedByType.Exactly });
    };
  }
}

public class SandSlam : EnemyAttackBase
{
  public SandSlam()
  {
    Id = "sand_slam";
    Name = "Sand Slam";
    Damage = 10;
    ConditionType = ConditionType.OnBlockedByExactly2Cards;
    Text = EnemyAttackTextHelper.GetText(EnemyAttackTextType.MustBeBlockedExactly, 2);

    OnAttackReveal = (entityManager) =>
    {
      EventManager.Publish(new MustBeBlockedEvent { Threshold = ValuesParse[0], Type = MustBeBlockedByType.Exactly });
    };
  }
}