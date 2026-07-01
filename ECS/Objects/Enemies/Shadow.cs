using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Objects.EnemyAttacks;
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
      HealthPerCard = 2.09f;
      StartAnathema -= (int)difficulty * 1;
      Difficulty = difficulty;

      OnStartOfBattle = (entityManager) =>
      {
        EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Enemy"), Type = AppliedPassiveType.Anathema, Delta = StartAnathema });
      };
    }

    public override IEnumerable<string> GetAttackIds(EntityManager entityManager, int turnNumber)
    {
      if (turnNumber % 2 == 0)
      {
        return ArrayUtils.TakeRandomWithoutReplacement(new List<string> { "snuff_out_the_light", "night_fall", "from_the_shadows", "umbra_slice" }, 3);
      }
      return ArrayUtils.TakeRandomWithoutReplacement(new List<string> { "shadow_strike", "dissipating_darkness" }, 1);
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
    Damage = 10;
    BlockRequiredToPreventEffect = 7;
    Text = $"{EnemyAttackTextHelper.GetBlockThresholdText(Damage - BlockRequiredToPreventEffect.Value, $"The enemy loses {AnathemaLoss} anathema.")}";

    OnDamageThresholdMet = (entityManager) =>
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

public class UmbraSlice : EnemyAttackBase
{
  private int Scar = 1;
  public UmbraSlice()
  {
    Id = "umbra_slice";
    Name = "Umbra Slice";
    Damage = 3;
    ConditionType = ConditionType.OnHit;
    Text = $"On hit - gain {Scar} scar{(Scar > 1 ? "s" : "")}.";

    OnAttackHit = (entityManager) =>
    {
      EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Player"), Type = AppliedPassiveType.Scar, Delta = Scar });
    };
  }
}
