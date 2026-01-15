using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Objects.EnemyAttacks;
using Crusaders30XX.ECS.Services;

namespace Crusaders30XX.ECS.Objects.Enemies
{
    public class CinderboltDemon : EnemyBase
    {
      private bool UsedInsidiousBolt = false;
        public CinderboltDemon(EnemyDifficulty difficulty = EnemyDifficulty.Easy) : base(difficulty)
        {
            Id = "cinderbolt_demon";
            Name = "Cinderbolt Demon";
            MaxHealth = 105;
        }

        public override IEnumerable<string> GetAttackIds(EntityManager entityManager, int turnNumber)
        {
          var random = Random.Shared.Next(0, 100);
          if (!UsedInsidiousBolt && (turnNumber == 3 && random < 50 || turnNumber > 3))
          {
            UsedInsidiousBolt = true;
            return ["insidious_bolt"];
          }
          return ["cinderbolt"];
        }
    }
}

public class Cinderbolt : EnemyAttackBase
{
  private int Burn = 1;
  private bool AppliedBurn = false;
  private CardData.CardColor Color = CardData.CardColor.White;
    public Cinderbolt()
    {
        Id = "cinderbolt";
        Name = "Cinderbolt";
        Damage = 10;
        OnAttackReveal = (entityManager) =>
        {
          Color = Cinderbolt.GetRandomCardColorInPlayerHand(EntityManager);
          Text = $"Gain {Burn} burn if at least one {Color.ToString().ToLower()} card blocks this.";
        };

        OnBlockProcessed = (entityManager, card) =>
        {
          var color = card.GetComponent<CardData>().Color;
          if (color == Color && !AppliedBurn)
          {
            EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Player"), Type = AppliedPassiveType.Burn, Delta = Burn });
            AppliedBurn = true;
          }
        };
    }

    public static CardData.CardColor GetRandomCardColorInPlayerHand(EntityManager entityManager)
    {
      var deckEntity = entityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
      var deck = deckEntity?.GetComponent<Deck>();
      var hand = deck?.Hand;
      if (hand == null) return CardData.CardColor.White;
      var colors = hand.Select(c => c.GetComponent<CardData>().Color).Distinct().ToList();
      if (colors.Count == 0) return CardData.CardColor.White;
      return colors[Random.Shared.Next(0, colors.Count)];
    }
}

public class InsidiousBolt : EnemyAttackBase
{
  private int Penance = 3;
  private bool AppliedPenance = false;
  private CardData.CardColor Color = CardData.CardColor.White;
  public InsidiousBolt()
  {
    Id = "insidious_bolt";
    Name = "Insidious Bolt";
    Damage = 10;

    OnAttackReveal = (entityManager) =>
    {
      Color = Cinderbolt.GetRandomCardColorInPlayerHand(EntityManager);
      Text = $"Gain {Penance} penance if at least one {Color.ToString().ToLower()} card blocks this.";
    };

    OnBlockProcessed = (entityManager, card) =>
    {
      var color = card.GetComponent<CardData>().Color;
      if (color == Color && !AppliedPenance)
      {
        EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Player"), Type = AppliedPassiveType.Penance, Delta = Penance });
        AppliedPenance = true;
      }
    };
  }
}