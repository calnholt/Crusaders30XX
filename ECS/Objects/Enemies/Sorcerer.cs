using System;
using System.Collections.Generic;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Objects.EnemyAttacks;
using Crusaders30XX.ECS.Services;
using Crusaders30XX.ECS.Utils;

namespace Crusaders30XX.ECS.Objects.Enemies;

public class Sorcerer : EnemyBase
{
  public Sorcerer()
  {
    Id = "sorcerer";
    Name = "Sorcerer";
    MaxHealth = 120;

    OnStartOfBattle = (entityManager) =>
    {
      EventQueueBridge.EnqueueTriggerAction("Sorcerer.OnCreate", () =>
      {
        EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Player"), Type = AppliedPassiveType.Intimidated, Delta = 1 });

      }, 0.5f);
      EventQueueBridge.EnqueueTriggerAction("Sorcerer.OnCreate", () =>
      {
        EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Player"), Type = AppliedPassiveType.MindFog, Delta = 1 });
      }, 0.5f);
      EventQueueBridge.EnqueueTriggerAction("Sorcerer.OnCreate", () =>
      {
        EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Enemy"), Type = AppliedPassiveType.Channel, Delta = 1 });
      }, 0.5f);
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
  private int Channel = 1;
  public StrangeForce()
  {
    Id = "strange_force";
    Name = "Strange Force";
    Damage = 11;
    ConditionType = ConditionType.OnHit;

    OnAttackReveal = (entityManager) =>
    {
      var passives = GetComponentHelper.GetAppliedPassives(entityManager, "Enemy");
      passives.Passives.TryGetValue(AppliedPassiveType.Channel, out int channelStacks);
      Text = $"On attack - Draw {DrawCount} {(DrawCount == 1 ? "card" : "cards")}.\n\nOn hit - Mill cards equal to the enemy's channel ({channelStacks}) and the enemy gains {Channel} channel.";
      EventManager.Publish(new RequestDrawCardsEvent { Count = DrawCount });
    };

    OnAttackHit = (entityManager) =>
    {
      var passives = GetComponentHelper.GetAppliedPassives(entityManager, "Enemy");
      passives.Passives.TryGetValue(AppliedPassiveType.Channel, out int channelStacks);
      for (int i = 0; i < channelStacks; i++)
      {
        EventQueueBridge.EnqueueTriggerAction("StrangeForce.OnAttackHit.Mill", () =>
        {
          EventManager.Publish(new MillCardEvent { });
        }, 0.5f);
      }
      EventQueueBridge.EnqueueTriggerAction("StrangeForce.OnAttackHit.Channel", () =>
      {
        EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Enemy"), Type = AppliedPassiveType.Channel, Delta = Channel });
      }, 0.5f);
    };

  }
}