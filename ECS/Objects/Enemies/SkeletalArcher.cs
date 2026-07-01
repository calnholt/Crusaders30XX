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

public class SkeletalArcher : EnemyBase
{
  private int Armor = 1;
  private int SnipeCount = 0;

  public SkeletalArcher(EnemyDifficulty difficulty = EnemyDifficulty.Easy) : base(difficulty)
  {
    Id = "skeletal_archer";
    Name = "Skeletal Archer";
    HP = 23;

    OnStartOfBattle = (entityManager) =>
    {
      EventQueueBridge.EnqueueTriggerAction("SkeletalArcher.OnStartOfBattle", () =>
      {
        EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Enemy"), Type = AppliedPassiveType.Armor, Delta = Armor });
      }, AppliedPassivesManagementSystem.Duration);
    };
  }

  public override IEnumerable<string> GetAttackIds(EntityManager entityManager, int turnNumber)
  {
    int roll = Random.Shared.Next(0, 100);

    if (roll <= 5 || SnipeCount >= 2 || turnNumber == 1)
    {
      SnipeCount = SnipeCount == 2 ? 0 : SnipeCount;
      // 70%: 3 attacks from pool
      var pool = new List<string> { "piercing_shot", "weathering_shot", "quick_shot" };
      return ArrayUtils.TakeRandomWithoutReplacement(pool, 2);
    }
    else
    {
      // 30%: Single heavy attack
      SnipeCount++;
      return ["snipe"];
    }
  }
}

public class PiercingShot : EnemyAttackBase
{
  private int PiercingDamage = 2;
  public PiercingShot()
  {
    Id = "piercing_shot";
    Name = "Piercing Shot";
    Damage = 4;
    Text = "On Attack - Deal " + PiercingDamage + " damage.";

    OnAttackReveal = (entityManager) =>
    {
      EventQueueBridge.EnqueueTriggerAction("PiercingShot.OnAttackReveal", () =>
      {
        EventManager.Publish(new ModifyHpRequestEvent
        {
          Source = entityManager.GetEntity("Enemy"),
          Target = entityManager.GetEntity("Player"),
          Delta = -PiercingDamage,
          DamageType = ModifyTypeEnum.Effect
        });
      }, AppliedPassivesManagementSystem.Duration);
    };
  }
}

public class WeatheringShot : EnemyAttackBase
{
  public WeatheringShot()
  {
    Id = "weathering_shot";
    Name = "Weathering Shot";
    Damage = 5;
    ConditionType = ConditionType.OnHit;
    Text = EnemyAttackTextHelper.GetText(EnemyAttackTextType.Custom, conditionType: ConditionType.OnHit, customText: "Apply brittle to each card used to block this attack.");

    OnAttackHit = (entityManager) =>
    {
      var assignedBlockCards = entityManager.GetEntitiesWithComponent<AssignedBlockCard>()
        .Where(e => !e.GetComponent<AssignedBlockCard>().IsEquipment)
        .ToList();

      foreach (var card in assignedBlockCards)
      {
        if (card.GetComponent<Brittle>() != null) continue;
        entityManager.AddComponent(card, new Brittle { Owner = card });
        RunScopedStateService.SyncCardRestrictionsFromComponents(card);
      }
    };
  }
}

public class QuickShot : EnemyAttackBase
{
  public QuickShot()
  {
    Id = "quick_shot";
    Name = "Quick Shot";
    Damage = 6;
  }
}

public class Snipe : EnemyAttackBase
{
  public Snipe()
  {
    Id = "snipe";
    Name = "Snipe";
    Damage = 10;
    IgnoresAegis = true;
    Text = EnemyAttackTextHelper.GetText(EnemyAttackTextType.Custom, customText: "Ignores aegis.");
  }
}
