using System;
using System.Collections.Generic;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Objects.Enemies;

namespace Crusaders30XX.ECS.Objects.EnemyAttacks;

public class Gleeber : EnemyBase
{
  public Gleeber(EnemyDifficulty difficulty = EnemyDifficulty.Easy) : base(difficulty)
  {
    Id = "gleeber";
    Name = "Gleeber";
    MaxHealth = 13;
  }
  public override IEnumerable<string> GetAttackIds(EntityManager entityManager, int turnNumber)
  {
    Console.WriteLine("[Gleeber] GetAttackIds: turnNumber=" + turnNumber);
    return ["pounce"];
  }
}
public class Pounce : EnemyAttackBase
{
  public Pounce()
  {
    Id = "pounce";
    Name = "Pounce";
    Damage = 5;
  }
}