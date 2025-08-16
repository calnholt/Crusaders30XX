using System;
using System.Linq;
using System.Collections.Generic;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Data.Attacks;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// On StartEnemyTurn, selects attacks from EnemyArsenal and queues them into AttackIntent.
	/// Publishes IntentPlanned for UI/telegraphing.
	/// </summary>
	public class EnemyIntentPlanningSystem : Core.System
	{
		private Dictionary<string, AttackDefinition> _attackDefs;

		public EnemyIntentPlanningSystem(EntityManager em) : base(em)
		{
			EventManager.Subscribe<StartEnemyTurn>(_ => OnStartEnemyTurn());
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			// Operates on enemies with EnemyArsenal
			return EntityManager.GetEntitiesWithComponent<EnemyArsenal>();
		}

		protected override void UpdateEntity(Entity entity, Microsoft.Xna.Framework.GameTime gameTime) { }

		private void EnsureAttackDefsLoaded()
		{
			if (_attackDefs != null) return;
			string root = FindProjectRootContaining("Crusaders30XX.csproj");
			if (string.IsNullOrEmpty(root))
			{
				_attackDefs = new();
				return;
			}
			string dir = System.IO.Path.Combine(root, "ECS", "Data", "Enemies");
			_attackDefs = AttackRepository.LoadFromFolder(dir);
		}

		private void OnStartEnemyTurn()
		{
			EnsureAttackDefsLoaded();
			foreach (var enemy in GetRelevantEntities())
			{
				var arsenal = enemy.GetComponent<EnemyArsenal>();
				if (arsenal == null || arsenal.AttackIds.Count == 0) continue;
				var intent = enemy.GetComponent<AttackIntent>();
				if (intent == null)
				{
					intent = new AttackIntent();
					EntityManager.AddComponent(enemy, intent);
				}
				var next = enemy.GetComponent<NextTurnAttackIntent>();
				if (next == null)
				{
					next = new NextTurnAttackIntent();
					EntityManager.AddComponent(enemy, next);
				}

				if (intent.Planned.Count == 0 && next.Planned.Count == 0)
				{
					// First turn: generate both this turn and next turn
					intent.Planned.Clear();
					PlanOne(arsenal, intent);
					next.Planned.Clear();
					PlanOne(arsenal, next);
				}
				else
				{
					// Subsequent turns: promote next -> current, then generate new next
					intent.Planned.Clear();
					intent.Planned.AddRange(next.Planned.Select(pa => new PlannedAttack
					{
						AttackId = pa.AttackId,
						ResolveStep = pa.ResolveStep,
						ContextId = Guid.NewGuid().ToString("N"),
						WasBlocked = false
					}));
					// publish intents for UI
					foreach (var pa in intent.Planned)
					{
						if (_attackDefs.TryGetValue(pa.AttackId, out var def2))
						{
							EventManager.Publish(new IntentPlanned
							{
								AttackId = pa.AttackId,
								ContextId = pa.ContextId,
								Step = def2.resolveStep,
								TelegraphText = def2.name
							});
						}
					}
					next.Planned.Clear();
					PlanOne(arsenal, next);
				}
			}
		}

		private void PlanOne(EnemyArsenal arsenal, dynamic targetIntent)
		{
			string chosenId = arsenal.AttackIds[0];
			if (!_attackDefs.TryGetValue(chosenId, out var def)) return;
			string ctx = Guid.NewGuid().ToString("N");
			targetIntent.Planned.Add(new PlannedAttack
			{
				AttackId = chosenId,
				ResolveStep = System.Math.Max(1, def.resolveStep),
				ContextId = ctx,
				WasBlocked = false
			});
			EventManager.Publish(new IntentPlanned
			{
				AttackId = chosenId,
				ContextId = ctx,
				Step = def.resolveStep,
				TelegraphText = def.name
			});
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


