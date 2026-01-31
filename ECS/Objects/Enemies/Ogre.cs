using System;
using System.Collections.Generic;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Objects.Enemies;

namespace Crusaders30XX.ECS.Objects.EnemyAttacks
{
  public class Ogre : EnemyBase
  {
    public Ogre(EnemyDifficulty difficulty = EnemyDifficulty.Easy) : base(difficulty)
    {
      Id = "ogre";
      Name = "Ogre";
      MaxHealth = 20;
    }

    public override IEnumerable<string> GetAttackIds(EntityManager entityManager, int turnNumber)
    {
      int random = Random.Shared.Next(0, 100);
      if (random <= 20)
      {
        return ["slam_trunk", "fake_out"];
      }
      if (random <= 40)
      {
        return ["slam_trunk", "thud"];
      }
      if (random <= 60)
      {
        return ["tree_stomp"];
      }
      if (random <= 80)
      {
        return ["pummel_into_submission"];
      }
      return ["slam_trunk", "have_no_mercy"];
    }
  }
  public class PummelIntoSubmission : EnemyAttackBase
  {
    private int Penance = 1;
    public PummelIntoSubmission()
    {
      Id = "pummel_into_submission";
      Name = "Pummel Into Submission";
      Damage = 10;
      ConditionType = ConditionType.OnHit;
      Text = $"{EnemyAttackTextHelper.GetText(EnemyAttackTextType.Intimidate, 1)}\n\n{EnemyAttackTextHelper.GetText(EnemyAttackTextType.Penance, 1, ConditionType)}";
      OnAttackReveal = (entityManager) =>
      {
        EventManager.Publish(new IntimidateEvent { Amount = Penance });
      };
      OnAttackHit = (entityManager) =>
      {
        EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Player"), Type = AppliedPassiveType.Penance, Delta = Penance });
      };
    }
  }
  public class TreeStomp : EnemyAttackBase
  {
    private int IntimidateAmount = 2;
    public TreeStomp()
    {
      Id = "tree_stomp";
      Name = "Tree Stomp";
      Damage = 9;
      Text = EnemyAttackTextHelper.GetText(EnemyAttackTextType.Intimidate, 2);
      OnAttackReveal = (entityManager) =>
      {
        EventManager.Publish(new IntimidateEvent { Amount = IntimidateAmount });
      };
    }
  }

  public class SlamTrunk : EnemyAttackBase
  {
    private int IntimidateAmount = 1;
    public SlamTrunk()
    {
      Id = "slam_trunk";
      Name = "Slam Trunk";
      Damage = 4;
      Text = EnemyAttackTextHelper.GetText(EnemyAttackTextType.Intimidate, 1);
      OnAttackReveal = (entityManager) =>
      {
        EventManager.Publish(new IntimidateEvent { Amount = IntimidateAmount });
      };
    }
  }

  public class FakeOut : EnemyAttackBase
  {
    private int IntimidateAmount = 2;
    public FakeOut()
    {
      Id = "fake_out";
      Name = "Fake Out";
      Damage = 3;
      Text = EnemyAttackTextHelper.GetText(EnemyAttackTextType.Intimidate, 2);
      OnAttackReveal = (entityManager) =>
      {
        EventManager.Publish(new IntimidateEvent { Amount = IntimidateAmount });
      };
    }
  }
  public class Thud : EnemyAttackBase
  {
    private int WoundedAmount = 1;
    public Thud()
    {
      Id = "thud";
      Name = "Thud";
      Damage = 3;
      Text = EnemyAttackTextHelper.GetText(EnemyAttackTextType.Wounded, 1);
      ConditionType = ConditionType.OnHit;
      OnAttackHit = (entityManager) =>
      {
        EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Player"), Type = AppliedPassiveType.Wounded, Delta = WoundedAmount });
      };
    }
  }
}