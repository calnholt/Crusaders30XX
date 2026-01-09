using System;
using System.Collections.Generic;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Objects.EnemyAttacks;
using Crusaders30XX.ECS.Utils;
using Microsoft.Xna.Framework.Graphics;
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


    OnStartOfBattle = (entityManager) =>
    {
      EventManager.Subscribe<ModifyHpEvent>(OnModifyHpEvent);
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
    return ["prickly_burst"];
  }

  public override void Dispose()
  {
    Console.WriteLine($"[Cactus] Unsubscribed from ModifyHpEvent");
    EventManager.Unsubscribe<ModifyHpEvent>(OnModifyHpEvent);
  }
}
public class PricklyBurst : EnemyAttackBase
{
  private int Bleed = 2;
  private CardData.CardColor Color = CardData.CardColor.Red;
  public PricklyBurst()
  {
    Id = "prickly_burst";
    Name = "Prickly Burst";
    Damage = 9;
    ConditionType = ConditionType.None;

    OnAttackReveal = (entityManager) =>
    {
      Color = Cinderbolt.GetRandomCardColorInPlayerHand(EntityManager);
      Text = $"Gain {Bleed} bleed for each {Color.ToString().ToLower()} card that blocks this.";
    };

    OnBlockProcessed = (entityManager, card) =>
    {
      var color = card.GetComponent<CardData>().Color;
      if (color == Color)
      {
        EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Player"), Type = AppliedPassiveType.Bleed, Delta = Bleed });
      }
    };
  }
}