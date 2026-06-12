using System;
using System.Collections.Generic;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Objects.Enemies;
using Crusaders30XX.ECS.Services;
using Crusaders30XX.ECS.Data.Tutorials;
using Crusaders30XX.ECS.Components;

namespace Crusaders30XX.ECS.Objects.EnemyAttacks;

public class Gleeber : EnemyBase
{
  public Gleeber(EnemyDifficulty difficulty = EnemyDifficulty.Easy) : base(difficulty)
  {
    Id = "gleeber";
    Name = "Gleeber";
    IsTutorialOnly = true;
    HealthPerCard = 0.715f;
  }
  public override IEnumerable<string> GetAttackIds(EntityManager entityManager, int turnNumber)
  {
    if (GuidedTutorialService.IsActive(entityManager))
      return GuidedTutorialDefinitions.GetTurn(TutorialBattle.Gleeber, turnNumber).AttackIds;
    Console.WriteLine("[Gleeber] GetAttackIds: turnNumber=" + turnNumber);
    return ["pounce"];
  }
}
public class TutorialGleeberStrike : EnemyAttackBase
{
  public TutorialGleeberStrike()
  {
    Id = "tutorial_gleeber_strike";
    Name = "Pounce";
    Damage = 9;
    GuardConversionChance = 0f;
  }
}
public class Pounce : EnemyAttackBase
{
  public Pounce()
  {
    Id = "pounce";
    Name = "Pounce";
    Damage = 5;
    GuardConversionChance = 0f;
  }
}
