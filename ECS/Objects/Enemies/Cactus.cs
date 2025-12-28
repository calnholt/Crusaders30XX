using System;
using System.Collections.Generic;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Objects.EnemyAttacks;
using Crusaders30XX.ECS.Utils;
using static Crusaders30XX.ECS.Components.CardData;

namespace Crusaders30XX.ECS.Objects.Enemies;

public class Cactus : EnemyBase
{
  public int Thorns { get; set; } = 3;
  public Cactus()
  {
    Id = "cactus";
    Name = "Cactus";
    MaxHealth = 80;

    EventManager.Subscribe<ModifyHpEvent>(OnModifyHpEvent);

    OnCreate = (entityManager) =>
    {
      EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Enemy"), Type = AppliedPassiveType.Thorns, Delta = Thorns });
    };
  }

  private void OnModifyHpEvent(ModifyHpEvent evt)
  {
    if (evt.Target.Name == "Enemy" && evt.DamageType == ModifyTypeEnum.Attack)
    {
      EventManager.Publish(new ApplyPassiveEvent { Target = EntityManager.GetEntity("Player"), Type = AppliedPassiveType.Bleed, Delta = Thorns });
    }
  }
  public override IEnumerable<string> GetAttackIds(EntityManager entityManager, int turnNumber)
  {
    return ArrayUtils.TakeRandomWithoutReplacement(new List<string> { "needle_rain", "barbed_volley", "prickly_burst" }, 1);
  }

  public override void Dispose()
  {
    Console.WriteLine($"[Cactus] Unsubscribed from ModifyHpEvent");
    EventManager.Unsubscribe<ModifyHpEvent>(OnModifyHpEvent);
  }
}

public class NeedleRain : EnemyAttackBase
{
  public NeedleRain()
  {
    Id = "needle_rain";
    Name = "Needle Rain";
    Damage = 9;
    ConditionType = ConditionType.None;
    Text = "Gain [2] bleed for each black card that blocks this.";

    OnBlockProcessed = (entityManager, card) =>
    {
      var color = card.GetComponent<CardData>().Color;
      if (color == CardColor.Black)
      {
        EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Player"), Type = AppliedPassiveType.Bleed, Delta = ValuesParse[0] });
      }
    };
  }
}
public class BarbedVolley : EnemyAttackBase
{
  public BarbedVolley()
  {
    Id = "barbed_volley";
    Name = "Barbed Volley";
    Damage = 9;
    ConditionType = ConditionType.None;
    Text = "Gain [2] bleed for each white card that blocks this.";

    OnBlockProcessed = (entityManager, card) =>
    {
      var color = card.GetComponent<CardData>().Color;
      if (color == CardColor.White)
      {
        EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Player"), Type = AppliedPassiveType.Bleed, Delta = ValuesParse[0] });
      }
    };
  }
}

public class PricklyBurst : EnemyAttackBase
{
  public PricklyBurst()
  {
    Id = "prickly_burst";
    Name = "Prickly Burst";
    Damage = 9;
    ConditionType = ConditionType.None;
    Text = "Gain [2] bleed for each red card that blocks this.";

    OnBlockProcessed = (entityManager, card) =>
    {
      var color = card.GetComponent<CardData>().Color;
      if (color == CardColor.Red)
      {
        EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Player"), Type = AppliedPassiveType.Bleed, Delta = ValuesParse[0] });
      }
    };
  }
}