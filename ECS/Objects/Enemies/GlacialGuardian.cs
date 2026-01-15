using System;
using System.Collections.Generic;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Objects.EnemyAttacks;
using Crusaders30XX.ECS.Systems;
using Crusaders30XX.ECS.Utils;
using static Crusaders30XX.ECS.Systems.MustBeBlockedSystem;

namespace Crusaders30XX.ECS.Objects.Enemies;

public class GlacialGuardian : EnemyBase
{
  public GlacialGuardian(EnemyDifficulty difficulty = EnemyDifficulty.Easy) : base(difficulty)
  {
    Id = "glacial_guardian";
    Name = "Glacial Guardian";
    MaxHealth = 110;

    OnStartOfBattle = (entityManager) =>
    {
      EventManager.Subscribe<CardMoved>(OnCardMoved, priority: 10);
      EventManager.Subscribe<ChangeBattlePhaseEvent>(OnChangeBattlePhase);
      EventQueueBridge.EnqueueTriggerAction("GlacialGuardian.OnCreate", () =>
      {
        EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Player"), Type = AppliedPassiveType.Windchill, Delta = 1 });
      }, AppliedPassivesManagementSystem.Duration);

      EventQueueBridge.EnqueueTriggerAction("GlacialGuardian.OnCreate", () =>
      {
        EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Player"), Type = AppliedPassiveType.SubZero, Delta = 1 });
      }, AppliedPassivesManagementSystem.Duration);
    };
  }

  public override IEnumerable<string> GetAttackIds(EntityManager entityManager, int turnNumber)
  {
    return ArrayUtils.TakeRandomWithoutReplacement(new List<string> { "glacial_strike", "glacial_blast" }, 1);
  }

  private void OnChangeBattlePhase(ChangeBattlePhaseEvent evt)
  {
    Console.WriteLine($"[GlacialGuardian] OnChangeBattlePhase - {evt.Current}");
    if (evt.Current == SubPhase.EnemyStart)
    {
      EventManager.Publish(new FreezeCardsEvent { Amount = 1, Type = FreezeType.Hand });
    }
  }

  private void OnCardMoved(CardMoved evt)
  {
    if ((evt.To == CardZoneType.DiscardPile || evt.To == CardZoneType.ExhaustPile) && evt.From == CardZoneType.AssignedBlock && evt.Card.GetComponent<Frozen>() != null)
    {
      EventManager.Publish(new ApplyPassiveEvent { Target = EntityManager.GetEntity("Player"), Type = AppliedPassiveType.Penance, Delta = 1 });
    }
  }

  public override void Dispose()
  {
    EventManager.Unsubscribe<CardMoved>(OnCardMoved);
    EventManager.Unsubscribe<ChangeBattlePhaseEvent>(OnChangeBattlePhase);
    base.Dispose();
  }
}

public class GlacialStrike : EnemyAttackBase
{
  public GlacialStrike()
  {
    Id = "glacial_strike";
    Name = "Glacial Strike";
    Damage = 8;
    ConditionType = ConditionType.MustBeBlockedByAtLeast1Card;
    Text = EnemyAttackTextHelper.GetText(EnemyAttackTextType.MustBeBlockedByAtLeast, 1);

    OnAttackReveal = (entityManager) =>
{
  EventManager.Publish(new MustBeBlockedEvent { Threshold = ValuesParse[0], Type = MustBeBlockedByType.AtLeast });
};
  }
}

public class GlacialBlast : EnemyAttackBase
{
  public GlacialBlast()
  {
    Id = "glacial_blast";
    Name = "Glacial Blast";
    Damage = 11;
    ConditionType = ConditionType.MustBeBlockedByAtLeast2Cards;
    Text = EnemyAttackTextHelper.GetText(EnemyAttackTextType.MustBeBlockedByAtLeast, 2);

    OnAttackReveal = (entityManager) =>
    {
      EventManager.Publish(new MustBeBlockedEvent { Threshold = ValuesParse[0], Type = MustBeBlockedByType.AtLeast });
    };
  }
}