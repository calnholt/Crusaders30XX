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
	}
}


