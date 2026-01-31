using System;
using System.Collections.Generic;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Objects.EnemyAttacks;
using Crusaders30XX.ECS.Services;
using Crusaders30XX.ECS.Utils;

namespace Crusaders30XX.ECS.Objects.Enemies
{
  public class Shadow : EnemyBase
  {
    private int StartAnathema = 4;
    public Shadow(EnemyDifficulty difficulty = EnemyDifficulty.Easy) : base(difficulty)
    {
      Id = "shadow";
      Name = "Shadow";
      MaxHealth = 38 + (int)difficulty * 1;
      StartAnathema -= (int)difficulty * 1;

      OnStartOfBattle = (entityManager) =>
      {
        EventManager.Subscribe<PledgeAddedEvent>(OnPledgeAddedEvent);
        EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Enemy"), Type = AppliedPassiveType.Anathema, Delta = StartAnathema });
      };
            Difficulty = difficulty;
        }

    private void OnPledgeAddedEvent(PledgeAddedEvent evt)
    {
      var ap = GetComponentHelper.GetAppliedPassives(EntityManager, "Enemy");
      if (ap == null || ap.Passives == null || ap.Passives.Count == 0) return;
      if (ap.Passives.TryGetValue(AppliedPassiveType.Anathema, out int darknessStacks) && darknessStacks > 0)
      {
        Console.WriteLine($"[Shadow] Anathema triggered - darknessStacks={darknessStacks}");
        EventManager.Publish(new ModifyHpRequestEvent
        {
          Source = EntityManager.GetEntity("Enemy"),
          Target = EntityManager.GetEntity("Enemy"),
          Delta = -darknessStacks,
          DamageType = ModifyTypeEnum.Effect
        });
      }
    }
    public override IEnumerable<string> GetAttackIds(EntityManager entityManager, int turnNumber)
    {
      if (turnNumber % 2 == 0)
      {
        return ArrayUtils.TakeRandomWithoutReplacement(new List<string> { "snuff_out_the_light", "night_fall", "from_the_shadows" }, 3);
      }
      return ArrayUtils.TakeRandomWithoutReplacement(new List<string> { "shadow_strike", "dissipating_darkness" }, 1);
    }

    public override void Dispose()
    {
      Console.WriteLine($"[Shadow] Unsubscribed from PledgeAddedEvent");
      EventManager.Unsubscribe<PledgeAddedEvent>(OnPledgeAddedEvent);
    }
  }
}

public class ShadowStrike : EnemyAttackBase
{
  private int AnathemaLoss = 1;
  public ShadowStrike()
  {
    Id = "shadow_strike";
    Name = "Shadow Strike";
    Damage = 9;
    ConditionType = ConditionType.OnHit;
    Text = $"On hit - the enemy loses {AnathemaLoss} anathema.";

    OnAttackHit = (entityManager) =>
    {
      EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Enemy"), Type = AppliedPassiveType.Anathema, Delta = -AnathemaLoss });
    };
  }
}

public class EncroachingDarkness : EnemyAttackBase
{
  private int AnathemaGain = 1;
  public EncroachingDarkness()
  {
    Id = "dissipating_darkness";
    Name = "Encroaching Darkness";
    Damage = 10;
    ConditionType = ConditionType.OnBlockedByAtLeast2Cards;
    Text = $"{EnemyAttackTextHelper.GetConditionText(ConditionType)} the enemy gains {AnathemaGain} anathema.";

    OnAttackHit = (entityManager) =>
    {
      EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Enemy"), Type = AppliedPassiveType.Anathema, Delta = AnathemaGain });
    };
  }
}

public class SnuffOutTheLight : EnemyAttackBase
{
  private int SilencedGain = 1;
  public SnuffOutTheLight()
  {
    Id = "snuff_out_the_light";
    Name = "Snuff Out the Light";
    Damage = 3;
    ConditionType = ConditionType.OnHit;
    Text = $"On hit - Gain {SilencedGain} silenced.";

    OnAttackHit = (entityManager) =>
    {
      EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Player"), Type = AppliedPassiveType.Silenced, Delta = SilencedGain });
    };
  }
}

public class FromTheShadows : EnemyAttackBase
{
  public FromTheShadows()
  {
    Id = "from_the_shadows";
    Name = "From the Shadows";
    Damage = 3;
    ConditionType = ConditionType.OnHit;
    Text = EnemyAttackTextHelper.GetText(EnemyAttackTextType.Intimidate, 1);

    OnAttackReveal = (entityManager) =>
    {
      EventManager.Publish(new IntimidateEvent { Amount = 1 });
    };

  }
}

public class NightFall : EnemyAttackBase
{
  private int AnathemaLoss = 1;
  public NightFall()
  {
    Id = "night_fall";
    Name = "Night Fall";
    Damage = 3;
    ConditionType = ConditionType.OnHit;
    Text = $"On hit - the enemy loses {AnathemaLoss} anathema.";

    OnAttackHit = (entityManager) =>
    {
      EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Enemy"), Type = AppliedPassiveType.Anathema, Delta = -AnathemaLoss });
    };
  }
}