using System.Linq;
using System.Collections.Generic;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Data.Attacks;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Tracks per-context EnemyAttackProgress from block assignment events and planned attacks,
	/// and precomputes IsBlocked, ActualDamage, and PreventedDamage for UI/logic.
	/// </summary>
	public class EnemyAttackProgressManagementSystem : Core.System
	{
		public EnemyAttackProgressManagementSystem(EntityManager entityManager) : base(entityManager)
		{
			EventManager.Subscribe<BlockAssignmentAdded>(OnBlockAssignmentAdded);
			EventManager.Subscribe<BlockAssignmentRemoved>(OnBlockAssignmentRemoved);
			EventManager.Subscribe<ModifyStoredBlock>(_ => RecomputeAll());
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<AttackIntent>();
		}

		protected override void UpdateEntity(Entity enemy, GameTime gameTime)
		{
			var intent = enemy.GetComponent<AttackIntent>();
			if (intent == null) return;

			// Ensure a progress entity exists for each planned attack context
			var activeContextIds = new HashSet<string>();
			foreach (var pa in intent.Planned)
			{
				if (string.IsNullOrEmpty(pa.ContextId)) continue;
				activeContextIds.Add(pa.ContextId);
				var progress = FindOrCreateProgress(pa.ContextId, enemy, pa.AttackId);
				// Keep AttackId in sync in case it changes
				progress.AttackId = pa.AttackId;
				Recompute(progress);
			}

			// Remove any progress entities whose contextId is no longer present
			var all = EntityManager.GetEntitiesWithComponent<EnemyAttackProgress>().ToList();
			foreach (var e in all)
			{
				var p = e.GetComponent<EnemyAttackProgress>();
				if (p == null) continue;
				if (p.Enemy == enemy && !string.IsNullOrEmpty(p.ContextId) && !activeContextIds.Contains(p.ContextId))
				{
					EntityManager.DestroyEntity(e.Id);
				}
			}
		}

		private void OnBlockAssignmentAdded(BlockAssignmentAdded e)
		{
			if (e == null || string.IsNullOrWhiteSpace(e.Color)) return;
			string color = NormalizeColorKey(e.Color);

			// Match existing behavior: increment for all active planned contexts
			var contexts = new List<(Entity enemy, string ctxId, string attackId)>();
			foreach (var enemy in EntityManager.GetEntitiesWithComponent<AttackIntent>())
			{
				var intent = enemy.GetComponent<AttackIntent>();
				if (intent == null) continue;
				foreach (var pa in intent.Planned)
				{
					if (!string.IsNullOrEmpty(pa.ContextId))
						contexts.Add((enemy, pa.ContextId, pa.AttackId));
				}
			}

			foreach (var (enemy, ctx, attackId) in contexts)
			{
				var p = FindOrCreateProgress(ctx, enemy, attackId);
				// Increment typed counters
				p.PlayedCards = SafeInc(p.PlayedCards);
				switch (color)
				{
					case "Red": p.PlayedRed = SafeInc(p.PlayedRed); break;
					case "White": p.PlayedWhite = SafeInc(p.PlayedWhite); break;
					case "Black": p.PlayedBlack = SafeInc(p.PlayedBlack); break;
				}
				if (e.DeltaBlock > 0) p.AssignedBlockTotal = SafeInc(p.AssignedBlockTotal, e.DeltaBlock);
				Recompute(p);
			}
		}

		private void OnBlockAssignmentRemoved(BlockAssignmentRemoved e)
		{
			if (e == null || string.IsNullOrEmpty(e.ContextId)) return;

			// Find the owning enemy and attack for this context
			var progress = FindProgressByContext(e.ContextId);
			if (progress == null) return;

			// Maintain running totals
			long nextAssigned = (long)progress.AssignedBlockTotal + e.DeltaBlock;
			progress.AssignedBlockTotal = nextAssigned < 0 ? 0 : (int)nextAssigned;

			// Adjust color play counters and played cards like previous system
			if (!string.IsNullOrWhiteSpace(e.Color) && e.DeltaBlock < 0)
			{
				string color = NormalizeColorKey(e.Color);
				switch (color)
				{
					case "Red": progress.PlayedRed = SafeDec(progress.PlayedRed); break;
					case "White": progress.PlayedWhite = SafeDec(progress.PlayedWhite); break;
					case "Black": progress.PlayedBlack = SafeDec(progress.PlayedBlack); break;
				}
			}
			progress.PlayedCards = SafeDec(progress.PlayedCards);

			Recompute(progress);
		}

		private EnemyAttackProgress FindOrCreateProgress(string contextId, Entity enemy, string attackId)
		{
			var existing = FindProgressByContext(contextId);
			if (existing != null)
			{
				existing.Enemy = enemy;
				return existing;
			}
			var entity = EntityManager.CreateEntity($"EnemyAttackProgress[{contextId}]");
			var comp = new EnemyAttackProgress
			{
				ContextId = contextId,
				Enemy = enemy,
				AttackId = attackId,
				AssignedBlockTotal = 0,
				PlayedCards = 0,
				PlayedRed = 0,
				PlayedWhite = 0,
				PlayedBlack = 0,
				IsBlocked = false,
				ActualDamage = 0,
				PreventedDamage = 0
			};
			EntityManager.AddComponent(entity, comp);
			return comp;
		}

		private EnemyAttackProgress FindProgressByContext(string contextId)
		{
			foreach (var e in EntityManager.GetEntitiesWithComponent<EnemyAttackProgress>())
			{
				var p = e.GetComponent<EnemyAttackProgress>();
				if (p != null && p.ContextId == contextId) return p;
			}
			return null;
		}

		private void RecomputeAll()
		{
			foreach (var e in EntityManager.GetEntitiesWithComponent<EnemyAttackProgress>())
			{
				var p = e.GetComponent<EnemyAttackProgress>();
				if (p != null) Recompute(p);
			}
		}

		private void Recompute(EnemyAttackProgress p)
		{
			if (p == null || string.IsNullOrEmpty(p.AttackId)) return;
			if (!AttackDefinitionCache.TryGet(p.AttackId, out var def) || def == null) return;

			bool isBlocked = ConditionService.Evaluate(def.conditionsBlocked, p.ContextId, EntityManager, p.Enemy, null);
			int full = DamagePredictionService.ComputeFullDamage(def);
			int stored = DamagePredictionService.GetStoredBlockAmount(EntityManager);
			int preventedBlockCondition = isBlocked ? (def.effectsOnNotBlocked ?? System.Array.Empty<EffectDefinition>())
				.Where(e => e.type == "Damage")
				.Sum(e => e.amount) : 0;
			int reduced = stored + p.AssignedBlockTotal;
			int actual = full - reduced - preventedBlockCondition;
			if (actual < 0) actual = 0;

			p.IsBlocked = isBlocked;
			p.ActualDamage = actual;
			p.PreventedDamage = stored + p.AssignedBlockTotal + preventedBlockCondition;
			p.DamageBeforePrevention = full; // actual + prevented (by definition of ComputeFullDamage)
		}

		private static string NormalizeColorKey(string color)
		{
			string c = color.Trim().ToLowerInvariant();
			switch (c)
			{
				case "r":
				case "red": return "Red";
				case "w":
				case "white": return "White";
				case "b":
				case "black": return "Black";
				default: return char.ToUpperInvariant(color[0]) + color.Substring(1);
			}
		}

		private static int SafeInc(int value, int delta = 1)
		{
			long next = (long)value + delta;
			return next < 0 ? 0 : (int)next;
		}

		private static int SafeDec(int value, int delta = 1)
		{
			long next = (long)value - delta;
			return next < 0 ? 0 : (int)next;
		}
	}
}


