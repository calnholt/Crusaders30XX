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
    public Ogre()
    {
      Id = "ogre";
      Name = "Ogre";
      MaxHealth = 80;
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
    public PummelIntoSubmission()
    {
      Id = "pummel_into_submission";
      Name = "Pummel Into Submission";
      Damage = 10;
      Text = EnemyAttackTextHelper.GetText(EnemyAttackTextType.Intimidate, 1);
      OnAttackReveal = (entityManager) =>
      {
        EventManager.Publish(new IntimidateEvent { Amount = ValuesParse[0] });
      };
    }
  }
  public class TreeStomp : EnemyAttackBase
  {
    public TreeStomp()
    {
      Id = "tree_stomp";
      Name = "Tree Stomp";
      Damage = 9;
      Text = EnemyAttackTextHelper.GetText(EnemyAttackTextType.Intimidate, 2);
      OnAttackReveal = (entityManager) =>
      {
        EventManager.Publish(new IntimidateEvent { Amount = ValuesParse[0] });
      };
    }
  }

  public class SlamTrunk : EnemyAttackBase
  {
    public SlamTrunk()
    {
      Id = "slam_trunk";
      Name = "Slam Trunk";
      Damage = 4;
      Text = EnemyAttackTextHelper.GetText(EnemyAttackTextType.Intimidate, 1);
      OnAttackReveal = (entityManager) =>
      {
        EventManager.Publish(new IntimidateEvent { Amount = ValuesParse[0] });
      };
    }
  }

  public class FakeOut : EnemyAttackBase
  {
    public FakeOut()
    {
      Id = "fake_out";
      Name = "Fake Out";
      Damage = 3;
      Text = EnemyAttackTextHelper.GetText(EnemyAttackTextType.Intimidate, 2);
      OnAttackReveal = (entityManager) =>
    {
      EventManager.Publish(new IntimidateEvent { Amount = ValuesParse[0] });
    };
  }
  }
  public class Thud : EnemyAttackBase
  {
    public Thud()
    {
      Id = "thud";
      Name = "Thud";
      Damage = 5;
      Text = EnemyAttackTextHelper.GetText(EnemyAttackTextType.Wounded, 1);
      OnAttackHit = (entityManager) =>
      {
        EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Player"), Type = AppliedPassiveType.Wounded, Delta = ValuesParse[0] });
      };
    }
  }
}