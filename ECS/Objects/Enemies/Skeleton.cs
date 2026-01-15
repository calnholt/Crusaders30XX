using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Objects.Enemies;
using Crusaders30XX.ECS.Services;
using Crusaders30XX.ECS.Systems;
using Crusaders30XX.ECS.Utils;

namespace Crusaders30XX.ECS.Objects.EnemyAttacks;

public class Skeleton : EnemyBase
{
  private int Armor = 3;

  public Skeleton(EnemyDifficulty difficulty = EnemyDifficulty.Easy) : base(difficulty)
  {
    Id = "skeleton";
    Name = "Skeleton";
    MaxHealth = 55 + (int)difficulty * 7;
    Armor += (int)difficulty;

    OnStartOfBattle = (entityManager) =>
    {
      EventQueueBridge.EnqueueTriggerAction("Skeleton.OnStartOfBattle", () =>
      {
        EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Enemy"), Type = AppliedPassiveType.Armor, Delta = Armor });
      }, AppliedPassivesManagementSystem.Duration);
    };
  }

  public override IEnumerable<string> GetAttackIds(EntityManager entityManager, int turnNumber)
  {
    int random = Random.Shared.Next(0, 100);
    var linkers = new List<string> { "bone_strike", "sweep", "calcify" };
    if (random <= 65)
    {
      var selected = ArrayUtils.TakeRandomWithReplacement(linkers, 3);
      var sweepCount = selected.Count(x => x == "sweep");
      while (sweepCount > 2)
      {
        selected = ArrayUtils.TakeRandomWithReplacement(linkers, 3);
        sweepCount = selected.Count(x => x == "sweep");
      }
      int haveNoMercy = Random.Shared.Next(0, 100);
      if (haveNoMercy <= 5)
      {
        var selected2 = ArrayUtils.TakeRandomWithReplacement(linkers, 2);
        selected2 = selected2.Append("have_no_mercy");
        selected = ArrayUtils.Shuffled(selected2);
      }
      return selected;
    }
    return ["skull_crusher"];
  }
}

public class BoneStrike : EnemyAttackBase
{
  public BoneStrike()
  {
    Id = "bone_strike";
    Name = "Bone Strike";
    Damage = 2;
    ConditionType = ConditionType.OnHit;
    Text = EnemyAttackTextHelper.GetText(EnemyAttackTextType.Penance, 1, ConditionType.OnHit);

    OnAttackHit = (entityManager) =>
    {
      EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Player"), Type = AppliedPassiveType.Penance, Delta = ValuesParse[0] });
    };
  }
}

public class Sweep : EnemyAttackBase
{
  public Sweep()
  {
    Id = "sweep";
    Name = "Sweep";
    Damage = 4;
    Text = EnemyAttackTextHelper.GetText(EnemyAttackTextType.Corrode, 1);

    OnAttackReveal = (entityManager) =>
    {
      if (IsOneBattleOrLastBattle)
      {
        Text = string.Empty;
      }
    };
    
    OnBlockProcessed = (entityManager, card) =>
    {
      if (!IsOneBattleOrLastBattle)
      {
        // TODO: should send an event
        BlockValueService.ApplyDelta(card, -ValuesParse[0], "Corrode");
      }
    };
  }
}

public class Calcify : EnemyAttackBase
{
  public Calcify()
  {
    Id = "calcify";
    Name = "Calcify";
    Damage = 2;
    ConditionType = ConditionType.OnHit;
    Text = EnemyAttackTextHelper.GetText(EnemyAttackTextType.Armor, 1, ConditionType);

    OnAttackHit = (entityManager) =>
    {
      EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Enemy"), Type = AppliedPassiveType.Armor, Delta = ValuesParse[0] });
    };
  }
}

public class SkullCrusher : EnemyAttackBase
{
  public SkullCrusher()
  {
    Id = "skull_crusher";
    Name = "Skull Crusher";
    Damage = 9;
  }
}