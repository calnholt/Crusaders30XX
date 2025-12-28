using System;
using System.Collections.Generic;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Objects.Enemies;
using Crusaders30XX.ECS.Utils;
using static Crusaders30XX.ECS.Systems.MustBeBlockedSystem;

namespace Crusaders30XX.ECS.Objects.EnemyAttacks;

public class Spider : EnemyBase
{
  public Spider()
  {
    Id = "spider";
    Name = "Spider";
    MaxHealth = 80;
  }

  public override IEnumerable<string> GetAttackIds(EntityManager entityManager, int turnNumber)
  {
    var enders = new List<string> { "mandible_breaker", "rafterfall_ambush", "eight_limbs_of_death" };
    var linkers = new List<string> { "suffocating_silk", "fang_feint" };
    var ender = enders[Random.Shared.Next(0, enders.Count)];
    var linker = linkers[Random.Shared.Next(0, linkers.Count)];
    if (linker == "fang_feint")
    {
      return new List<string> { linker, ender };
    }
    return new List<string> { ender, linker };
  }
}

public class SuffocatingSilk : EnemyAttackBase
{
  public SuffocatingSilk()
  {
    Id = "suffocating_silk";
    Name = "Suffocating Silk";
    Damage = 2;
    ConditionType = ConditionType.OnHit;
    Text = EnemyAttackTextHelper.GetText(EnemyAttackTextType.Slow, 4, ConditionType);
    AmbushPercentage = 75;

    OnAttackHit = (entityManager) =>
    {
      EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Player"), Type = AppliedPassiveType.Slow, Delta = ValuesParse[0] });
    };
  }
}

public class MandibleBreaker : EnemyAttackBase
{
  public MandibleBreaker()
  {
    Id = "mandible_breaker";
    Name = "Mandible Breaker";
    Damage = 6;
    ConditionType = ConditionType.OnHit;
    Text = EnemyAttackTextHelper.GetText(EnemyAttackTextType.Fear, 2, ConditionType);
    AmbushPercentage = 75;
    OnAttackHit = (entityManager) =>
    {
      EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Player"), Type = AppliedPassiveType.Fear, Delta = ValuesParse[0] });
    };
  }
}

public class RafterfallAmbush : EnemyAttackBase
{
  public RafterfallAmbush()
  {
    Id = "rafterfall_ambush";
    Name = "Rafterfall Ambush";
    Damage = 8;
    ConditionType = ConditionType.MustBeBlockedByAtLeast1Card;
    Text = EnemyAttackTextHelper.GetText(EnemyAttackTextType.MustBeBlockedByAtLeast, 1);
    AmbushPercentage = 75;
    OnAttackReveal = (entityManager) =>
    {
      EventManager.Publish(new MustBeBlockedEvent { Threshold = ValuesParse[0], Type = MustBeBlockedByType.AtLeast });
    };
  }
}

public class EightLimbsOfDeath : EnemyAttackBase
{
  public EightLimbsOfDeath()
  {
    Id = "eight_limbs_of_death";
    Name = "Eight Limbs of Death";
    Damage = 8;
    ConditionType = ConditionType.OnBlockedByAtLeast1Card;
    Text = EnemyAttackTextHelper.GetText(EnemyAttackTextType.Fear, 2, ConditionType);
    AmbushPercentage = 75;
    OnAttackHit = (entityManager) =>
    {
      EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Player"), Type = AppliedPassiveType.Fear, Delta = ValuesParse[0] });
    };
  }
}

public class FangFeint : EnemyAttackBase
{
  public FangFeint()
  {
    Id = "fang_feint";
    Name = "Fang Feint";
    Damage = 1;
    ConditionType = ConditionType.OnHit;
    Text = EnemyAttackTextHelper.GetText(EnemyAttackTextType.Aggression, 4, ConditionType, 50);
    AmbushPercentage = 75;
    OnAttackHit = (entityManager) =>
    {
      if (Random.Shared.Next(0, 100) < ValuesParse[1])
      {
        EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Enemy"), Type = AppliedPassiveType.Aggression, Delta = ValuesParse[0] });
      }
    };
  }

}