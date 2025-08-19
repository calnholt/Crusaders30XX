using System;
using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.Diagnostics;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("Combat Events (Debug)")]
	public class CombatEventsDebugSystem : Core.System
	{
		public CombatEventsDebugSystem(EntityManager entityManager) : base(entityManager)
		{
			EventManager.Subscribe<StartEnemyTurn>(_ => Console.WriteLine("[EventsDebug] StartEnemyTurn"));
			EventManager.Subscribe<EndEnemyTurn>(_ => Console.WriteLine("[EventsDebug] EndEnemyTurn"));
			EventManager.Subscribe<IntentPlanned>(e => Console.WriteLine($"[EventsDebug] IntentPlanned id={e.AttackId} ctx={e.ContextId} step={e.Step}"));
			EventManager.Subscribe<BlockAssignmentAdded>(e => Console.WriteLine($"[EventsDebug] BlockCardPlayed color={e.Color} card={(e.Card != null ? e.Card.Name : "null")}"));
			EventManager.Subscribe<ResolveAttack>(e => Console.WriteLine($"[EventsDebug] ResolveAttack ctx={e.ContextId}"));
			EventManager.Subscribe<ApplyEffect>(e => Console.WriteLine($"[EventsDebug] ApplyEffect type={e.EffectType} amt={e.Amount} status={e.Status} stacks={e.Stacks}"));
			EventManager.Subscribe<AttackResolved>(e => Console.WriteLine($"[EventsDebug] AttackResolved ctx={e.ContextId} blocked={e.WasBlocked}"));
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return Array.Empty<Entity>();
		}

		protected override void UpdateEntity(Entity entity, Microsoft.Xna.Framework.GameTime gameTime) { }

		[DebugAction("Publish StartEnemyTurn")]
		public void Debug_PublishStartEnemyTurn()
		{
			EventManager.Publish(new StartEnemyTurn());
		}

		[DebugAction("Seed NextTurn intents (2 items)")]
		public void Debug_SeedNextTurnIntents()
		{
			var enemy = EntityManager.GetEntitiesWithComponent<Crusaders30XX.ECS.Components.Enemy>().FirstOrDefault();
			if (enemy == null) { Console.WriteLine("[EventsDebug] No enemy found"); return; }
			var next = enemy.GetComponent<Crusaders30XX.ECS.Components.NextTurnAttackIntent>();
			if (next == null)
			{
				next = new Crusaders30XX.ECS.Components.NextTurnAttackIntent();
				EntityManager.AddComponent(enemy, next);
			}
			next.Planned.Clear();
			next.Planned.Add(new Crusaders30XX.ECS.Components.PlannedAttack { AttackId = "demon_bite", ResolveStep = 1, ContextId = "next_a" });
			next.Planned.Add(new Crusaders30XX.ECS.Components.PlannedAttack { AttackId = "demon_bite", ResolveStep = 2, ContextId = "next_b" });
			Console.WriteLine("[EventsDebug] Seeded next-turn intents = 2");
		}

		[DebugAction("Publish BlockCardPlayed (Red)")]
		public void Debug_PublishCardPlayedRed()
		{
			var anyCard = EntityManager.GetEntitiesWithComponent<Crusaders30XX.ECS.Components.CardData>().FirstOrDefault();
			EventManager.Publish(new BlockAssignmentAdded { Card = anyCard, Color = "Red" });
		}

		[DebugAction("Publish ResolveAttack (ctx=test_ctx)")]
		public void Debug_PublishResolveAttack()
		{
			EventManager.Publish(new ResolveAttack { ContextId = "test_ctx" });
		}
	}
}


