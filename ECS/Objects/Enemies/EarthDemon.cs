using System;
using System.Collections.Generic;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Objects.EnemyAttacks;
using Crusaders30XX.ECS.Systems;

namespace Crusaders30XX.ECS.Objects.Enemies;

/// <summary>
/// A slow, armored demon whose attacks punish poor blocking decisions.
/// Forces players to carefully manage their blocking card count.
/// </summary>
public class EarthDemon : EnemyBase
{
    private int StartingArmor = 1;

    public EarthDemon(EnemyDifficulty difficulty = EnemyDifficulty.Easy) : base(difficulty)
    {
        Id = "earth_demon";
        Name = "Earth Demon";
        MaxHealth = 13 + (int)difficulty * 2;

        // Earthen Resilience: Start of battle, gain 3 Armor
        OnStartOfBattle = (entityManager) =>
        {
            EventQueueBridge.EnqueueTriggerAction("EarthDemon.OnStartOfBattle", () =>
            {
                EventManager.Publish(new ApplyPassiveEvent
                {
                    Target = entityManager.GetEntity("Enemy"),
                    Type = AppliedPassiveType.Armor,
                    Delta = StartingArmor
                });
            }, AppliedPassivesManagementSystem.Duration);
        };
    }

    public override IEnumerable<string> GetAttackIds(EntityManager entityManager, int turnNumber)
    {
        // Attack weights: Tremor Strike (40%), Stone Barrage (35%), Earthen Wall (25%)
        var random = Random.Shared.Next(0, 100);
        if (random < 40)
        {
            return ["tremor_strike"];
        }
        else if (random < 75)
        {
            return ["stone_barrage"];
        }
        return ["earthen_wall"];
    }
}

/// <summary>
/// Tremor Strike: 8 damage. If not blocked by 2+ cards, apply 1 Shackled.
/// The "must block well" attack - creates tension between committing cards and accepting debuffs.
/// </summary>
public class TremorStrike : EnemyAttackBase
{
    private int ShackledAmount = 2;

    public TremorStrike()
    {
        Id = "tremor_strike";
        Name = "Tremor Strike";
        Damage = 8;
        ConditionType = ConditionType.OnBlockedByAtLeast2Cards;
        Text = $"If not blocked by 2+ cards - Gain {ShackledAmount} shackled.";

        OnAttackHit = (entityManager) =>
        {
            EventManager.Publish(new ApplyPassiveEvent
            {
                Target = entityManager.GetEntity("Player"),
                Type = AppliedPassiveType.Shackled,
                Delta = ShackledAmount
            });
        };
    }
}

/// <summary>
/// Stone Barrage: 6 damage. On reveal, pick random color from player's hand.
/// Apply 1 Bleed per blocking card of that color.
/// The "choose your blockers carefully" attack - makes hand composition matter.
/// </summary>
public class StoneBarrage : EnemyAttackBase
{
    private int BleedPerCard = 2;
    private CardData.CardColor Color = CardData.CardColor.White;

    public StoneBarrage()
    {
        Id = "stone_barrage";
        Name = "Stone Barrage";
        Damage = 4;
        ConditionType = ConditionType.None;

        OnAttackReveal = (entityManager) =>
        {
            Color = Cinderbolt.GetRandomCardColorInPlayerHand(EntityManager);
            Text = $"Gain {BleedPerCard} bleed for each {Color.ToString().ToLower()} card that blocks this.";
        };

        OnBlockProcessed = (entityManager, card) =>
        {
            var color = card.GetComponent<CardData>().Color;
            if (color == Color)
            {
                EventManager.Publish(new ApplyPassiveEvent
                {
                    Target = entityManager.GetEntity("Player"),
                    Type = AppliedPassiveType.Bleed,
                    Delta = BleedPerCard
                });
            }
        };
    }
}

/// <summary>
/// Earthen Wall: 5 damage. On attack reveal, enemy gains 2 Armor.
/// The "scaling threat" attack - creates time pressure to kill quickly.
/// </summary>
public class EarthenWall : EnemyAttackBase
{
    private int ArmorGain = 1;

    public EarthenWall()
    {
        Id = "earthen_wall";
        Name = "Earthen Wall";
        Damage = 6;
        ConditionType = ConditionType.None;
        Text = $"On attack - Gain {ArmorGain} armor.";

        OnAttackReveal = (entityManager) =>
        {
            EventManager.Publish(new ApplyPassiveEvent
            {
                Target = entityManager.GetEntity("Enemy"),
                Type = AppliedPassiveType.Armor,
                Delta = ArmorGain
            });
        };
    }
}
