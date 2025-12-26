using System.Collections.Generic;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Objects.Enemies;

namespace Crusaders30XX.ECS.Objects.EnemyAttacks;

public class Succubus : EnemyBase
{
  public Succubus()
  {
    Id = "succubus";
    Name = "Succubus";
    MaxHealth = 75;
  }

  public override IEnumerable<string> GetAttackIds(EntityManager entityManager, int turnNumber)
  {
    return ["velvet_fangs", "soul_siphon", "enthralling_gaze", "crushing_adoration", "teasing_nip"];
  }
}