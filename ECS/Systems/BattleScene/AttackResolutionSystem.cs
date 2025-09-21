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
			System.Console.WriteLine("[AttackResolutionSystem] Subscribed to ResolveAttack");
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

			// Load definition via shared cache
			if (!AttackDefinitionCache.TryGet(pa.AttackId, out var def)) return;

			bool blocked = ConditionService.Evaluate(def.conditionsBlocked, pa.ContextId, EntityManager, enemy, null);
			pa.WasBlocked = blocked;

			var target = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
			var source = enemy;
			// Always apply on-hit; if NOT blocked, also apply on-not-blocked
			void ApplyEffects(EffectDefinition[] list)
			{
				if (list == null) return;
				foreach (var eff in list)
				{
					EventManager.Publish(new ApplyEffect
					{
						EffectType = eff.type,
						Amount = eff.amount,
						Status = eff.status,
						Stacks = eff.stacks,
						Source = source,
						Target = target
					});
				}
			}

			ApplyEffects(def.effectsOnHit);
			if (!blocked)
			{
				ApplyEffects(def.effectsOnNotBlocked);
				HandleDiscardSpecificCards(def, pa.ContextId);
			}

			EventManager.Publish(new AttackResolved { ContextId = pa.ContextId, WasBlocked = blocked });
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
					deck.Hand.Remove(c);
					deck.DiscardPile.Add(c);
					EntityManager.RemoveComponent<MarkedForSpecificDiscard>(c);
					EventManager.Publish(new CardMoved { Card = c, Deck = deckEntity, From = CardZoneType.Hand, To = CardZoneType.DiscardPile, ContextId = contextId });
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

	}
}


