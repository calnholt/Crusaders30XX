using System;
using System.Linq;
using System.Collections.Generic;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Data.Attacks;
using Microsoft.Xna.Framework;
using System.IO;
using Crusaders30XX.ECS.Data.Enemies;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// On StartEnemyTurn, selects attacks from EnemyArsenal and queues them into AttackIntent.
	/// Publishes IntentPlanned for UI/telegraphing.
	/// </summary>
	public class EnemyIntentPlanningSystem : Core.System
	{
		private Dictionary<string, AttackDefinition> _attackDefs;
		private bool _isFirstLoad = true;

		public EnemyIntentPlanningSystem(EntityManager em) : base(em)
		{
			EventManager.Subscribe<ChangeBattlePhaseEvent>(OnStartEnemyTurn);
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			// Operates on enemies with EnemyArsenal
			return EntityManager.GetEntitiesWithComponent<EnemyArsenal>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		private void EnsureAttackDefsLoaded()
		{
			if (_attackDefs != null) return;
			_attackDefs = AttackDefinitionCache.GetAll();
		}

		private void OnStartEnemyTurn(ChangeBattlePhaseEvent evt)
		{
			if ((_isFirstLoad && evt.Current == SubPhase.EnemyStart) || (evt.Current == SubPhase.PlayerStart && !_isFirstLoad))
			{
				System.Console.WriteLine("[EnemyIntentPlanningSystem] Planning intents");
				EnsureAttackDefsLoaded();
				int turnNumber = GetCurrentTurnNumber();
				// When planning during PlayerStart, we are planning for the upcoming enemy turn
				if (!_isFirstLoad && evt.Current == SubPhase.PlayerStart)
				{
					turnNumber += 1;
				}
				foreach (var enemy in GetRelevantEntities())
				{
					var arsenal = enemy.GetComponent<EnemyArsenal>();
					if (arsenal == null || arsenal.AttackIds.Count == 0) continue;
					var enemyCmp = enemy.GetComponent<Enemy>();
					string enemyId = enemyCmp?.Id ?? "demon";
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

					// Use per-enemy intent service to (re)plan
					IEnemyIntentService service = CreateServiceForEnemy(enemyId);
					if (service == null)
					{
						System.Console.WriteLine($"[EnemyIntentPlanningSystem] No intent service for enemy '{enemyId}', falling back to round-robin.");
						PlanRoundRobin(arsenal, intent, next);
					}
					else
					{
						System.Console.WriteLine($"[EnemyIntentPlanningSystem] Planning {enemyId} attacks, turn {turnNumber}");
						service.Plan(enemy, arsenal, intent, next, turnNumber, _attackDefs);
					}
				}
				_isFirstLoad = false;
			}
		}

		private void PlanRoundRobin(EnemyArsenal arsenal, dynamic currentIntent, dynamic nextIntent)
		{
			currentIntent.Planned.Clear();
			nextIntent.Planned.Clear();
			int index = 0;
			foreach (var chosenId in arsenal.AttackIds.Take(2))
			{
				if (!_attackDefs.TryGetValue(chosenId, out var def)) continue;
				string ctx = Guid.NewGuid().ToString("N");
				currentIntent.Planned.Add(new PlannedAttack
				{
					AttackId = chosenId,
					ResolveStep = System.Math.Max(1, index + 1),
					ContextId = ctx,
					WasBlocked = false
				});
				EventManager.Publish(new IntentPlanned
				{
					AttackId = chosenId,
					ContextId = ctx,
					Step = System.Math.Max(1, index + 1),
					TelegraphText = def.name
				});
				index++;
			}
		}

		private int GetCurrentTurnNumber()
		{
			var infoEntity = EntityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault();
			var info = infoEntity.GetComponent<PhaseState>();
			return info.TurnNumber;
		}

		private IEnemyIntentService CreateServiceForEnemy(string enemyId)
		{
			switch (enemyId)
			{
				case "demon": return new DemonIntentService();
				default: return null;
			}
		}

	}
}


