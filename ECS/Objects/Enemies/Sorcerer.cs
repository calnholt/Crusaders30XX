using System;
using System.Collections.Generic;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Objects.EnemyAttacks;
using Crusaders30XX.ECS.Systems;
using Crusaders30XX.ECS.Utils;

namespace Crusaders30XX.ECS.Objects.Enemies;

public class Sorcerer : EnemyBase
{
  public Sorcerer(EnemyDifficulty difficulty = EnemyDifficulty.Easy) : base(difficulty)
  {
    Id = "sorcerer";
    Name = "Sorcerer";
    HP = 25;

    OnStartOfBattle = (entityManager) =>
    {
      // EventQueueBridge.EnqueueTriggerAction("Sorcerer.OnCreate", () =>
      // {
      //   EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Player"), Type = AppliedPassiveType.Intimidated, Delta = 1 });

      // }, AppliedPassivesManagementSystem.Duration);
      EventQueueBridge.EnqueueTriggerAction("Sorcerer.OnCreate", () =>
      {
        EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Player"), Type = AppliedPassiveType.MindFog, Delta = 1 });
      }, AppliedPassivesManagementSystem.Duration);
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
  private int DrawCount = 1;
  private int IntimidateAmount = 1;

  public StrangeForce()
  {
    Id = "strange_force";
    Name = "Strange Force";
    Damage = 11;
    ConditionType = ConditionType.OnBlockedByAtLeast2DifferentColors;
    Text = $"On attack - Draw {DrawCount} {(DrawCount == 1 ? "card" : "cards")}, intimidate {IntimidateAmount}.\n\n{EnemyAttackTextHelper.GetConditionText(ConditionType)}Mill 1.";

    OnAttackReveal = (entityManager) =>
    {
      EventManager.Publish(new RequestDrawCardsEvent { Count = DrawCount });
      EventManager.Publish(new IntimidateEvent { Amount = IntimidateAmount });
    };

    OnAttackHit = (entityManager) =>
    {
      EventQueueBridge.EnqueueTriggerAction("StrangeForce.OnAttackHit.Mill", () =>
      {
        EventManager.Publish(new MillCardEvent { });
      }, 0.5f);
    };
  }
}
