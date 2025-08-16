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
				// Simple: clear and plan first available attack for this turn
				intent.Planned.Clear();
				string chosenId = arsenal.AttackIds[0];
				if (!_attackDefs.TryGetValue(chosenId, out var def)) continue;
				string ctx = Guid.NewGuid().ToString("N");
				intent.Planned.Add(new PlannedAttack
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


