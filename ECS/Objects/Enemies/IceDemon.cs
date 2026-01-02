using System.Collections.Generic;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Objects.EnemyAttacks;
using Crusaders30XX.ECS.Utils;

namespace Crusaders30XX.ECS.Objects.Enemies
{
  public class IceDemon : EnemyBase
  {
    public IceDemon()
    {
      Id = "ice_demon";
      Name = "Ice Demon";
      MaxHealth = 100;
    }

    public override IEnumerable<string> GetAttackIds(EntityManager entityManager, int turnNumber)
    {
      return ArrayUtils.TakeRandomWithReplacement(new List<string> { "icy_blade", "frozen_claw" }, 1);
    }
  }
}

public class IcyBlade : EnemyAttackBase
{
  private int Frostbite = 2;
  public IcyBlade()
  {
    Id = "icy_blade";
    Name = "Icy Blade";
    Damage = 10;
    ConditionType = ConditionType.OnBlockedByAtLeast1Card;
    Text = EnemyAttackTextHelper.GetText(EnemyAttackTextType.Frostbite, Frostbite, ConditionType);

    OnAttackHit = (entityManager) =>
    {
      EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Player"), Type = AppliedPassiveType.Frostbite, Delta = Frostbite });
    };
  }
}

public class FrozenClaw : EnemyAttackBase
{
  public FrozenClaw()
  {
    Id = "frozen_claw";
    Name = "Frozen Claw";
    Damage = 6;
    ConditionType = ConditionType.OnHit;
    Text = "On attack - Intimidate 1 card.\n\nOn hit - Freeze the top card of your draw pile.";

    OnAttackReveal = (entityManager) =>
    {
      EventManager.Publish(new IntimidateEvent { Amount = 1 });
      EventManager.Publish(new FreezeCardsEvent { Amount = 1, Type = FreezeType.TopXCards });
    };
  }
}