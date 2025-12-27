using System;
using System.Collections.Generic;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;

namespace Crusaders30XX.ECS.Objects.Enemies;

public abstract class EnemyBase
{
  public string Id { get; set; }
  public string Name { get; set; }
  public int MaxHealth { get; set; }
  public int CurrentHealth { get; set; }
  public List<AppliedPassiveType> Passives { get; set; } = new List<AppliedPassiveType>();
  public Action<EntityManager> OnCreate { get; protected set; }

  public abstract IEnumerable<string> GetAttackIds(EntityManager entityManager, int turnNumber);
}