using System;
using System.Collections.Generic;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Objects.EnemyAttacks;
using Crusaders30XX.ECS.Services;
using Crusaders30XX.ECS.Utils;

namespace Crusaders30XX.ECS.Objects.Enemies;

public class BloodMartyr : EnemyBase
{
  private const int SanguineCurseThreshold = 7;
  private const int SanguineCurseBleed = 4;

  public BloodMartyr(EnemyDifficulty difficulty = EnemyDifficulty.Easy) : base(difficulty)
  {
    Id = "blood_martyr";
    Name = "Blood Martyr";
    MaxHealth = 80;

    OnStartOfBattle = (entityManager) =>
    {
      EntityManager = entityManager;
      EventManager.Subscribe<ModifyHpEvent>(OnModifyHp);
      EventManager.Subscribe<ChangeBattlePhaseEvent>(OnChangeBattlePhase);
    };
  }

  private void OnChangeBattlePhase(ChangeBattlePhaseEvent evt)
  {
    if (evt.Current == SubPhase.EnemyStart)
    {
      var battleStateInfo = GetComponentHelper.GetBattleStateInfo(EntityManager);
      if (battleStateInfo != null)
      {
        battleStateInfo.BattleTracking["sanguine_curse_triggered"] = 0;
      }
    }
  }

  private void OnModifyHp(ModifyHpEvent evt)
  {
    // Only track damage dealt to enemy
    if (evt.Delta >= 0) return;
    var enemy = EntityManager.GetEntity("Enemy");
    if (evt.Target != enemy) return;

    // Check if already triggered this turn
    var battleStateInfo = GetComponentHelper.GetBattleStateInfo(EntityManager);
    if (battleStateInfo == null) return;

    battleStateInfo.BattleTracking.TryGetValue("sanguine_curse_triggered", out int triggered);
    if (triggered > 0) return;

    // Calculate actual damage (already accounts for Armor/Wounded in the HP system)
    int actualDamage = Math.Abs(evt.Delta);

    if (actualDamage >= SanguineCurseThreshold)
    {
      battleStateInfo.BattleTracking["sanguine_curse_triggered"] = 1;
      EventManager.Publish(new ApplyPassiveEvent
      {
        Target = EntityManager.GetEntity("Player"),
        Type = AppliedPassiveType.Bleed,
        Delta = SanguineCurseBleed
      });
    }
  }

  public override IEnumerable<string> GetAttackIds(EntityManager entityManager, int turnNumber)
  {
    var attacks = new List<string> { "flagellation", "blood_ward", "blood_tithe" };
    return ArrayUtils.TakeRandomWithoutReplacement(attacks, 1);
  }

  public override void Dispose()
  {
    EventManager.Unsubscribe<ModifyHpEvent>(OnModifyHp);
    EventManager.Unsubscribe<ChangeBattlePhaseEvent>(OnChangeBattlePhase);
    Console.WriteLine($"[BloodMartyr] Unsubscribed from events");
  }
}

public class Flagellation : EnemyAttackBase
{
  private const int BurnAmount = 2;
  private const int WoundedAmount = 1;

  public Flagellation()
  {
    Id = "flagellation";
    Name = "Flagellation";
    Damage = 3;
    ConditionType = ConditionType.OnHit;
    Text = $"On hit - this enemy gains {BurnAmount} burn and {WoundedAmount} wounded.";

    OnAttackHit = (entityManager) =>
    {
      EventManager.Publish(new ApplyPassiveEvent
      {
        Target = entityManager.GetEntity("Enemy"),
        Type = AppliedPassiveType.Burn,
        Delta = BurnAmount
      });
      EventManager.Publish(new ApplyPassiveEvent
      {
        Target = entityManager.GetEntity("Enemy"),
        Type = AppliedPassiveType.Wounded,
        Delta = WoundedAmount
      });
    };
  }
}

public class BloodWard : EnemyAttackBase
{
  private const int ArmorAmount = 2;

  public BloodWard()
  {
    Id = "blood_ward";
    Name = "Blood Ward";
    Damage = 3;
    ConditionType = ConditionType.OnHit;
    Text = $"On hit - this enemy gains {ArmorAmount} armor.";

    OnAttackHit = (entityManager) =>
    {
      EventManager.Publish(new ApplyPassiveEvent
      {
        Target = entityManager.GetEntity("Enemy"),
        Type = AppliedPassiveType.Armor,
        Delta = ArmorAmount
      });
    };
  }
}

public class BloodTithe : EnemyAttackBase
{
  private const int BaseDamage = 5;
  private const int BonusDamage = 2;

  public BloodTithe()
  {
    Id = "blood_tithe";
    Name = "Blood Tithe";
    Damage = BaseDamage;

    OnAttackReveal = (entityManager) =>
    {
      var playerPassives = GetComponentHelper.GetAppliedPassives(entityManager, "Player");
      if (playerPassives?.Passives != null &&
          playerPassives.Passives.TryGetValue(AppliedPassiveType.Bleed, out int bleedStacks) &&
          bleedStacks > 0)
      {
        Damage = BaseDamage + BonusDamage;
        Text = $"+{BonusDamage} damage (you have bleed).";
      }
      else
      {
        Damage = BaseDamage;
        Text = "";
      }
    };
  }
}
