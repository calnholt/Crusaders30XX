using System.Collections.Generic;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Objects.EnemyAttacks;
using Crusaders30XX.ECS.Systems;

namespace Crusaders30XX.ECS.Objects.Enemies
{
  public class Berserker : EnemyBase
  {
    private int WoundedAmount = 5;
    private int ShackledAmount = 4;
    public Berserker(EnemyDifficulty difficulty = EnemyDifficulty.Easy) : base(difficulty)
    {
      Id = "berserker";
      Name = "Berserker";
      MaxHealth = 95;

      OnStartOfBattle = (entityManager) =>
      {
        EventQueueBridge.EnqueueTriggerAction("Berserker.OnStartOfBattle", () => {  
          EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Enemy"), Type = AppliedPassiveType.Wounded, Delta = WoundedAmount });
        }, AppliedPassivesManagementSystem.Duration);
        EventQueueBridge.EnqueueTriggerAction("Berserker.OnStartOfBattle", () => {  
          EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Player"), Type = AppliedPassiveType.Shackled, Delta = ShackledAmount });
        }, AppliedPassivesManagementSystem.Duration);
      };
    }
    public override IEnumerable<string> GetAttackIds(EntityManager entityManager, int turnNumber)
    {
      return ["rage"];
    }
  }
}

public class Rage : EnemyAttackBase
{
  public Rage()
  {
    Id = "rage";
    Name = "Rage";
    Damage = 11;
  }
}