using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Save;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Objects.EnemyAttacks;
using Crusaders30XX.ECS.Systems;
using Crusaders30XX.ECS.Utils;

namespace Crusaders30XX.ECS.Objects.Enemies;

public class Ninja : EnemyBase
{
  public Ninja(EnemyDifficulty difficulty = EnemyDifficulty.Easy) : base(difficulty)
  {
    Id = "ninja";
    Name = "Ninja";
    MaxHealth = 20;

    OnStartOfBattle = (entityManager) =>
    {
      EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Enemy"), Type = AppliedPassiveType.Stealth, Delta = 1 });
    };
  }

  public override IEnumerable<string> GetAttackIds(EntityManager entityManager, int turnNumber)
  {
    var hasSliceAndDice = false;
    var attacks = new List<string> { "slice" };
    int random = Random.Shared.Next(0, 100);
    var isQuestCompleted = SaveCache.IsQuestCompleted("desert", "desert_13");
    if (random >= 90 || (!isQuestCompleted && turnNumber == 1))
    {
      return ["slice", "dice", "sharpen_blade", "nightveil_guillotine"];
    }
    random = Random.Shared.Next(0, 100);
    if (random >= 50)
    {
      attacks.Add("dice");
      hasSliceAndDice = true;
    }
    // give shadow_step higher weight - maybe improve array util function?
    var linkers = new List<string> { "dusk_flick", "cloaked_reaver", "silencing_stab", "sharpen_blade", "shadow_step", "shadow_step", "shadow_step" };
    var count = (Random.Shared.Next(0, 100) >= 50 ? 1 : 0) + 2;
    attacks.AddRange(ArrayUtils.TakeRandomWithReplacement(linkers, count));
    var shuffledAttacks = ArrayUtils.Shuffled(attacks);
    random = Random.Shared.Next(0, 100);
    if (random >= 80 && hasSliceAndDice)
    {
      return shuffledAttacks.Append("nightveil_guillotine");
    }
    else if (random >= 60)
    {
      return shuffledAttacks.Append("have_no_mercy");
    }
    else if (random >= 50)
    {
      shuffledAttacks.Append(ArrayUtils.TakeRandomWithReplacement(linkers, 1).FirstOrDefault());
    }
    return shuffledAttacks;
  }
}

public class Slice : EnemyAttackBase
{
  public Slice()
  {
    Id = "slice";
    Name = "Slice";
    Damage = 1;
  }
}

public class Dice : EnemyAttackBase
{
  public Dice()
  {
    Id = "dice";
    Name = "Dice";
    Damage = 1;
  }
}

public class DuskFlick : EnemyAttackBase
{
  private int Wounded = 1;
  public DuskFlick()
  {
    Id = "dusk_flick";
    Name = "Dusk Flick";
    Damage = 3;
    ConditionType = ConditionType.OnHit;
    Text = EnemyAttackTextHelper.GetText(EnemyAttackTextType.Wounded, 1, ConditionType);

    OnAttackHit = (entityManager) =>
    {
      EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Player"), Type = AppliedPassiveType.Wounded, Delta = Wounded });
    };
  }
}

public class CloakedReaver : EnemyAttackBase
{
  private int Penance = 2;
  public CloakedReaver()
  {
    Id = "cloaked_reaver";
    Name = "Cloaked Reaver";
    Damage = 3;
    ConditionType = ConditionType.OnHit;
    Text = EnemyAttackTextHelper.GetText(EnemyAttackTextType.Penance, Penance, ConditionType);

    OnAttackHit = (entityManager) =>
    {
      EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Player"), Type = AppliedPassiveType.Penance, Delta = Penance });
    };
  }
}

public class SilencingStab : EnemyAttackBase
{
  private int Frozen = 3;
  public SilencingStab()
  {
    Id = "silencing_stab";
    Name = "Silencing Stab";
    Damage = 3;
    ConditionType = ConditionType.OnHit;
    Text = EnemyAttackTextHelper.GetText(EnemyAttackTextType.Frozen, Frozen, ConditionType);

    OnAttackHit = (entityManager) =>
    {
      EventManager.Publish(new FreezeCardsEvent { Amount = Frozen });
    };
  }
}
public class SharpenBlade : EnemyAttackBase
{
  private int Aggression = 3;
  public SharpenBlade()
  {
    Id = "sharpen_blade";
    Name = "Sharpen Blade";
    Damage = 2;
    ConditionType = ConditionType.OnHit;
    Text = EnemyAttackTextHelper.GetText(EnemyAttackTextType.Aggression, Aggression, ConditionType);

    OnAttackHit = (entityManager) =>
    {
      EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Enemy"), Type = AppliedPassiveType.Aggression, Delta = Aggression });
    };
  }
}

public class ShadowStep : EnemyAttackBase
{
  private int Corrode = 2;
  public ShadowStep()
  {
    Id = "shadow_step";
    Name = "Shadow Step";
    Damage = 3;
    Text = EnemyAttackTextHelper.GetText(EnemyAttackTextType.Corrode, Corrode);

    OnAttackReveal = (entityManager) =>
    {
      if (IsOneBattleOrLastBattle)
      {
        Text = string.Empty;
      }
    };

    OnBlockProcessed = (entityManager, card) =>
    {
      if (!IsOneBattleOrLastBattle)
      {
        // TODO: should send an event to the player to block the attack
        BlockValueService.ApplyDelta(card, -Corrode, "Corrode");
      }
    };
  }
}

public class NightveilGuillotine : EnemyAttackBase
{
  private int DamageIncrease = 4;
  private int Penance = 2;
  public NightveilGuillotine()
  {
    Id = "nightveil_guillotine";
    Name = "Nightveil Guillotine";
    Damage = 4;
    Text = $"If both Slice and Dice hit this turn, this gains +{DamageIncrease} damage and you gain {Penance} penance on hit.";

    OnAttackReveal = (entityManager) =>
    {
      var battleStateInfo = entityManager.GetEntitiesWithComponent<Player>().FirstOrDefault().GetComponent<BattleStateInfo>();
      battleStateInfo.TurnTracking.TryGetValue("slice", out int sliceCount);
      battleStateInfo.TurnTracking.TryGetValue("dice", out int diceCount);
      Console.WriteLine($"[NightveilGuillotine]: slice: {sliceCount} // dice: {diceCount}");
      if (sliceCount > 0 && diceCount > 0)
      {
        Damage += DamageIncrease;
        ConditionType = ConditionType.OnHit;
      }
    };

    OnAttackHit = (entityManager) =>
    {
      if (ConditionType == ConditionType.OnHit)
      {
        EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Player"), Type = AppliedPassiveType.Penance, Delta = Penance });
      }
    };

  }
}