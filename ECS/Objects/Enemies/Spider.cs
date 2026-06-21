using System;
using System.Collections.Generic;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Objects.Enemies;
using Crusaders30XX.ECS.Systems;
using Crusaders30XX.ECS.Utils;
using static Crusaders30XX.ECS.Systems.MustBeBlockedSystem;

namespace Crusaders30XX.ECS.Objects.EnemyAttacks;

public class Spider : EnemyBase
{
  private int FearAmount = 2;
  public Spider(EnemyDifficulty difficulty = EnemyDifficulty.Easy) : base(difficulty)
  {
    Id = "spider";
    Name = "Spider";
    HealthPerCard = 1.4f;

    OnStartOfBattle = (entityManager) =>
    {
      EventQueueBridge.EnqueueTriggerAction("Spider.OnStartOfBattle", () => {  
        EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Player"), Type = AppliedPassiveType.Fear, Delta = FearAmount });
      }, AppliedPassivesManagementSystem.Duration);
    };
  }

  public override IEnumerable<string> GetAttackIds(EntityManager entityManager, int turnNumber)
  {
    var random = Random.Shared.Next(0, 100);
    if (random <= 65)
    {
      return ["suffocating_silk"];
    }
    return ["mandible_breaker"];
  }
}

public class SuffocatingSilk : EnemyAttackBase
{
  private int SlowAmount = 4;
  public SuffocatingSilk()
  {
    Id = "suffocating_silk";
    Name = "Suffocating Silk";
    Damage = 10;
    BlockRequiredToPreventEffect = Random.Shared.Next(0, 100) <= 50 ? 6 : 7;
    Text = $"{EnemyAttackTextHelper.GetBlockThresholdText(Damage - BlockRequiredToPreventEffect.Value, EnemyAttackTextHelper.GetText(EnemyAttackTextType.Slow, SlowAmount, ConditionType))}";

    OnDamageThresholdMet = (entityManager) =>
    {
      EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Player"), Type = AppliedPassiveType.Slow, Delta = SlowAmount });
    };
  }
}

public class MandibleBreaker : EnemyAttackBase
{
  private int FearAmount = 1;
  public MandibleBreaker()
  {
    Id = "mandible_breaker";
    Name = "Mandible Breaker";
    Damage = 10;
    ConditionType = ConditionType.OnBlockedByAtLeast1Card;
    Text = $"On attack - Intimidate 1 card.\n\n{EnemyAttackTextHelper.GetText(EnemyAttackTextType.Fear, FearAmount, ConditionType)}";

    OnAttackReveal = (entityManager) =>
    {
      EventManager.Publish(new IntimidateEvent { Amount = 1 });
    };

    OnAttackHit = (entityManager) =>
    {
      EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Player"), Type = AppliedPassiveType.Fear, Delta = FearAmount });
    };
  }
}
