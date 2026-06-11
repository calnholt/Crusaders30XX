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
            HealthPerCard = 1.485f;
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
  private CardData.CardColor? Color;
    public Cinderbolt()
    {
        Id = "cinderbolt";
        Name = "Cinderbolt";
        Damage = 10;
        OnAttackReveal = (entityManager) =>
        {
          Color = Cinderbolt.GetRandomCardColorInPlayerHand(EntityManager);
          Text = Color.HasValue
            ? $"Gain {Burn} burn if at least one {Color.Value.ToString().ToLower()} card blocks this."
            : $"Gain {Burn} burn if a card of the selected color blocks this. No color is selected.";
        };

        OnBlockProcessed = (entityManager, card) =>
        {
          var color = CardColorQualificationService.GetQualifiedColor(card);
          if (color == Color && !AppliedBurn)
          {
            EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Player"), Type = AppliedPassiveType.Burn, Delta = Burn });
            AppliedBurn = true;
          }
        };
    }

    public static CardData.CardColor? GetRandomCardColorInPlayerHand(EntityManager entityManager)
    {
      var deckEntity = entityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
      var deck = deckEntity?.GetComponent<Deck>();
      var hand = deck?.Hand;
      if (hand == null) return null;
      var colors = hand
        .Select(CardColorQualificationService.GetQualifiedColor)
        .Where(color => color.HasValue)
        .Select(color => color.Value)
        .Distinct()
        .ToList();
      if (colors.Count == 0) return null;
      return colors[Random.Shared.Next(0, colors.Count)];
    }
}

public class InsidiousBolt : EnemyAttackBase
{
  private int Scar = 2;
  private bool AppliedScar = false;
  private CardData.CardColor? Color;
  public InsidiousBolt()
  {
    Id = "insidious_bolt";
    Name = "Insidious Bolt";
    Damage = 10;

    OnAttackReveal = (entityManager) =>
    {
      Color = Cinderbolt.GetRandomCardColorInPlayerHand(EntityManager);
      Text = Color.HasValue
        ? $"Gain {Scar} scar if at least one {Color.Value.ToString().ToLower()} card blocks this."
        : $"Gain {Scar} scar if a card of the selected color blocks this. No color is selected.";
    };

    OnBlockProcessed = (entityManager, card) =>
    {
      var color = CardColorQualificationService.GetQualifiedColor(card);
      if (color == Color && !AppliedScar)
      {
        EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Player"), Type = AppliedPassiveType.Scar, Delta = Scar });
        AppliedScar = true;
      }
    };
  }
}
