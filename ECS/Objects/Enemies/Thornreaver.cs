using System;
using System.Collections.Generic;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Objects.EnemyAttacks;
using Crusaders30XX.ECS.Utils;
using Microsoft.Xna.Framework.Graphics;
using static Crusaders30XX.ECS.Components.CardData;
using Crusaders30XX.ECS.Services;

namespace Crusaders30XX.ECS.Objects.Enemies;

public class Thornreaver : EnemyBase
{
  public Thornreaver(EnemyDifficulty difficulty = EnemyDifficulty.Easy) : base(difficulty)
  {
    Id = "thornreaver";
    Name = "Thornreaver";
    HP = 34;
  }

  public override IEnumerable<string> GetAttackIds(EntityManager entityManager, int turnNumber)
  {
    return ["sawtooth_rend"];
  }

}
public class SawtoothRend : EnemyAttackBase
{
  private int Bleed = 2;
  private CardData.CardColor? Color;
  public SawtoothRend()
  {
    Id = "sawtooth_rend";
    Name = "Sawtooth Rend";
    Damage = 9;
    ConditionType = ConditionType.None;

    OnAttackReveal = (entityManager) =>
    {
      Color = PlayerHandColorService.GetRandomCardColorInPlayerHand(EntityManager);
      Text = Color.HasValue
        ? $"Gain {Bleed} bleed for each {Color.Value.ToString().ToLower()} card that blocks this."
        : $"Gain {Bleed} bleed for each card of the selected color that blocks this. No color is selected.";
    };

    OnBlockProcessed = (entityManager, card) =>
    {
      var color = CardColorQualificationService.GetQualifiedColor(card);
      if (color == Color)
      {
        EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Player"), Type = AppliedPassiveType.Bleed, Delta = Bleed });
      }
    };
  }
}
