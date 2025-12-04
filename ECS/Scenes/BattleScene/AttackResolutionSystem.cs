using System;
using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Data.Attacks;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Resolves planned attacks by evaluating conditions and publishing ApplyEffect events
	/// for either on-hit or on-blocked outcomes. Emits AttackResolved at the end.
	/// </summary>
	public class AttackResolutionSystem : Core.System
	{
		public AttackResolutionSystem(EntityManager entityManager) : base(entityManager)
		{
			EventManager.Subscribe<ResolveAttack>(OnResolveAttack);
            Console.WriteLine("[AttackResolutionSystem] Subscribed to ResolveAttack");
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<AttackIntent>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		private void OnResolveAttack(ResolveAttack e)
		{
			if (string.IsNullOrEmpty(e.ContextId)) return;
			// Find the planned attack by context
			var enemy = GetRelevantEntities().FirstOrDefault(en => en.GetComponent<AttackIntent>().Planned.Any(pa => pa.ContextId == e.ContextId));
			if (enemy == null) return;
			var intent = enemy.GetComponent<AttackIntent>();
			var pa = intent.Planned.FirstOrDefault(x => x.ContextId == e.ContextId);
			if (pa == null) return;

			var def = pa.AttackDefinition;
			if (def == null) return;

			var progress = EntityManager.GetEntitiesWithComponent<EnemyAttackProgress>()
				.FirstOrDefault(ent => ent.GetComponent<EnemyAttackProgress>()?.ContextId == pa.ContextId)
				?.GetComponent<EnemyAttackProgress>();

			bool blocked = ConditionService.Evaluate(def.blockingCondition, EntityManager, progress);

			// If a special effect like GlassCannon has already fully prevented this attack
			// (preview shows 0 damage with the condition met), treat it as blocked and
			// avoid publishing any base damage.
			bool fullyPreventedBySpecial = false;
			if (progress != null && def.specialEffects != null && def.specialEffects.Length > 0)
			{
				bool hasGlassCannon = def.specialEffects.Any(se =>
					string.Equals(se.type, "GlassCannon", StringComparison.OrdinalIgnoreCase));
				if (hasGlassCannon && progress.IsConditionMet && progress.ActualDamage <= 0)
				{
					fullyPreventedBySpecial = true;
					blocked = true;
				}
			}

			pa.WasBlocked = blocked;

			var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
			var source = enemy;
			Console.WriteLine($"[AttackResolutionSystem] ResolveAttack {pa.AttackId} {def.damage} isBlocked: {blocked}, fullyPreventedBySpecial: {fullyPreventedBySpecial}");

			if (def.damage > 0 && !fullyPreventedBySpecial)
			{
				EventManager.Publish(new ApplyEffect
				{
					EffectType = "Damage",
					Amount = def.damage,
					Source = enemy,
					Target = player,
					attackId = !blocked ? pa.AttackId : null,
					Percentage = 100
				});
			}

			// Defer not-blocked effects (and final resolution signal) until the attack impact occurs.
			// IMPORTANT: we intentionally reuse the original blocked result captured at resolution time
			// so that changes to Aegis or other prevention between resolution and impact do not cause
			// effectsOnNotBlocked (like bonus damage) to misfire.
			bool blockedAtResolution = blocked;
			System.Action<EnemyAttackImpactNow> impactHandler = null;
			impactHandler = (impact) =>
			{
				if (impact == null || impact.ContextId != pa.ContextId) return;
				EventManager.Unsubscribe(impactHandler);
				if (!blockedAtResolution)
				{
					ApplyEffects(def.effectsOnNotBlocked, source, player);
					HandleDiscardSpecificCards(def, pa.ContextId);
				}
				EventManager.Publish(new AttackResolved { ContextId = pa.ContextId, WasBlocked = blockedAtResolution });
			};
			EventManager.Subscribe(impactHandler);
		}

		private void HandleDiscardSpecificCards(AttackDefinition def, string contextId)
		{
			try
			{
				if (def == null || def.effectsOnNotBlocked == null) return;
				int amount = def.effectsOnNotBlocked.Where(e => e.type == "DiscardSpecificCard").Sum(e => e.amount);
				if (amount <= 0) return;
				var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
				var deck = deckEntity?.GetComponent<Deck>();
				if (deck == null) return;
				var selected = deck.Hand
					.Where(c => {
						var m = c.GetComponent<MarkedForSpecificDiscard>();
						return m != null && m.ContextId == contextId;
					})
					.Take(amount)
					.ToList();
				foreach (var c in selected)
				{
					EntityManager.RemoveComponent<MarkedForSpecificDiscard>(c);
					EventManager.Publish(new CardMoveRequested { Card = c, Deck = deckEntity, Destination = CardZoneType.DiscardPile, ContextId = contextId, Reason = "DiscardSpecificCard" });
				}
				// Clear any leftover marks for this context
				foreach (var c in deck.Hand)
				{
					var m = c.GetComponent<MarkedForSpecificDiscard>();
					if (m != null && m.ContextId == contextId)
					{
						EntityManager.RemoveComponent<MarkedForSpecificDiscard>(c);
					}
				}
			}
			catch { }
		}

		private void ApplyEffects(EffectDefinition[] list, Entity source, Entity player)
		{
			if (list == null) return;
			foreach (var eff in list)
			{
				EventManager.Publish(new ApplyEffect
				{
					EffectType = eff.type,
					Amount = eff.amount,
					Status = eff.status,
					Percentage = eff.percentage,
					Stacks = eff.stacks,
					Source = source,
					Target = string.IsNullOrEmpty(eff.target) || eff.target == "Player" ? player : source
				});
			}
		}

	}
}


