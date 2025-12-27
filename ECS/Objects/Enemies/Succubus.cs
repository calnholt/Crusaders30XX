using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Objects.Enemies;
using Crusaders30XX.ECS.Services;
using Crusaders30XX.ECS.Utils;
using static Crusaders30XX.ECS.Systems.MustBeBlockedSystem;

namespace Crusaders30XX.ECS.Objects.EnemyAttacks;

public class Succubus : EnemyBase
{
  public Succubus()
  {
    Id = "succubus";
    Name = "Succubus";
    MaxHealth = 85;
  }

  public override IEnumerable<string> GetAttackIds(EntityManager entityManager, int turnNumber)
  {
    var linkers = new List<string> { "enthralling_gaze", "teasing_nip", "crushing_adoration" };
    var battleStateInfo = GetComponentHelper.GetBattleStateInfo(entityManager);
    battleStateInfo.BattleTracking.TryGetValue("courage_lost", out int count);
    var enders = new List<string> { "soul_siphon" };
    if (count > 0)
    {
      enders.Add("velvet_fangs");
    }
    var attacks = ArrayUtils.TakeRandomWithReplacement(linkers, 2).Concat(ArrayUtils.TakeRandomWithReplacement(enders, 1));
    return ArrayUtils.Shuffled(attacks);
  }

  public static void HandleLoseCourage(EntityManager entityManager, int amount)
  {
    var courage = entityManager.GetEntitiesWithComponent<Courage>().FirstOrDefault();
    if (courage == null) return;
    var courageAmount = courage.GetComponent<Courage>().Amount;
    int courageLost = System.Math.Min(System.Math.Abs(amount), courageAmount);
    EventManager.Publish(new ModifyCourageEvent { Delta = -amount });
    EventManager.Publish(new TrackingEvent { Type = "courage_lost", Delta = courageLost });
  }

}

public class SoulSiphon : EnemyAttackBase
{
  public SoulSiphon()
  {
    Id = "soul_siphon";
    Name = "Soul Siphon";
    Damage = 5;
    ConditionType = ConditionType.OnHit;
    BlockingRestrictionType = BlockingRestrictionType.NotRed;
    Text = $"{EnemyAttackTextHelper.GetBlockingRestrictionText(BlockingRestrictionType)}\n\n{EnemyAttackTextHelper.GetText(EnemyAttackTextType.Custom, 0, ConditionType, 100, "Lose [2] courage.")}";

    OnAttackHit = (entityManager) =>
    {
      Succubus.HandleLoseCourage(entityManager, ValuesParse[0]);
    };
  }
}

public class EnthrallingGaze : EnemyAttackBase
{
  public EnthrallingGaze()
  {
    Id = "enthralling_gaze";
    Name = "Enthralling Gaze";
    Damage = 3;
    ConditionType = ConditionType.MustBeBlockedByAtLeast1Card;
    Text = EnemyAttackTextHelper.GetText(EnemyAttackTextType.MustBeBlockedByAtLeast, 1);

    OnAttackReveal = (entityManager) =>
    {
      EventManager.Publish(new MustBeBlockedEvent { Threshold = ValuesParse[0], Type = MustBeBlockedByType.AtLeast });
    };
  }
}

public class TeasingNip : EnemyAttackBase
{
  public TeasingNip()
  {
    Id = "teasing_nip";
    Name = "Teasing Nip";
    Damage = 2;
    ConditionType = ConditionType.OnHit;
    Text = EnemyAttackTextHelper.GetText(EnemyAttackTextType.Custom, 0, ConditionType, 50, "Lose [1] courage.");

    OnAttackHit = (entityManager) =>
    {
      if (Random.Shared.Next(0, 100) >= 50) return;
      Succubus.HandleLoseCourage(entityManager, ValuesParse[0]);
    };
  }
}

public class CrushingAdoration : EnemyAttackBase
{
  public CrushingAdoration()
  {
    Id = "crushing_adoration";
    Name = "Crushing Adoration";
    Damage = 3;
    ConditionType = ConditionType.OnHit;
    Text = EnemyAttackTextHelper.GetText(EnemyAttackTextType.Aggression, 2, ConditionType);

    OnAttackHit = (entityManager) =>
    {
      EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Enemy"), Type = AppliedPassiveType.Aggression, Delta = ValuesParse[0] });
    };
  }
}

public class VelvetFangs : EnemyAttackBase
{
  public int Multiplier { get; set; } = 4;
  public VelvetFangs()
  {
    Id = "velvet_fangs";
    Name = "Velvet Fangs";
    Damage = 6;
    ConditionType = ConditionType.OnHit;

    OnAttackReveal = (entityManager) =>
    {
      var battleStateInfo = GetComponentHelper.GetBattleStateInfo(entityManager);
      battleStateInfo.BattleTracking.TryGetValue("courage_lost", out int count);
      Text = EnemyAttackTextHelper.GetText(EnemyAttackTextType.Custom, 0, ConditionType, 100, $"This monster heals {Multiplier} HP per courage lost this battle ({count * Multiplier} HP).");

    };
    OnAttackHit = (entityManager) =>
    {
      var battleStateInfo = GetComponentHelper.GetBattleStateInfo(entityManager);
      battleStateInfo.BattleTracking.TryGetValue("courage_lost", out int count);
      EventManager.Publish(new HealEvent { Target = entityManager.GetEntity("Enemy"), Delta = +(count * Multiplier) });
    };
  }
}
