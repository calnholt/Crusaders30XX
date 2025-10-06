using System.Linq;
using System.Collections.Generic;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Data.Attacks;
using Microsoft.Xna.Framework;
using System;
using Crusaders30XX.Diagnostics;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Tracks per-context EnemyAttackProgress from block assignment events and planned attacks,
	/// and precomputes IsBlocked, ActualDamage, and PreventedDamage for UI/logic.
	/// </summary>
	[DebugTab("EnemyAttackProgress")]
	public class EnemyAttackProgressManagementSystem : Core.System
	{
		public EnemyAttackProgressManagementSystem(EntityManager entityManager) : base(entityManager)
		{
			EventManager.Subscribe<BlockAssignmentAdded>(OnBlockAssignmentAdded);
			EventManager.Subscribe<BlockAssignmentRemoved>(OnBlockAssignmentRemoved);
			EventManager.Subscribe<ApplyPassiveEvent>(_ => RecomputeAll());
			EventManager.Subscribe<RemovePassive>(_ => RecomputeAll());
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

		private void PrintProgress(EnemyAttackProgress p)
		{
			Console.WriteLine($"[EnemyAttackProgressManagementSystem] Progress p={p.ContextId} playedCards={p.PlayedCards} playedRed={p.PlayedRed} playedWhite={p.PlayedWhite} playedBlack={p.PlayedBlack} assignedBlockTotal={p.AssignedBlockTotal} additionalConditionalDamageTotal={p.AdditionalConditionalDamageTotal} isConditionMet={p.IsConditionMet} actualDamage={p.ActualDamage} preventedDamage={p.AegisTotal} damageBeforePrevention={p.DamageBeforePrevention} baseDamage={p.BaseDamage} aegisTotal={p.AegisTotal}");
		}

		[DebugAction("Print Progress")]
		public void Debug_PrintProgress()
		{
			var e = EntityManager.GetEntitiesWithComponent<AttackIntent>().FirstOrDefault();
			var progress = FindProgressByContext(e.GetComponent<AttackIntent>().Planned.First().ContextId);
			PrintProgress(progress);
		}


		private void OnBlockAssignmentAdded(BlockAssignmentAdded e)
		{
			System.Console.WriteLine($"[EnemyAttackProgressManagementSystem] BlockAssignmentAdded ctx={e.ContextId} color={e.Color} delta={e.DeltaBlock}");
			if (e == null || string.IsNullOrWhiteSpace(e.Color)) return;
			string color = NormalizeColorKey(e.Color);
			if (string.IsNullOrEmpty(e.ContextId)) return;

			// Update only the specific context this assignment targets
			var enemy = EntityManager.GetEntitiesWithComponent<AttackIntent>()
				.FirstOrDefault(en => en.GetComponent<AttackIntent>()?.Planned?.Any(pa => pa.ContextId == e.ContextId) == true);
			if (enemy == null) return;
			var attackId = enemy.GetComponent<AttackIntent>().Planned.First(pa => pa.ContextId == e.ContextId).AttackId;
			var p = FindOrCreateProgress(e.ContextId, enemy, attackId);
			p.PlayedCards = SafeInc(p.PlayedCards);
			switch (color)
			{
				case "Red": p.PlayedRed = SafeInc(p.PlayedRed); break;
				case "White": p.PlayedWhite = SafeInc(p.PlayedWhite); break;
				case "Black": p.PlayedBlack = SafeInc(p.PlayedBlack); break;
			}
			if (e.DeltaBlock > 0) p.AssignedBlockTotal = SafeInc(p.AssignedBlockTotal, e.DeltaBlock);
			Recompute(p);
			PrintProgress(p);
		}

		private void OnBlockAssignmentRemoved(BlockAssignmentRemoved e)
		{
			System.Console.WriteLine($"[EnemyAttackProgressManagementSystem] BlockAssignmentRemoved ctx={e.ContextId} color={e.Color} delta={e.DeltaBlock}");
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
			PrintProgress(progress);
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
				IsConditionMet = false,
				ActualDamage = 0,
				AegisTotal = 0
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


			int full = DamagePredictionService.ComputeFullDamage(def);
			int aegis = DamagePredictionService.GetAegisAmount(EntityManager);
			p.AegisTotal = aegis;
			// Compute assigned block directly from AssignedBlockCard components for this context
			int assignedFromCardsAndEquipment = 0;
			foreach (var e in EntityManager.GetEntitiesWithComponent<AssignedBlockCard>())
			{
				var abc = e.GetComponent<AssignedBlockCard>();
				if (abc != null && abc.ContextId == p.ContextId)
				{
					assignedFromCardsAndEquipment += abc.BlockAmount;
				}
			}
			p.AdditionalConditionalDamageTotal = (def.effectsOnNotBlocked ?? System.Array.Empty<EffectDefinition>())
				.Where(e => e.type == "Damage")
				.Sum(e => e.amount);
			bool isConditionMet = ConditionService.Evaluate(def.blockingCondition, EntityManager);
			p.BaseDamage = (def.effectsOnHit ?? System.Array.Empty<EffectDefinition>())
				.Where(e => e.type == "Damage")
				.Sum(e => e.amount);
			int preventedDamageFromBlockCondition = isConditionMet ? (def.effectsOnNotBlocked ?? System.Array.Empty<EffectDefinition>())
				.Where(e => e.type == "Damage")
				.Sum(e => e.amount) : 0;
			int reduced = aegis + p.AssignedBlockTotal;
			int actual = full - reduced - preventedDamageFromBlockCondition;
			if (actual < 0) actual = 0;

			p.IsConditionMet = isConditionMet;
			p.ActualDamage = actual;
			p.PreventedDamageFromBlockCondition = preventedDamageFromBlockCondition;
			p.TotalPreventedDamage = aegis + preventedDamageFromBlockCondition + assignedFromCardsAndEquipment;
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


