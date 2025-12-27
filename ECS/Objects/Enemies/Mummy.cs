using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Objects.Enemies;

namespace Crusaders30XX.ECS.Objects.EnemyAttacks;

public class Mummy : EnemyBase
{
  public Mummy()
  {
    Id = "mummy";
    Name = "Mummy";
    MaxHealth = 70;
  }
  public override IEnumerable<string> GetAttackIds(EntityManager entityManager, int turnNumber)
  {
    int random = Random.Shared.Next(0, 100);
    if (random <= 70)
    {
      return ["entomb"];
    }
    return ["mummify"];
  }
}

public class Entomb : EnemyAttackBase
{
  public Entomb()
  {
    Id = "entomb";
    Name = "Entomb";
    Damage = 16;
    Text = EnemyAttackTextHelper.GetText(EnemyAttackTextType.GlassCannon, 2);

    ProgressOverride = (entityManager) =>
    {
      var p = entityManager.GetEntitiesWithComponent<EnemyAttackProgress>().FirstOrDefault().GetComponent<EnemyAttackProgress>();
      var assignedBlockCards = entityManager.GetEntitiesWithComponent<AssignedBlockCard>()
              .Where(e => !e.GetComponent<AssignedBlockCard>().IsEquipment)
              .ToList();
      var cards = entityManager.GetEntitiesWithComponent<CardData>().ToList();
      cards.ForEach(e => entityManager.RemoveComponent<ExhaustOnBlock>(e));
      if (assignedBlockCards.Count == ValuesParse[0])
      {
        int fullDamage = p.DamageBeforePrevention > 0 ? p.DamageBeforePrevention : Damage;
        int blockFromCards = Math.Max(0, p.AssignedBlockTotal);

        p.ActualDamage = 0;
        p.IsConditionMet = true;
        p.BaseDamage = Damage;
        p.AegisTotal = 0;
        p.PreventedDamageFromBlockCondition = Math.Max(0, fullDamage - blockFromCards);
        p.TotalPreventedDamage = fullDamage;
        p.FullyPreventedBySpecial = true;
        foreach (var e in assignedBlockCards)
        {
          entityManager.AddComponent(e, new ExhaustOnBlock { Owner = e });
        }
        return true;
      }
      p.IsConditionMet = false;
      return false;
    };
  }
}

public class Mummify : EnemyAttackBase
{
  public Mummify()
  {
    Id = "mummify";
    Name = "Mummify";
    Damage = 8;
    ConditionType = ConditionType.OnHit;
    Text = EnemyAttackTextHelper.GetText(EnemyAttackTextType.Penance, 2, ConditionType);

    OnAttackHit = (entityManager) =>
    {
      EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Player"), Type = AppliedPassiveType.Penance, Delta = ValuesParse[0] });
    };
  }
}