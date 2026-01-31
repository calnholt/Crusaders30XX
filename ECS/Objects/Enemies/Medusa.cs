using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Objects.EnemyAttacks;
using Crusaders30XX.ECS.Services;
using Crusaders30XX.ECS.Utils;

namespace Crusaders30XX.ECS.Objects.Enemies
{
	/// <summary>
	/// Medusa - A petrifying enemy that seals cards.
	/// Identity: Card control through sealing/petrification mechanics.
	/// Decision: Balance blocking with sealed cards vs playing cards to crack the seals.
	/// </summary>
	public class Medusa : EnemyBase
	{
		public Medusa(EnemyDifficulty difficulty = EnemyDifficulty.Easy) : base(difficulty)
		{
			Id = "medusa";
			Name = "Medusa";
			MaxHealth = 24;
		}

		public override IEnumerable<string> GetAttackIds(EntityManager entityManager, int turnNumber)
		{
			// Weighted random selection:
			// Gaze: 35%, Stone Stare: 25%, Basilisk Glare: 20%, Serpent Strike: 20%
			var random = new Random();
			int roll = random.Next(100);

			string attack;
			if (roll < 35)
				attack = "gaze";
			else if (roll < 60)  // 35 + 25
				attack = "stone_stare";
			else if (roll < 80)  // 60 + 20
				attack = "basilisk_glare";
			else
				attack = "serpent_strike";

			return new List<string> { attack };
		}
	}
}

/// <summary>
/// Gaze - Medusa's basic attack that seals a card on hit.
/// 4 damage, On hit: seal 1 random card from hand.
/// </summary>
public class Gaze : EnemyAttackBase
{
	public Gaze()
	{
		Id = "gaze";
		Name = "Gaze";
		Damage = 3;
		ConditionType = ConditionType.OnHit;
		Text = "On attack - Seal 1 random card from your hand.\n\nOn hit - Seal the top card of your deck.";

		OnAttackReveal = (entityManager) =>
		{
			EventManager.Publish(new SealCardsEvent { Amount = 1, Type = SealType.Hand });
		};

		OnAttackHit = (entityManager) =>
		{
			EventManager.Publish(new SealCardsEvent { Amount = 1, Type = SealType.TopOfDrawPile });
		};
	}
}

/// <summary>
/// Stone Stare - Medusa's preemptive seal attack.
/// 6 damage, On reveal: seal top card of draw pile.
/// </summary>
public class StoneStare : EnemyAttackBase
{
	public StoneStare()
	{
		Id = "stone_stare";
		Name = "Stone Stare";
		Damage = 6;
		Text = "On attack - Seal the top card of your draw pile.";

		OnAttackReveal = (entityManager) =>
		{
			EventManager.Publish(new SealCardsEvent { Amount = 1, Type = SealType.TopOfDrawPile });
		};
	}
}

/// <summary>
/// Basilisk Glare - Low damage attack that delays sealed cards.
/// 3 damage, On hit: shuffle a sealed card from hand into draw pile.
/// </summary>
public class BasiliskGlare : EnemyAttackBase
{
	public BasiliskGlare()
	{
		Id = "basilisk_glare";
		Name = "Basilisk Glare";
		Damage = 3;
		ConditionType = ConditionType.OnHit;
		Text = "This cannot be blocked by sealed cards.";

		OnAttackReveal = (entityManager) =>
		{
			var cards = GetComponentHelper.GetHandOfCards(entityManager).Where(x => x.GetComponent<Sealed>() != null).ToList();
			cards.ForEach(x => {
				var cannotBlock = new CannotBlockThisAttack{ Reason = "This attack cannot be blocked by sealed cards." };
				entityManager.AddComponent(x, cannotBlock);
			});
		};

		OnBlocksConfirmed = (entityManager) =>
		{
			var cards = GetComponentHelper.GetHandOfCards(entityManager);
			cards.ForEach(x => {
				entityManager.RemoveComponent<CannotBlockThisAttack>(x);
			});
		};
	}
}

/// <summary>
/// Serpent Strike - High damage attack that removes crack progress.
/// 7 damage, On hit: all sealed cards lose 1 crack.
/// </summary>
public class SerpentStrike : EnemyAttackBase
{
	public SerpentStrike()
	{
		Id = "serpent_strike";
		Name = "Serpent Strike";
		Damage = 7;
		ConditionType = ConditionType.OnHit;
		Text = "On hit - All sealed cards you own lose 1 crack.";

		OnAttackHit = (entityManager) =>
		{
			EventManager.Publish(new ModifySealCracksEvent { Delta = -1 });
		};
	}
}
