using System;
using System.Collections.Generic;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Objects.EnemyAttacks;

namespace Crusaders30XX.ECS.Objects.Enemies;

public class DustWuurm : EnemyBase
{
  public DustWuurm(EnemyDifficulty difficulty = EnemyDifficulty.Easy) : base(difficulty)
  {
    Id = "dust_wuurm";
    Name = "Dust Wuurm";
    MaxHealth = 110;

    OnStartOfBattle = (entityManager) =>
    {
      EventManager.Subscribe<ChangeBattlePhaseEvent>(OnChangeBattlePhaseEvent);
    };
  }

  public override IEnumerable<string> GetAttackIds(EntityManager entityManager, int turnNumber)
  {
    return ["dust_storm"];
  }

  private void OnChangeBattlePhaseEvent(ChangeBattlePhaseEvent evt)
  {
    if (evt.Current == SubPhase.EnemyStart)
    {
      EventManager.Publish(new ApplyPassiveEvent { Target = EntityManager.GetEntity("Enemy"), Type = AppliedPassiveType.Power, Delta = 1 });
    }
  }

  public override void Dispose()
  {
    EventManager.Unsubscribe<ChangeBattlePhaseEvent>(OnChangeBattlePhaseEvent);
    Console.WriteLine($"[DustWuurm] Unsubscribed from ChangeBattlePhaseEvent");
  }

}

public class DustStorm : EnemyAttackBase
{
  public DustStorm()
  {
    Id = "dust_storm";
    Name = "Dust Storm";
    Damage = 7;
  }
}