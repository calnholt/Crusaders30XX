using System;
using System.Collections.Generic;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;

namespace Crusaders30XX.ECS.Objects.Enemies;

public abstract class EnemyBase : IDisposable
{


  public string Id { get; set; }
  public string Name { get; set; }
  public int MaxHealth { get; set; }
  public int CurrentHealth { get; set; }
  public Action<EntityManager> OnStartOfBattle { get; protected set; }
  public EntityManager EntityManager { get; set; }

  public virtual void Dispose()
  {
    Console.WriteLine($"[EnemyBase] Dispose: {Id}");
  }

  public abstract IEnumerable<string> GetAttackIds(EntityManager entityManager, int turnNumber);
}