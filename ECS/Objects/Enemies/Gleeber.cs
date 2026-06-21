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
    {
      var state = GuidedTutorialService.GetState(entityManager);
      if (state != null)
        return GuidedTutorialDefinitions.GetTurn(state.Section, turnNumber).AttackIds;
    }
    Console.WriteLine("[Gleeber] GetAttackIds: turnNumber=" + turnNumber);
    return ["pounce"];
  }
}

public class TutorialGleeberStrike : EnemyAttackBase
{
  public TutorialGleeberStrike()
  {
    Id = "tutorial_gleeber_strike_9";
    Name = "Pounce";
    Damage = 9;
    GuardConversionChance = 0f;
  }
}

public class TutorialGleeberStrike3 : EnemyAttackBase
{
  public TutorialGleeberStrike3()
  {
    Id = "tutorial_gleeber_strike_3";
    Name = "Pounce";
    Damage = 3;
    GuardConversionChance = 0f;
  }
}

public class TutorialGleeberStrike5 : EnemyAttackBase
{
  public TutorialGleeberStrike5()
  {
    Id = "tutorial_gleeber_strike_5";
    Name = "Pounce";
    Damage = 5;
    GuardConversionChance = 0f;
  }
}

public class TutorialGleeberStrike6 : EnemyAttackBase
{
  public TutorialGleeberStrike6()
  {
    Id = "tutorial_gleeber_strike_6";
    Name = "Pounce";
    Damage = 6;
    GuardConversionChance = 0f;
  }
}

public class TutorialGleeberStrike8 : EnemyAttackBase
{
  public TutorialGleeberStrike8()
  {
    Id = "tutorial_gleeber_strike_8";
    Name = "Pounce";
    Damage = 8;
    GuardConversionChance = 0f;
  }
}

public class TutorialGleeberStrike9 : EnemyAttackBase
{
  public TutorialGleeberStrike9()
  {
    Id = "tutorial_gleeber_strike_9";
    Name = "Pounce";
    Damage = 9;
    GuardConversionChance = 0f;
  }
}
public class TutorialGleeberStrike7 : EnemyAttackBase
{
  public TutorialGleeberStrike7()
  {
    Id = "tutorial_gleeber_strike_7";
    Name = "Pounce";
    Damage = 7;
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
