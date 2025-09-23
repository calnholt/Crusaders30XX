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
		private int _lastPlannedTurnNumber = -1;

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
			// Reset planning guard when a new battle starts so turn 0 can plan again
			if (evt.Current == SubPhase.StartBattle)
			{
				_lastPlannedTurnNumber = -1;
				_isFirstLoad = true;
				return;
			}
			if (evt.Current == SubPhase.EnemyStart)
			{
				int turnNumber = GetCurrentTurnNumber();
				// Guard: prevent multiple plans for the same EnemyStart turn
				if (_lastPlannedTurnNumber == turnNumber)
				{
					return;
				}
				System.Console.WriteLine("[EnemyIntentPlanningSystem] Planning intents");
				EnsureAttackDefsLoaded();
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

					// Promote next -> current if current is empty
					if (intent.Planned.Count == 0 && next.Planned.Count > 0)
					{
						intent.Planned = next.Planned;
						next.Planned = new List<PlannedAttack>();
					}

					// Use per-enemy intent service to select attack IDs; centralized planning handles clears, context IDs, and telegraphs
					IEnemyIntentService service = CreateServiceForEnemy(enemyId);
					if (service == null) continue;
					System.Console.WriteLine($"[EnemyIntentPlanningSystem] Planning {enemyId} attacks, turn {turnNumber}");
					// Always clear the next-turn preview before populating
					next.Planned.Clear();
					// If current (this turn) is empty, fill it using the prior turn's selection
					if (intent.Planned.Count == 0)
					{
						var currentIds = service.SelectForTurn(enemy, arsenal, Math.Max(0, turnNumber - 1));
						AddPlanned(currentIds, intent, enemyId);
					}
					// Plan next-turn preview using this turn's selection
					var nextIds = service.SelectForTurn(enemy, arsenal, Math.Max(0, turnNumber));
					{
						AddPlanned(nextIds, next, enemyId);
					}
				}
				_lastPlannedTurnNumber = turnNumber;
				_isFirstLoad = false;
			}
		}

		private void AddPlanned(IEnumerable<string> attackIds, dynamic target, string enemyId)
		{
			int index = (target.Planned is List<PlannedAttack> l) ? l.Count : 0;
			foreach (var id in attackIds)
			{
				if (!_attackDefs.TryGetValue(id, out var def)) continue;
				string ctx = Guid.NewGuid().ToString("N");
				EnemyDefinitionCache.TryGet(enemyId, out var enemyDef);
				AttackDefinitionCache.TryGet(id, out var attackDef);
				Console.WriteLine($"[EnemyIntentPlanningSystem] AddPlanned id:{id} enemyId:{enemyId} isGeneric:{attackDef.isGeneric} genericAmbushPercentage:{enemyDef.genericAttackAmbushPercentage} ambushPercentage:{def.ambushPercentage}");
				int ambushChance = attackDef.isGeneric ? enemyDef.genericAttackAmbushPercentage : def.ambushPercentage;
				target.Planned.Add(new PlannedAttack
				{
					AttackId = id,
					ResolveStep = System.Math.Max(1, index + 1),
					ContextId = ctx,
					WasBlocked = false,
					IsAmbush = ambushChance > 0 && Random.Shared.Next(0, 100) < ambushChance
				});
				EventManager.Publish(new IntentPlanned
				{
					AttackId = id,
					ContextId = ctx,
					Step = System.Math.Max(1, index + 1),
					TelegraphText = def.name
				});
				index++;
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
				case "succubus": return new SuccubusIntentService();
				case "spider": return new SpiderIntentService();
				default: return null;
			}
		}

	}
}


