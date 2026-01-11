using System;
using System.Linq;
using System.Collections.Generic;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using System.IO;
using Crusaders30XX.ECS.Factories;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// On StartEnemyTurn, selects attacks from EnemyArsenal and queues them into AttackIntent.
	/// Publishes IntentPlanned for UI/telegraphing.
	/// </summary>
	public class EnemyIntentPlanningSystem : Core.System
	{
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

		private void OnStartEnemyTurn(ChangeBattlePhaseEvent evt)
		{
			Console.WriteLine($"[EnemyIntentPlanningSystem] OnStartEnemyTurn called with Current={evt.Current}");
			// Reset planning guard when a new battle starts so turn 1 can plan again
			if (evt.Current == SubPhase.StartBattle)
			{
				_lastPlannedTurnNumber = -1;
				return;
			}
			if (evt.Current == SubPhase.EnemyStart)
			{
				// Get the phase state to check previous phase before PhaseCoordinatorSystem updates it
				var phaseStateEntity = EntityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault();
				var phaseState = phaseStateEntity?.GetComponent<PhaseState>();
				var previousPhase = phaseState?.Sub ?? SubPhase.StartBattle;
				
				// Read current turn number (before PhaseCoordinatorSystem increments it)
				int currentTurnNumber = GetCurrentTurnNumber();
				
				// If coming from a player phase, PhaseCoordinatorSystem will increment the turn
				// So we need to plan for the incremented turn number
				bool isNewTurn = previousPhase == SubPhase.PlayerEnd || previousPhase == SubPhase.Action || previousPhase == SubPhase.PlayerStart;
				int turnNumber = isNewTurn ? currentTurnNumber + 1 : currentTurnNumber;
				
				Console.WriteLine($"[EnemyIntentPlanningSystem] EnemyStart event received, currentTurn={currentTurnNumber}, planningTurn={turnNumber}, _lastPlannedTurnNumber={_lastPlannedTurnNumber}, previousPhase={previousPhase}, isNewTurn={isNewTurn}");
				
				// Guard: prevent multiple plans for the same EnemyStart turn
				if (_lastPlannedTurnNumber == turnNumber)
				{
					Console.WriteLine($"[EnemyIntentPlanningSystem] Skipping - already planned for turn {turnNumber}");
					return;
				}
				Console.WriteLine("[EnemyIntentPlanningSystem] Planning intents");
				foreach (var enemy in GetRelevantEntities())
				{
					var enemyCmp = enemy.GetComponent<Enemy>();
					var arsenal = enemy.GetComponent<EnemyArsenal>();
					if (arsenal == null || arsenal.AttackIds.Count == 0) continue;
					string enemyId = enemyCmp?.EnemyBase?.Id ?? "demon";
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

					// If this is a new turn (different from last planned), clear old attacks
					if (_lastPlannedTurnNumber != -1 && turnNumber != _lastPlannedTurnNumber)
					{
						Console.WriteLine($"[EnemyIntentPlanningSystem] New turn detected ({_lastPlannedTurnNumber} -> {turnNumber}), clearing old attacks");
						intent.Planned.Clear();
					}

					// Promote next -> current if current is empty
					if (intent.Planned.Count == 0 && next.Planned.Count > 0)
					{
						intent.Planned = next.Planned;
						next.Planned = new List<PlannedAttack>();
					}

					next.Planned.Clear();
					// If current (this turn) is empty, fill it using the current turn's selection
					if (intent.Planned.Count == 0)
					{
						Console.WriteLine("[EnemyIntentPlanningSystem] Planning current turn");
						var currentIds = enemyCmp?.EnemyBase?.GetAttackIds(EntityManager, turnNumber) ?? [];
						Console.WriteLine("[EnemyIntentPlanningSystem] Current turn IDs: " + string.Join(", ", currentIds));
						AddPlanned(currentIds, intent, enemyId);
					}
					// Plan next-turn preview using next turn's selection
					var nextIds = enemyCmp?.EnemyBase?.GetAttackIds(EntityManager, turnNumber + 1) ?? [];
					{
						AddPlanned(nextIds, next, enemyId);
					}
				}
				_lastPlannedTurnNumber = turnNumber;
			}
			else if (evt.Current == SubPhase.PreBlock)
			{
				var intent = EntityManager.GetEntity("Enemy").GetComponent<AttackIntent>();
				var planned = intent.Planned.FirstOrDefault();
				if (planned == null) return;
				if (planned.AttackDefinition.OnAttackReveal != null)
				{
					planned.AttackDefinition.OnAttackReveal(EntityManager);
				}
			}
		}

		private void AddPlanned(IEnumerable<string> attackIds, dynamic target, string enemyId)
		{
			int index = (target.Planned is List<PlannedAttack> l) ? l.Count : 0;
			foreach (var id in attackIds)
			{
				var attackDef = EnemyAttackFactory.Create(id);
				attackDef.Initialize(EntityManager);
				string ctx = Guid.NewGuid().ToString("N");
				var passives = EntityManager.GetEntity("Player").GetComponent<AppliedPassives>().Passives;
				int ambushChance = attackDef.AmbushPercentage + (passives.ContainsKey(AppliedPassiveType.Fear) ? passives[AppliedPassiveType.Fear] * 10 : 0);
				target.Planned.Add(new PlannedAttack
				{
					AttackId = id,
					ResolveStep = Math.Max(1, index + 1),
					ContextId = ctx,
					WasBlocked = false,
					IsAmbush = ambushChance > 0 && Random.Shared.Next(0, 100) < ambushChance,
					AttackDefinition = attackDef
				});
				EventManager.Publish(new IntentPlanned
				{
					AttackId = id,
					ContextId = ctx,
					Step = Math.Max(1, index + 1),
					TelegraphText = attackDef.Name
				});
				index++;
			}
		}

		private int GetCurrentTurnNumber()
		{
			var infoEntity = EntityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault();
			if (infoEntity == null) return 1;
			var info = infoEntity.GetComponent<PhaseState>();
			return info.TurnNumber;
		}

	}
}


