using System.Collections.Generic;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Objects.Enemies;
using Crusaders30XX.ECS.Utils;
using Crusaders30XX.ECS.Services;
using Crusaders30XX.ECS.Data.Tutorials;
using Crusaders30XX.ECS.Components;

namespace Crusaders30XX.ECS.Objects.EnemyAttacks;

public class SandCorpse : EnemyBase
{
  public SandCorpse(EnemyDifficulty difficulty = EnemyDifficulty.Easy) : base(difficulty)
  {
    Id = "sand_corpse";
    Name = "Sand Corpse";
    IsTutorialOnly = true;
    HealthPerCard = 0.825f;
  }
  public override IEnumerable<string> GetAttackIds(EntityManager entityManager, int turnNumber)
  {
    if (GuidedTutorialService.IsActive(entityManager))
      return GuidedTutorialDefinitions.GetTurn(TutorialBattle.SandCorpse, turnNumber).AttackIds;
    return ArrayUtils.Shuffled(["sand_blast", "sand_storm"]);
  }
}
public class TutorialSandBlast : EnemyAttackBase
{
  public TutorialSandBlast()
  {
    Id = "tutorial_sand_blast";
    Name = "Sand Blast";
    Damage = 4;
    GuardConversionChance = 0f;
  }
}

public class TutorialSandStorm : EnemyAttackBase
{
  public TutorialSandStorm()
  {
    Id = "tutorial_sand_storm";
    Name = "Sand Storm";
    Damage = 3;
    GuardConversionChance = 0f;
  }
}
public class SandBlast : EnemyAttackBase
{
  public SandBlast()
  {
    Id = "sand_blast";
    Name = "Sand Blast";
    Damage = 4;
    GuardConversionChance = 0f;
  }
}

public class SandStorm : EnemyAttackBase
{
  public SandStorm()
  {
    Id = "sand_storm";
    Name = "Sand Storm";
    Damage = 3;
    GuardConversionChance = 0f;
  }
}
