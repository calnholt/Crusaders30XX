using System;
using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.Diagnostics;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("Combat Debug")]
	public class CombatDebugSystem : Core.System
	{
		public CombatDebugSystem(EntityManager entityManager) : base(entityManager) { }

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return Array.Empty<Entity>();
		}

		protected override void UpdateEntity(Entity entity, Microsoft.Xna.Framework.GameTime gameTime) { }

		[DebugAction("Phase 2 Test: Print Combat Components")]
		public void PrintCombatComponents()
		{
			var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
			if (player == null)
			{
				Console.WriteLine("[CombatDebug] No Player entity found");
			}
			else
			{
				var prog = player.GetComponent<BlockProgress>();
				Console.WriteLine($"[CombatDebug] Player has BlockProgress: {(prog != null)}");
				if (prog?.Counters != null && prog.Counters.Count > 0)
				{
					foreach (var (ctx, counters) in prog.Counters)
					{
						var pairs = string.Join(", ", counters.Select(kv => kv.Key + ":" + kv.Value));
						Console.WriteLine($"  Context {ctx}: {pairs}");
					}
				}
			}

			var enemy = EntityManager.GetEntitiesWithComponent<EnemyArsenal>().FirstOrDefault();
			if (enemy == null)
			{
				Console.WriteLine("[CombatDebug] No Enemy with EnemyArsenal found");
			}
			else
			{
				var arsenal = enemy.GetComponent<EnemyArsenal>();
				var intent = enemy.GetComponent<AttackIntent>();
				Console.WriteLine($"[CombatDebug] Enemy '{enemy.Name}' Arsenal: [" + string.Join(", ", arsenal.AttackIds) + "]");
				if (intent == null || intent.Planned.Count == 0)
				{
					Console.WriteLine("[CombatDebug] No planned attacks yet");
				}
				else
				{
					foreach (var pa in intent.Planned)
					{
						Console.WriteLine($"  Planned: id={pa.AttackId}, step={pa.ResolveStep}, ctx={pa.ContextId}, blocked={pa.WasBlocked}");
					}
				}
			}
		}

		[DebugAction("Phase 5 Test: Simulate BlockCardPlayed Red and Print Counters")]
		public void Phase5Test_SimulateCardPlayedRed()
		{
			Crusaders30XX.ECS.Core.EventManager.Publish(new Crusaders30XX.ECS.Events.BlockCardPlayed { Card = null, Color = "Red" });
			var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
			var prog = player?.GetComponent<BlockProgress>();
			if (prog == null || prog.Counters.Count == 0)
			{
				System.Console.WriteLine("[CombatDebug] No BlockProgress counters yet");
				return;
			}
			foreach (var (ctx, counters) in prog.Counters)
			{
				var val = counters.TryGetValue("played_Red", out var n) ? n : 0;
				System.Console.WriteLine($"[CombatDebug] Ctx {ctx} played_Red={val}");
			}
		}

		[DebugAction("Phase 6 Test: Evaluate current intent block condition")] 
		public void Phase6Test_EvaluateCurrentIntent()
		{
			var enemy = EntityManager.GetEntitiesWithComponent<AttackIntent>().FirstOrDefault();
			if (enemy == null)
			{
				System.Console.WriteLine("[CombatDebug] No enemy with AttackIntent found");
				return;
			}
			var intent = enemy.GetComponent<AttackIntent>();
			if (intent == null || intent.Planned.Count == 0)
			{
				System.Console.WriteLine("[CombatDebug] No planned attacks to evaluate");
				return;
			}
			var pa = intent.Planned[0];
			// Load def via repository
			if (!Crusaders30XX.ECS.Data.Attacks.AttackDefinitionCache.TryGet(pa.AttackId, out var def)) { System.Console.WriteLine("[CombatDebug] Attack def not found: " + pa.AttackId); return; }
			bool blocked = Crusaders30XX.ECS.Systems.ConditionService.Evaluate(def.conditionsBlocked, pa.ContextId, EntityManager, enemy, null);
			System.Console.WriteLine($"[CombatDebug] Evaluate '{def.name}' blocked={blocked}");
		}

		[DebugAction("Phase 7 Test: Resolve Next Intent")]
		public void Phase7Test_ResolveNextIntent()
		{
			var enemy = EntityManager.GetEntitiesWithComponent<AttackIntent>().FirstOrDefault();
			var intent = enemy?.GetComponent<AttackIntent>();
			var pa = intent?.Planned.FirstOrDefault();
			if (pa == null)
			{
				System.Console.WriteLine("[CombatDebug] No planned attacks to resolve");
				return;
			}
			Crusaders30XX.ECS.Core.EventManager.Publish(new Crusaders30XX.ECS.Events.ResolveAttack { ContextId = pa.ContextId });
		}

		[DebugAction("Phase 8 Test: Resolve (no block) then Resolve (blocked)")]
		public void Phase8Test_ResolveTwice()
		{
			var enemy = EntityManager.GetEntitiesWithComponent<AttackIntent>().FirstOrDefault();
			var intent = enemy?.GetComponent<AttackIntent>();
			var pa = intent?.Planned.FirstOrDefault();
			if (pa == null) { System.Console.WriteLine("[CombatDebug] No planned attacks to resolve"); return; }
			// First resolve: no BlockCardPlayed
			Crusaders30XX.ECS.Core.EventManager.Publish(new Crusaders30XX.ECS.Events.ResolveAttack { ContextId = pa.ContextId });
			// Re-plan to get a fresh context
			Crusaders30XX.ECS.Core.EventManager.Publish(new Crusaders30XX.ECS.Events.StartEnemyTurn());
			enemy = EntityManager.GetEntitiesWithComponent<AttackIntent>().FirstOrDefault();
			intent = enemy?.GetComponent<AttackIntent>();
			pa = intent?.Planned.FirstOrDefault();
			if (pa == null) { System.Console.WriteLine("[CombatDebug] No planned attacks after replan"); return; }
			// Simulate block and resolve
			Crusaders30XX.ECS.Core.EventManager.Publish(new Crusaders30XX.ECS.Events.BlockCardPlayed { Color = "Red" });
			Crusaders30XX.ECS.Core.EventManager.Publish(new Crusaders30XX.ECS.Events.ResolveAttack { ContextId = pa.ContextId });
		}

		private static string FindProjectRootContaining(string filename)
		{
			try
			{
				var dir = new System.IO.DirectoryInfo(AppContext.BaseDirectory);
				for (int i = 0; i < 6 && dir != null; i++)
				{
					var candidate = System.IO.Path.Combine(dir.FullName, filename);
					if (System.IO.File.Exists(candidate)) return dir.FullName;
					dir = dir.Parent;
				}
			}
			catch { }
			return null;
		}
	}
}


