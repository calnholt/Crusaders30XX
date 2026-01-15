using System.Collections.Generic;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Objects.Enemies;
using Crusaders30XX.ECS.Utils;

namespace Crusaders30XX.ECS.Objects.EnemyAttacks;

public class SandCorpse : EnemyBase
{
  public SandCorpse(EnemyDifficulty difficulty = EnemyDifficulty.Easy) : base(difficulty)
  {
    Id = "sand_corpse";
    Name = "Sand Corpse";
    MaxHealth = 60;
  }
  public override IEnumerable<string> GetAttackIds(EntityManager entityManager, int turnNumber)
  {
    return ArrayUtils.Shuffled(["sand_blast", "sand_storm"]);
  }
}
public class SandBlast : EnemyAttackBase
{
  public SandBlast()
  {
    Id = "sand_blast";
    Name = "Sand Blast";
    Damage = 4;
  }
}

public class SandStorm : EnemyAttackBase
{
  public SandStorm()
  {
    Id = "sand_storm";
    Name = "Sand Storm";
    Damage = 3;
  }
}