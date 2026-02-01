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
  private const int SanguineCursePenance = 1;

  private int SanguineCurseCurrent = 0;
  private bool HasTriggeredThisTurn = false;

  public BloodMartyr(EnemyDifficulty difficulty = EnemyDifficulty.Easy) : base(difficulty)
  {
    Id = "blood_martyr";
    Name = "Blood Martyr";
    MaxHealth = 32;

    OnStartOfBattle = (entityManager) =>
    {
      EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Enemy"), Type = AppliedPassiveType.SanguineCurse, Delta = 1 });
      EventManager.Subscribe<ModifyHpEvent>(OnModifyHp);
      EventManager.Subscribe<ChangeBattlePhaseEvent>(OnChangeBattlePhase);
    };
  }

  private void OnChangeBattlePhase(ChangeBattlePhaseEvent evt)
  {
    if (evt.Current == SubPhase.EnemyStart)
    {
      this.SanguineCurseCurrent = 0;
      this.HasTriggeredThisTurn = false;
    }
  }

  private void OnModifyHp(ModifyHpEvent evt)
  {
    if (this.HasTriggeredThisTurn) return;
    // Only track damage dealt to enemy
    if (evt.Delta >= 0) return;
    var enemy = EntityManager.GetEntity("Enemy");
    if (evt.Target != enemy) return;

    // Calculate actual damage (already accounts for Armor/Wounded in the HP system)
    int actualDamage = Math.Abs(evt.Delta);

    this.SanguineCurseCurrent += actualDamage;

    if (SanguineCurseCurrent >= SanguineCurseThreshold)
    {
      EventManager.Publish(new ApplyPassiveEvent
      {
        Target = EntityManager.GetEntity("Player"),
        Type = AppliedPassiveType.Penance,
        Delta = SanguineCursePenance
      });
      this.SanguineCurseCurrent = 0;
      this.HasTriggeredThisTurn = false;
    }
  }

  public override IEnumerable<string> GetAttackIds(EntityManager entityManager, int turnNumber)
  {
    var attacks = new List<string> { "flagellation", "blood_ward", "blood_tithe", "masochism" };
    return ArrayUtils.TakeRandomWithoutReplacement(attacks, 2);
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
  private const int BurnAmount = 1;
  private const int WoundedAmount = 1;

  public Flagellation()
  {
    Id = "flagellation";
    Name = "Flagellation";
    Damage = 7;
    ConditionType = ConditionType.OnHit;
    Text = $"On attack - this enemy gains {BurnAmount} burn and {WoundedAmount} wounded.";

    OnAttackReveal = (entityManager) =>
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
  private const int ArmorAmount = 1;

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

public class Masochism : EnemyAttackBase
{
  private const int SelfDamage = 1;
  public Masochism()
  {
    Id = "masochism";
    Name = "Masochism";
    Damage = 4;
    ConditionType = ConditionType.OnBlockedByAtLeast1Card;
    Text = $"On not blocked by at least one card - The enemy loses {SelfDamage} HP.";

    OnAttackHit = (entityManager) =>
    {
      EventManager.Publish(new ModifyHpRequestEvent { 
        Source = EntityManager.GetEntity("Enemy"), 
        Target = EntityManager.GetEntity("Enemy"), 
        Delta = -SelfDamage, 
        DamageType = ModifyTypeEnum.Effect 
      });
    };
  }
}

public class BloodTithe : EnemyAttackBase
{
  private const int BaseDamage = 3;
  private const int BonusDamage = 2;

  public BloodTithe()
  {
    Id = "blood_tithe";
    Name = "Blood Tithe";
    Damage = BaseDamage;
    Text = $"This attack gains +{BonusDamage} if you have penance.\n\nOn hit - remove all burn and wounded from Blood Martyr.";

    OnAttackReveal = (entityManager) =>
    {
      var playerPassives = GetComponentHelper.GetAppliedPassives(entityManager, "Player");
      if (playerPassives?.Passives != null &&
          playerPassives.Passives.TryGetValue(AppliedPassiveType.Penance, out int penanceStacks) &&
          penanceStacks > 0)
      {
        Damage = BaseDamage + BonusDamage;
      }
      else
      {
        Damage = BaseDamage;
      }
    };

    OnAttackHit = (entityManager) =>
    {
      EventManager.Publish(new RemovePassive
      {
        Owner = entityManager.GetEntity("Enemy"),
        Type = AppliedPassiveType.Wounded,
      });
      EventManager.Publish(new RemovePassive
      {
        Owner = entityManager.GetEntity("Enemy"),
        Type = AppliedPassiveType.Burn,
      });
    };
  }
}
